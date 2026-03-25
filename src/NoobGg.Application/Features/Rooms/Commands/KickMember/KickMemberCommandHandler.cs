using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Rooms.Commands.KickMember;

public class KickMemberCommandHandler : IRequestHandler<KickMemberCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public KickMemberCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(KickMemberCommand request, CancellationToken ct)
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
            return Result.Fail("Only the room owner can kick members", 403);

        if (room.Status == RoomStatus.Closed)
            return Result.Fail("Room is already closed");

        if (request.UserId == callerId)
            return Result.Fail("You cannot kick yourself. Use leave instead.");

        var targetMembership = await roomMembers.Find(m =>
                m.RoomId == request.RoomId && m.UserId == request.UserId)
            .FirstOrDefaultAsync(ct);

        if (targetMembership is null)
            return Result.Fail("User is not a member of this room", 404);

        await roomMembers.DeleteOneAsync(
            Builders<RoomMember>.Filter.Eq(m => m.Id, targetMembership.Id),
            ct);

        var updateDef = Builders<Room>.Update
            .Inc(r => r.CurrentMemberCount, -1)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);

        var updatedRoom = await rooms.FindOneAndUpdateAsync(
            Builders<Room>.Filter.Eq(r => r.Id, request.RoomId),
            updateDef,
            new FindOneAndUpdateOptions<Room> { ReturnDocument = ReturnDocument.After },
            ct);

        if (updatedRoom is not null && updatedRoom.Status == RoomStatus.Full && updatedRoom.CurrentMemberCount < updatedRoom.MaxMembers)
        {
            await rooms.UpdateOneAsync(
                Builders<Room>.Filter.Eq(r => r.Id, request.RoomId),
                Builders<Room>.Update.Set(r => r.Status, RoomStatus.Open),
                cancellationToken: ct);
        }

        return Result.Success();
    }
}
