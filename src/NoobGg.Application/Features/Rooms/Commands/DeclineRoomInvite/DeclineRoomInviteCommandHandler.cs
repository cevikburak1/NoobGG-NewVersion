using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Rooms.Commands.DeclineRoomInvite;

public class DeclineRoomInviteCommandHandler : IRequestHandler<DeclineRoomInviteCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public DeclineRoomInviteCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeclineRoomInviteCommand request, CancellationToken ct)
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

        await invites.UpdateOneAsync(
            Builders<RoomInvite>.Filter.Eq(i => i.Id, invite.Id),
            Builders<RoomInvite>.Update
                .Set(i => i.Status, RoomInviteStatus.Declined)
                .Set(i => i.RespondedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return Result.Success();
    }
}
