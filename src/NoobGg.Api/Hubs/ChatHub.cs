using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using NoobGg.Api.Hubs.Contracts;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Features.Chat.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Api.Hubs;

[Authorize]
public class ChatHub : Hub<IChatClient>
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IMongoContext _mongoContext;
    private readonly IChatPresenceService _presenceService;

    private const int MaxMessageLength = 2000;

    public ChatHub(
        ILogger<ChatHub> logger,
        IMongoContext mongoContext,
        IChatPresenceService presenceService)
    {
        _logger = logger;
        _mongoContext = mongoContext;
        _presenceService = presenceService;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("User {UserId} connected to ChatHub (conn: {ConnId})",
            GetUserId(), Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        var username = GetUsername();
        var connectionId = Context.ConnectionId;

        _logger.LogInformation("User {UserId} disconnected from ChatHub (conn: {ConnId})", userId, connectionId);

        var rooms = await _presenceService.GetUserRoomsAsync(userId);

        foreach (var roomId in rooms)
        {
            await Groups.RemoveFromGroupAsync(connectionId, roomId);

            bool fullyLeft = await _presenceService.TrackUserLeftRoomAsync(roomId, userId, connectionId);

            if (fullyLeft)
            {
                await Clients.Group(roomId).UserLeft(new ChatPresenceEvent
                {
                    UserId = userId,
                    Username = username,
                    RoomId = roomId,
                    Timestamp = DateTime.UtcNow
                });

                await BroadcastRoomPresenceAsync(roomId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRoom(string roomId)
    {
        var userId = GetUserId();
        var username = GetUsername();

        if (!await IsRoomMemberAsync(roomId, userId))
        {
            await Clients.Caller.ReceiveMessage(SystemMessage(roomId, "You are not a member of this room."));
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await _presenceService.TrackUserJoinedRoomAsync(roomId, userId, username, Context.ConnectionId);

        await Clients.Group(roomId).UserJoined(new ChatPresenceEvent
        {
            UserId = userId,
            Username = username,
            RoomId = roomId,
            Timestamp = DateTime.UtcNow
        });

        await BroadcastRoomPresenceAsync(roomId);

        _logger.LogInformation("User {UserId} joined chat for room {RoomId} (conn: {ConnId})",
            userId, roomId, Context.ConnectionId);
    }

    public async Task LeaveRoom(string roomId)
    {
        var userId = GetUserId();
        var username = GetUsername();

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        bool fullyLeft = await _presenceService.TrackUserLeftRoomAsync(roomId, userId, Context.ConnectionId);

        if (fullyLeft)
        {
            await Clients.Group(roomId).UserLeft(new ChatPresenceEvent
            {
                UserId = userId,
                Username = username,
                RoomId = roomId,
                Timestamp = DateTime.UtcNow
            });

            await BroadcastRoomPresenceAsync(roomId);
        }

        _logger.LogInformation("User {UserId} left chat for room {RoomId} (conn: {ConnId})",
            userId, roomId, Context.ConnectionId);
    }

    public async Task SendMessage(string roomId, string content)
    {
        var userId = GetUserId();
        var username = GetUsername();

        if (string.IsNullOrWhiteSpace(content))
            return;

        if (content.Length > MaxMessageLength)
            throw new HubException($"Message exceeds maximum length of {MaxMessageLength} characters");

        if (!await IsRoomMemberAsync(roomId, userId))
        {
            await Clients.Caller.ReceiveMessage(SystemMessage(roomId, "You are not a member of this room."));
            return;
        }

        var message = new Message
        {
            RoomId = roomId,
            SenderId = userId,
            SenderUsername = username,
            Content = content.Trim(),
            Type = MessageType.Text,
            CreatedAt = DateTime.UtcNow
        };

        var collection = _mongoContext.GetCollection<Message>(CollectionNames.Messages);
        await collection.InsertOneAsync(message);

        var profilesCol = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var senderProfile = await profilesCol.Find(p => p.UserId == userId).FirstOrDefaultAsync();

        var response = new ChatMessageResponse
        {
            Id = message.Id,
            RoomId = message.RoomId,
            SenderId = message.SenderId,
            SenderUsername = message.SenderUsername,
            SenderAvatarUrl = senderProfile?.AvatarUrl,
            Content = message.Content,
            Type = message.Type,
            IsEdited = false,
            CreatedAt = message.CreatedAt
        };

        await Clients.Group(roomId).ReceiveMessage(response);

        _logger.LogDebug("Message sent by {UserId} in room {RoomId}", userId, roomId);
    }

    public async Task StartTyping(string roomId)
    {
        var userId = GetUserId();
        var username = GetUsername();

        await Clients.OthersInGroup(roomId).UserStartedTyping(new TypingEvent
        {
            UserId = userId,
            Username = username,
            RoomId = roomId
        });
    }

    public async Task StopTyping(string roomId)
    {
        var userId = GetUserId();
        var username = GetUsername();

        await Clients.OthersInGroup(roomId).UserStoppedTyping(new TypingEvent
        {
            UserId = userId,
            Username = username,
            RoomId = roomId
        });
    }

    private async Task<bool> IsRoomMemberAsync(string roomId, string userId)
    {
        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        return await roomMembers
            .Find(m => m.RoomId == roomId && m.UserId == userId)
            .AnyAsync();
    }

    private async Task BroadcastRoomPresenceAsync(string roomId)
    {
        var onlineUsers = await _presenceService.GetOnlineUsersInRoomAsync(roomId);

        var userIds = onlineUsers.Select(u => u.UserId).ToList();
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var onlineProfiles = await profiles
            .Find(p => userIds.Contains(p.UserId))
            .ToListAsync();
        var avatarMap = onlineProfiles.ToDictionary(p => p.UserId, p => p.AvatarUrl);

        var response = new RoomPresenceResponse
        {
            RoomId = roomId,
            OnlineUsers = onlineUsers.Select(u => new OnlineUserInfo
            {
                UserId = u.UserId,
                Username = u.Username,
                AvatarUrl = avatarMap.GetValueOrDefault(u.UserId)
            }).ToList(),
            OnlineCount = onlineUsers.Count
        };

        await Clients.Group(roomId).RoomPresenceUpdated(response);
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

    private static ChatMessageResponse SystemMessage(string roomId, string content) => new()
    {
        Id = Guid.NewGuid().ToString(),
        RoomId = roomId,
        SenderId = "system",
        SenderUsername = "System",
        Content = content,
        Type = MessageType.System,
        CreatedAt = DateTime.UtcNow
    };
}
