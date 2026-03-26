using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Rooms.Commands.JoinRoom;

public class JoinRoomCommandHandler : IRequestHandler<JoinRoomCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IRoomNotificationService _roomNotification;
    private readonly INotificationService _notificationService;

    public JoinRoomCommandHandler(
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

    public async Task<Result> Handle(JoinRoomCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);

        // Atomic increment: only succeeds if room is open and has space
        var filter = Builders<Room>.Filter.And(
            Builders<Room>.Filter.Eq(r => r.Id, request.RoomId),
            Builders<Room>.Filter.Eq(r => r.Status, RoomStatus.Open),
            Builders<Room>.Filter.Where(r => r.CurrentMemberCount < r.MaxMembers));

        var update = Builders<Room>.Update
            .Inc(r => r.CurrentMemberCount, 1)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<Room>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updatedRoom = await rooms.FindOneAndUpdateAsync(filter, update, options, ct);

        if (updatedRoom is null)
        {
            // Distinguish between room not found vs full/closed
            var roomExists = await rooms.Find(r => r.Id == request.RoomId).AnyAsync(ct);
            if (!roomExists)
                return Result.Fail("Room not found", 404);

            return Result.Fail("Room is full or no longer accepting members");
        }

        var member = new RoomMember
        {
            RoomId = request.RoomId,
            UserId = userId,
            Role = RoomMemberRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        try
        {
            await roomMembers.InsertOneAsync(member, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Rollback the count increment — user was already a member
            await rooms.UpdateOneAsync(
                Builders<Room>.Filter.Eq(r => r.Id, request.RoomId),
                Builders<Room>.Update.Inc(r => r.CurrentMemberCount, -1),
                cancellationToken: ct);

            return Result.Fail("You are already a member of this room");
        }

        // If room is now at capacity, mark as Full
        if (updatedRoom.CurrentMemberCount >= updatedRoom.MaxMembers)
        {
            await rooms.UpdateOneAsync(
                Builders<Room>.Filter.Eq(r => r.Id, request.RoomId),
                Builders<Room>.Update.Set(r => r.Status, RoomStatus.Full),
                cancellationToken: ct);
        }

        await _roomNotification.NotifyMemberJoinedAsync(
            request.RoomId, userId, _currentUser.Username ?? "Unknown", ct);
        await _roomNotification.NotifyRoomListChangedAsync(ct);

        var username = _currentUser.Username ?? "Unknown";
        if (updatedRoom.CreatorId != userId)
        {
            await _notificationService.CreateAsync(
                updatedRoom.CreatorId,
                NotificationType.RoomJoined,
                "New member joined your room",
                $"{username} joined \"{updatedRoom.Title}\"",
                new Dictionary<string, string> { { "roomId", request.RoomId } },
                ct);
        }

        return Result.Success();
    }
}
