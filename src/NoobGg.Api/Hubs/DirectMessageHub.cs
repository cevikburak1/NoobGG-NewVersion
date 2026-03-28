using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using NoobGg.Api.Hubs.Contracts;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Features.DirectMessages.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Api.Hubs;

[Authorize]
public class DirectMessageHub : Hub<IDirectMessageClient>
{
    private readonly ILogger<DirectMessageHub> _logger;
    private readonly IMongoContext _mongoContext;
    private readonly IPresenceTracker _presenceTracker;
    private readonly INotificationService _notificationService;

    public DirectMessageHub(
        ILogger<DirectMessageHub> logger,
        IMongoContext mongoContext,
        IPresenceTracker presenceTracker,
        INotificationService notificationService)
    {
        _logger = logger;
        _mongoContext = mongoContext;
        _presenceTracker = presenceTracker;
        _notificationService = notificationService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        _presenceTracker.UserConnected(userId, Context.ConnectionId);

        var conversations = _mongoContext.GetCollection<Conversation>(CollectionNames.Conversations);
        var filter = Builders<Conversation>.Filter.Or(
            Builders<Conversation>.Filter.Eq(c => c.Participant1Id, userId),
            Builders<Conversation>.Filter.Eq(c => c.Participant2Id, userId));

        var convList = await conversations.Find(filter).ToListAsync();
        foreach (var conv in convList)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"dm:{conv.Id}");
        }

        if (await ShouldBroadcastPresence(userId))
            await Clients.Others.PresenceChanged(userId, true);

        _logger.LogInformation("User {UserId} connected to DirectMessageHub", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        _presenceTracker.UserDisconnected(userId, Context.ConnectionId);

        if (!_presenceTracker.IsOnline(userId))
        {
            if (await ShouldBroadcastPresence(userId))
                await Clients.Others.PresenceChanged(userId, false);
        }

        _logger.LogInformation("User {UserId} disconnected from DirectMessageHub", userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(string conversationId)
    {
        var userId = GetUserId();
        var conversations = _mongoContext.GetCollection<Conversation>(CollectionNames.Conversations);
        var conv = await conversations.Find(c => c.Id == conversationId).FirstOrDefaultAsync();

        if (conv is null) return;
        if (conv.Participant1Id != userId && conv.Participant2Id != userId) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"dm:{conversationId}");
    }

    private const int MaxMessageLength = 2000;

    public async Task SendDirectMessage(string conversationId, string content)
    {
        var userId = GetUserId();
        var username = GetUsername();

        if (string.IsNullOrWhiteSpace(content)) return;
        if (content.Length > MaxMessageLength)
            throw new HubException($"Message exceeds maximum length of {MaxMessageLength} characters");

        var conversations = _mongoContext.GetCollection<Conversation>(CollectionNames.Conversations);
        var messages = _mongoContext.GetCollection<DirectMessage>(CollectionNames.DirectMessages);
        var blocks = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);

        var conv = await conversations.Find(c => c.Id == conversationId).FirstOrDefaultAsync();
        if (conv is null) return;

        if (conv.Participant1Id != userId && conv.Participant2Id != userId) return;

        var partnerId = conv.Participant1Id == userId ? conv.Participant2Id : conv.Participant1Id;

        var isBlocked = await blocks.Find(b =>
            (b.BlockerId == userId && b.BlockedUserId == partnerId) ||
            (b.BlockerId == partnerId && b.BlockedUserId == userId)
        ).AnyAsync();
        if (isBlocked)
            throw new HubException("Cannot send message to this user");

        var settingsCol = _mongoContext.GetCollection<UserSettings>(CollectionNames.UserSettings);
        var partnerSettings = await settingsCol.Find(s => s.UserId == partnerId).FirstOrDefaultAsync();

        if (partnerSettings is not null)
        {
            switch (partnerSettings.DmPermission)
            {
                case DmPermission.Nobody:
                    throw new HubException("This user is not accepting direct messages");
                case DmPermission.FriendsOnly:
                    var friendships = _mongoContext.GetCollection<Friendship>(CollectionNames.Friendships);
                    var areFriends = await friendships.Find(f =>
                        f.Status == FriendshipStatus.Accepted &&
                        ((f.RequesterId == userId && f.AddresseeId == partnerId) ||
                         (f.RequesterId == partnerId && f.AddresseeId == userId))
                    ).AnyAsync();
                    if (!areFriends)
                        throw new HubException("This user only accepts messages from friends");
                    break;
            }
        }

        var dm = new DirectMessage
        {
            ConversationId = conversationId,
            SenderId = userId,
            SenderUsername = username,
            Content = content.Trim()
        };

        await messages.InsertOneAsync(dm);

        var isP1 = conv.Participant1Id == userId;
        var convUpdate = Builders<Conversation>.Update
            .Set(c => c.LastMessageContent, dm.Content.Length > 100 ? dm.Content[..100] : dm.Content)
            .Set(c => c.LastMessageSenderId, userId)
            .Set(c => c.LastMessageAt, dm.CreatedAt)
            .Set(c => c.UpdatedAt, DateTime.UtcNow)
            .Inc(isP1 ? c => c.Participant2UnreadCount : c => c.Participant1UnreadCount, 1);

        await conversations.UpdateOneAsync(c => c.Id == conv.Id, convUpdate);

        var response = new DirectMessageResponse
        {
            Id = dm.Id,
            ConversationId = dm.ConversationId,
            SenderId = dm.SenderId,
            SenderUsername = dm.SenderUsername,
            Content = dm.Content,
            IsRead = false,
            CreatedAt = dm.CreatedAt
        };

        await Clients.Group($"dm:{conversationId}").ReceiveDirectMessage(response);

        await _notificationService.CreateAsync(
            partnerId,
            NotificationType.DirectMessage,
            $"New message from {username}",
            dm.Content.Length > 100 ? dm.Content[..100] + "..." : dm.Content,
            new Dictionary<string, string>
            {
                { "conversationId", conversationId },
                { "senderId", userId }
            });
    }

    public async Task MarkAsRead(string conversationId)
    {
        var userId = GetUserId();
        var conversations = _mongoContext.GetCollection<Conversation>(CollectionNames.Conversations);
        var messages = _mongoContext.GetCollection<DirectMessage>(CollectionNames.DirectMessages);

        var conv = await conversations.Find(c => c.Id == conversationId).FirstOrDefaultAsync();
        if (conv is null) return;
        if (conv.Participant1Id != userId && conv.Participant2Id != userId) return;

        var partnerId = conv.Participant1Id == userId ? conv.Participant2Id : conv.Participant1Id;
        if (await IsBlocked(userId, partnerId)) return;

        var isP1 = conv.Participant1Id == userId;
        await conversations.UpdateOneAsync(
            c => c.Id == conv.Id,
            Builders<Conversation>.Update.Set(
                isP1 ? c => c.Participant1UnreadCount : c => c.Participant2UnreadCount, 0));

        var msgFilter = Builders<DirectMessage>.Filter.And(
            Builders<DirectMessage>.Filter.Eq(m => m.ConversationId, conversationId),
            Builders<DirectMessage>.Filter.Ne(m => m.SenderId, userId),
            Builders<DirectMessage>.Filter.Eq(m => m.IsRead, false));

        await messages.UpdateManyAsync(
            msgFilter,
            Builders<DirectMessage>.Update
                .Set(m => m.IsRead, true)
                .Set(m => m.ReadAt, DateTime.UtcNow));

        await Clients.Group($"dm:{conversationId}").MessagesRead(conversationId, userId);
    }

    public async Task StartTyping(string conversationId)
    {
        var userId = GetUserId();
        var partnerId = await GetConversationPartnerId(conversationId, userId);
        if (partnerId is null || await IsBlocked(userId, partnerId)) return;

        await Clients.OthersInGroup($"dm:{conversationId}")
            .UserTypingDM(conversationId, userId, GetUsername());
    }

    public async Task StopTyping(string conversationId)
    {
        var userId = GetUserId();
        var partnerId = await GetConversationPartnerId(conversationId, userId);
        if (partnerId is null || await IsBlocked(userId, partnerId)) return;

        await Clients.OthersInGroup($"dm:{conversationId}")
            .UserStoppedTypingDM(conversationId, userId);
    }

    private async Task<bool> IsBlocked(string userId1, string userId2)
    {
        var blocks = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);
        return await blocks.Find(b =>
            (b.BlockerId == userId1 && b.BlockedUserId == userId2) ||
            (b.BlockerId == userId2 && b.BlockedUserId == userId1)
        ).AnyAsync();
    }

    private async Task<string?> GetConversationPartnerId(string conversationId, string userId)
    {
        var conversations = _mongoContext.GetCollection<Conversation>(CollectionNames.Conversations);
        var conv = await conversations.Find(c => c.Id == conversationId).FirstOrDefaultAsync();
        if (conv is null) return null;
        if (conv.Participant1Id != userId && conv.Participant2Id != userId) return null;
        return conv.Participant1Id == userId ? conv.Participant2Id : conv.Participant1Id;
    }

    private async Task<bool> ShouldBroadcastPresence(string userId)
    {
        var settingsCol = _mongoContext.GetCollection<UserSettings>(CollectionNames.UserSettings);
        var settings = await settingsCol.Find(s => s.UserId == userId).FirstOrDefaultAsync();
        return settings?.ShowOnlineStatus ?? true;
    }

    private string GetUserId()
    {
        return Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? throw new HubException("User identity not found");
    }

    private string GetUsername()
    {
        return Context.User?.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
               ?? Context.User?.FindFirstValue(ClaimTypes.Name)
               ?? "Unknown";
    }
}
