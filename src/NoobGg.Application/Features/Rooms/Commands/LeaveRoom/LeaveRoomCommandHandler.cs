using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Rooms.Commands.LeaveRoom;

public class LeaveRoomCommandHandler : IRequestHandler<LeaveRoomCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public LeaveRoomCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
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
            await rooms.UpdateOneAsync(
                Builders<Room>.Filter.Eq(r => r.Id, request.RoomId),
                Builders<Room>.Update
                    .Set(r => r.Status, RoomStatus.Closed)
                    .Set(r => r.ClosedAt, DateTime.UtcNow)
                    .Set(r => r.CurrentMemberCount, 0)
                    .Set(r => r.UpdatedAt, DateTime.UtcNow),
                cancellationToken: ct);

            await roomMembers.DeleteManyAsync(
                Builders<RoomMember>.Filter.Eq(m => m.RoomId, request.RoomId),
                ct);

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

        return Result.Success();
    }
}
