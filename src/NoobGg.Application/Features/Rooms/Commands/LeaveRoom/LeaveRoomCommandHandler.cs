using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Rooms.Helpers;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;
using RecentActivityTargetType = NoobGg.Domain.Enums.RecentActivityTargetType;

namespace NoobGg.Application.Features.Rooms.Commands.LeaveRoom;

public class LeaveRoomCommandHandler : IRequestHandler<LeaveRoomCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IRoomNotificationService _roomNotification;
    private readonly INotificationService _notificationService;
    private readonly IRecentActivityService _recentActivityService;

    public LeaveRoomCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        IRoomNotificationService roomNotification,
        INotificationService notificationService,
        IRecentActivityService recentActivityService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _roomNotification = roomNotification;
        _notificationService = notificationService;
        _recentActivityService = recentActivityService;
    }

    public async Task<Result> Handle(LeaveRoomCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);

        var membership = await roomMembers.Find(m =>
                m.RoomId == request.RoomId && m.UserId == userId)
            .FirstOrDefaultAsync(ct);

        if (membership is null)
            return Result.Fail("You are not a member of this room", 404);

        // If owner leaves, close the entire room
        if (membership.Role == RoomMemberRole.Owner)
        {
            var closingRoom = await rooms.Find(r => r.Id == request.RoomId).FirstOrDefaultAsync(ct);
            var allMembers = await roomMembers
                .Find(m => m.RoomId == request.RoomId && m.UserId != userId)
                .ToListAsync(ct);

            await _roomNotification.NotifyRoomClosedAsync(request.RoomId, ct);

            foreach (var m in allMembers)
            {
                await _notificationService.CreateAsync(
                    m.UserId,
                    NotificationType.RoomClosed,
                    "Removed from room",
                    $"You were removed from \"{closingRoom?.Title ?? "A room"}\" because it was closed by the owner",
                    new Dictionary<string, string> { { "roomId", request.RoomId } },
                    ct);
            }

            await rooms.UpdateOneAsync(
                Builders<Room>.Filter.Eq(r => r.Id, request.RoomId),
                Builders<Room>.Update
                    .Set(r => r.Status, RoomStatus.Closed)
                    .Set(r => r.ClosedAt, DateTime.UtcNow)
                    .Set(r => r.CurrentMemberCount, 0)
                    .Set(r => r.AverageElo, (int?)null)
                    .Set(r => r.AverageRankTier, (string?)null)
                    .Set(r => r.UpdatedAt, DateTime.UtcNow),
                cancellationToken: ct);

            await roomMembers.DeleteManyAsync(
                Builders<RoomMember>.Filter.Eq(m => m.RoomId, request.RoomId),
                ct);

            await _roomNotification.NotifyRoomListChangedAsync(ct);

            _ = _recentActivityService.UpsertAsync(userId, request.RoomId, RecentActivityTargetType.Room, ct);

            return Result.Success();
        }

        // Regular member leaves
        var deleteResult = await roomMembers.DeleteOneAsync(
            Builders<RoomMember>.Filter.Eq(m => m.Id, membership.Id),
            ct);

        if (deleteResult.DeletedCount == 0)
            return Result.Fail("Failed to leave room");

        // Atomic decrement and reopen if was Full
        var updateFilter = Builders<Room>.Filter.Eq(r => r.Id, request.RoomId);
        var updateDef = Builders<Room>.Update
            .Inc(r => r.CurrentMemberCount, -1)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);

        var room = await rooms.FindOneAndUpdateAsync(
            updateFilter,
            updateDef,
            new FindOneAndUpdateOptions<Room> { ReturnDocument = ReturnDocument.After },
            ct);

        if (room is not null && room.Status == RoomStatus.Full && room.CurrentMemberCount < room.MaxMembers)
        {
            await rooms.UpdateOneAsync(
                Builders<Room>.Filter.Eq(r => r.Id, request.RoomId),
                Builders<Room>.Update.Set(r => r.Status, RoomStatus.Open),
                cancellationToken: ct);
        }

        await RoomEloHelper.RecalculateAsync(_mongoContext, request.RoomId, ct);

        await _roomNotification.NotifyMemberLeftAsync(
            request.RoomId, userId, _currentUser.Username ?? "Unknown", ct);
        await _roomNotification.NotifyRoomListChangedAsync(ct);

        if (room is not null && room.CreatorId != userId)
        {
            await _notificationService.CreateAsync(
                room.CreatorId,
                NotificationType.RoomLeft,
                "Member left your room",
                $"{_currentUser.Username ?? "Someone"} left \"{room.Title}\"",
                new Dictionary<string, string> { { "roomId", request.RoomId } },
                ct);
        }

        _ = _recentActivityService.UpsertAsync(userId, request.RoomId, RecentActivityTargetType.Room, ct);

        return Result.Success();
    }
}
