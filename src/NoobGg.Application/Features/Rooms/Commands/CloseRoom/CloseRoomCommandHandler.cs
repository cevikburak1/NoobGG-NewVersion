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

    public CloseRoomCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CloseRoomCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var callerId = _currentUser.UserId;
        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);

        var room = await rooms.Find(r => r.Id == request.RoomId).FirstOrDefaultAsync(ct);
        if (room is null)
            return Result.Fail("Room not found", 404);

        if (room.CreatorId != callerId)
            return Result.Fail("Only the room owner can close the room", 403);

        if (room.Status == RoomStatus.Closed)
            return Result.Fail("Room is already closed");

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
}
