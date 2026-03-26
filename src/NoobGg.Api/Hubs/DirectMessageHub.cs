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
            await Clients.Others.PresenceChanged(userId, false);
        }

        _logger.LogInformation("User {UserId} disconnected from DirectMessageHub", userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"dm:{conversationId}");
    }

    public async Task SendDirectMessage(string conversationId, string content)
    {
        var userId = GetUserId();
        var username = GetUsername();

        if (string.IsNullOrWhiteSpace(content)) return;

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
        if (isBlocked) return;

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
        await Clients.OthersInGroup($"dm:{conversationId}")
            .UserTypingDM(conversationId, GetUserId(), GetUsername());
    }

    public async Task StopTyping(string conversationId)
    {
        await Clients.OthersInGroup($"dm:{conversationId}")
            .UserStoppedTypingDM(conversationId, GetUserId());
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
