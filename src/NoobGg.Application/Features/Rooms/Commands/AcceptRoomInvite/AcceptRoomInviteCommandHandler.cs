using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Rooms.Commands.AcceptRoomInvite;

public class AcceptRoomInviteCommandHandler : IRequestHandler<AcceptRoomInviteCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IRoomNotificationService _roomNotification;
    private readonly INotificationService _notificationService;

    public AcceptRoomInviteCommandHandler(
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

    public async Task<Result> Handle(AcceptRoomInviteCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var invites = _mongoContext.GetCollection<RoomInvite>(CollectionNames.RoomInvites);

        var invite = await invites.Find(i => i.Id == request.InviteId).FirstOrDefaultAsync(ct);
        if (invite is null)
            return Result.Fail("Invite not found", 404);

        if (invite.InvitedUserId != userId)
            return Result.Fail("This invite is not for you", 403);

        if (invite.Status != RoomInviteStatus.Pending)
            return Result.Fail("This invite is no longer pending");

        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);

        // Atomic capacity check — same pattern as JoinRoomCommandHandler
        var filter = Builders<Room>.Filter.And(
            Builders<Room>.Filter.Eq(r => r.Id, invite.RoomId),
            Builders<Room>.Filter.Eq(r => r.Status, RoomStatus.Open),
            Builders<Room>.Filter.Where(r => r.CurrentMemberCount < r.MaxMembers));

        var update = Builders<Room>.Update
            .Inc(r => r.CurrentMemberCount, 1)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);

        var updatedRoom = await rooms.FindOneAndUpdateAsync(
            filter, update,
            new FindOneAndUpdateOptions<Room> { ReturnDocument = ReturnDocument.After },
            ct);

        if (updatedRoom is null)
        {
            var room = await rooms.Find(r => r.Id == invite.RoomId).FirstOrDefaultAsync(ct);
            if (room is null)
                return Result.Fail("Room no longer exists", 404);

            return Result.Fail("Room is full or no longer accepting members");
        }

        var member = new RoomMember
        {
            RoomId = invite.RoomId,
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
            await rooms.UpdateOneAsync(
                Builders<Room>.Filter.Eq(r => r.Id, invite.RoomId),
                Builders<Room>.Update.Inc(r => r.CurrentMemberCount, -1),
                cancellationToken: ct);

            // Mark invite accepted anyway since user is already a member
            await invites.UpdateOneAsync(
                Builders<RoomInvite>.Filter.Eq(i => i.Id, invite.Id),
                Builders<RoomInvite>.Update
                    .Set(i => i.Status, RoomInviteStatus.Accepted)
                    .Set(i => i.RespondedAt, DateTime.UtcNow),
                cancellationToken: ct);

            return Result.Fail("You are already a member of this room");
        }

        // Mark invite as accepted
        await invites.UpdateOneAsync(
            Builders<RoomInvite>.Filter.Eq(i => i.Id, invite.Id),
            Builders<RoomInvite>.Update
                .Set(i => i.Status, RoomInviteStatus.Accepted)
                .Set(i => i.RespondedAt, DateTime.UtcNow),
            cancellationToken: ct);

        if (updatedRoom.CurrentMemberCount >= updatedRoom.MaxMembers)
        {
            await rooms.UpdateOneAsync(
                Builders<Room>.Filter.Eq(r => r.Id, invite.RoomId),
                Builders<Room>.Update.Set(r => r.Status, RoomStatus.Full),
                cancellationToken: ct);
        }

        var username = _currentUser.Username ?? "Unknown";
        await _roomNotification.NotifyMemberJoinedAsync(invite.RoomId, userId, username, ct);
        await _roomNotification.NotifyRoomListChangedAsync(ct);

        if (updatedRoom.CreatorId != userId)
        {
            await _notificationService.CreateAsync(
                updatedRoom.CreatorId,
                NotificationType.RoomJoined,
                "New member joined your room",
                $"{username} joined \"{updatedRoom.Title}\"",
                new Dictionary<string, string> { { "roomId", invite.RoomId } },
                ct);
        }

        return Result.Success();
    }
}
