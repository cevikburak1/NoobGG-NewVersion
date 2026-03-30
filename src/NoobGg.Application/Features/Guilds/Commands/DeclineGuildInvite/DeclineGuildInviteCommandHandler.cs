using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guilds.Commands.DeclineGuildInvite;

public class DeclineGuildInviteCommandHandler : IRequestHandler<DeclineGuildInviteCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public DeclineGuildInviteCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeclineGuildInviteCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var invites = _mongoContext.GetCollection<GuildInvite>(CollectionNames.GuildInvites);

        var invite = await invites.Find(i => i.Id == request.InviteId).FirstOrDefaultAsync(ct);
        if (invite is null)
            return Result.Fail("Invite not found", 404);

        if (invite.InvitedUserId != userId)
            return Result.Fail("This invite is not for you", 403);

        if (invite.Status != GuildInviteStatus.Pending)
            return Result.Fail("This invite has already been responded to");

        await invites.UpdateOneAsync(
            Builders<GuildInvite>.Filter.Eq(i => i.Id, request.InviteId),
            Builders<GuildInvite>.Update
                .Set(i => i.Status, GuildInviteStatus.Declined)
                .Set(i => i.RespondedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return Result.Success();
    }
}
