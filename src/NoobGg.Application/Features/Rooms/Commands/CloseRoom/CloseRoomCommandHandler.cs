using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Rooms.Commands.CloseRoom;

public class CloseRoomCommandHandler : IRequestHandler<CloseRoomCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IRoomNotificationService _roomNotification;
    private readonly INotificationService _notificationService;

    public CloseRoomCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        IRoomNotificationService roomNotification,
        INotificationService notificationService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _roomNotification = roomNotification;
        _notificationService = notificationService;
    }

    public async Task<Result> Handle(CloseRoomCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var callerId = _currentUser.UserId;
        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        var messages = _mongoContext.GetCollection<Message>(CollectionNames.Messages);

        var room = await rooms.Find(r => r.Id == request.RoomId).FirstOrDefaultAsync(ct);
        if (room is null)
            return Result.Fail("Room not found", 404);

        if (room.CreatorId != callerId)
            return Result.Fail("Only the room owner can close the room", 403);

        var members = await roomMembers
            .Find(m => m.RoomId == request.RoomId)
            .ToListAsync(ct);

        var sessions = _mongoContext.GetCollection<GameSession>(CollectionNames.GameSessions);
        if (members.Count > 1)
        {
            await sessions.UpdateOneAsync(s => s.RoomId == room.Id,
                Builders<GameSession>.Update.SetOnInsert(s => s.Id, Guid.NewGuid().ToString())
                    .SetOnInsert(s => s.RoomId, room.Id)
                    .SetOnInsert(s => s.GameId, room.GameId)
                    .SetOnInsert(s => s.RoomTitle, room.Title)
                    .SetOnInsert(s => s.OwnerId, callerId)
                    .SetOnInsert(s => s.Region, room.Region)
                    .SetOnInsert(s => s.Language, room.Language)
                    .SetOnInsert(s => s.ParticipantIds, members.Select(m => m.UserId).Distinct().ToList())
                    .SetOnInsert(s => s.EndedAt, DateTime.UtcNow),
                new UpdateOptions { IsUpsert = true }, ct);
        }

        try { await _roomNotification.NotifyRoomClosedAsync(request.RoomId, ct); }
        catch { /* Persisted closure must not depend on real-time delivery. */ }

        foreach (var m in members)
        {
            if (m.UserId == callerId) continue;
            try
            {
                await _notificationService.CreateAsync(
                    m.UserId, NotificationType.RoomClosed, "Removed from room",
                    $"You were removed from \"{room.Title}\" because it was closed by the owner",
                    new Dictionary<string, string> { { "roomId", request.RoomId } }, ct);
            }
            catch { /* Continue closing the room if a notification fails. */ }
        }

        var roomFilter = Builders<RoomMember>.Filter.Eq(m => m.RoomId, request.RoomId);
        var msgFilter = Builders<Message>.Filter.Eq(m => m.RoomId, request.RoomId);

        await Task.WhenAll(
            roomMembers.DeleteManyAsync(roomFilter, ct),
            messages.DeleteManyAsync(msgFilter, ct),
            rooms.DeleteOneAsync(r => r.Id == request.RoomId, ct)
        );

        try { await _roomNotification.NotifyRoomListChangedAsync(ct); }
        catch { /* Clients will refresh the room list on their next request. */ }

        return Result.Success();
    }
}
