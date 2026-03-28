using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Blocks.Commands.BlockUser;

public class BlockUserCommandHandler : IRequestHandler<BlockUserCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IRoomNotificationService _roomNotification;
    private readonly INotificationService _notificationService;

    public BlockUserCommandHandler(
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

    public async Task<Result> Handle(BlockUserCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;

        if (userId == request.BlockedUserId)
            return Result.Fail("You cannot block yourself");

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var targetExists = await users.Find(u => u.Id == request.BlockedUserId).AnyAsync(ct);
        if (!targetExists)
            return Result.Fail("User not found", 404);

        var blocks = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);

        var block = new Block
        {
            BlockerId = userId,
            BlockedUserId = request.BlockedUserId
        };

        try
        {
            await blocks.InsertOneAsync(block, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return Result.Fail("User is already blocked");
        }

        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);

        var myRooms = await rooms.Find(r =>
            r.CreatorId == userId && r.Status != RoomStatus.Closed
        ).ToListAsync(ct);

        foreach (var room in myRooms)
        {
            var memberFilter = Builders<RoomMember>.Filter.And(
                Builders<RoomMember>.Filter.Eq(m => m.RoomId, room.Id),
                Builders<RoomMember>.Filter.Eq(m => m.UserId, request.BlockedUserId));

            var deleted = await roomMembers.DeleteOneAsync(memberFilter, ct);
            if (deleted.DeletedCount > 0)
            {
                await rooms.UpdateOneAsync(
                    Builders<Room>.Filter.Eq(r => r.Id, room.Id),
                    Builders<Room>.Update
                        .Inc(r => r.CurrentMemberCount, -1)
                        .Set(r => r.UpdatedAt, DateTime.UtcNow),
                    cancellationToken: ct);

                await _roomNotification.NotifyMemberLeftAsync(room.Id, request.BlockedUserId, "blocked user", ct);
            }
        }

        await _roomNotification.NotifyRoomListChangedAsync(ct);

        await _notificationService.SendBlockListChangedAsync(userId, request.BlockedUserId, true, ct);

        var audits = _mongoContext.GetCollection<Audit>(CollectionNames.Audits);
        await audits.InsertOneAsync(new Audit
        {
            ActorId = userId,
            Action = AuditAction.UserBlocked,
            TargetType = "User",
            TargetId = request.BlockedUserId
        }, cancellationToken: ct);

        return Result.Success();
    }
}
