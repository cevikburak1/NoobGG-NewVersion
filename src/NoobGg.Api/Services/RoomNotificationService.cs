using Microsoft.AspNetCore.SignalR;
using NoobGg.Api.Hubs;
using NoobGg.Api.Hubs.Contracts;
using NoobGg.Application.Common.Interfaces;

namespace NoobGg.Api.Services;

public class RoomNotificationService : IRoomNotificationService
{
    private readonly IHubContext<ChatHub, IChatClient> _chatHubContext;
    private readonly IHubContext<RoomHub, IRoomClient> _roomHubContext;

    public RoomNotificationService(
        IHubContext<ChatHub, IChatClient> chatHubContext,
        IHubContext<RoomHub, IRoomClient> roomHubContext)
    {
        _chatHubContext = chatHubContext;
        _roomHubContext = roomHubContext;
    }

    public async Task NotifyMemberJoinedAsync(string roomId, string userId, string username, CancellationToken ct = default)
    {
        await _chatHubContext.Clients.Group(roomId).RoomMemberJoined(new RoomMemberEvent
        {
            RoomId = roomId,
            UserId = userId,
            Username = username,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyMemberLeftAsync(string roomId, string userId, string username, CancellationToken ct = default)
    {
        await _chatHubContext.Clients.Group(roomId).RoomMemberLeft(new RoomMemberEvent
        {
            RoomId = roomId,
            UserId = userId,
            Username = username,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyRoomClosedAsync(string roomId, CancellationToken ct = default)
    {
        await _chatHubContext.Clients.Group(roomId).RoomClosed(new RoomClosedEvent
        {
            RoomId = roomId,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyRoomListChangedAsync(CancellationToken ct = default)
    {
        await _roomHubContext.Clients.All.RoomListUpdated();
    }
}
