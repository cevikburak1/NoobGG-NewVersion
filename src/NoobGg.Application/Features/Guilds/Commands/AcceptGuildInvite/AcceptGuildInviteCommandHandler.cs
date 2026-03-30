using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guilds.Commands.AcceptGuildInvite;

public class AcceptGuildInviteCommandHandler : IRequestHandler<AcceptGuildInviteCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public AcceptGuildInviteCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(AcceptGuildInviteCommand request, CancellationToken ct)
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

        var guilds = _mongoContext.GetCollection<Guild>(CollectionNames.Guilds);
        var guild = await guilds.Find(g => g.Id == invite.GuildId).FirstOrDefaultAsync(ct);
        if (guild is null)
            return Result.Fail("Guild no longer exists", 404);

        if (guild.CurrentMemberCount >= guild.MaxMembers)
            return Result.Fail("Guild is full");

        var guildMembers = _mongoContext.GetCollection<GuildMember>(CollectionNames.GuildMembers);

        var isAlreadyMember = await guildMembers
            .Find(m => m.GuildId == invite.GuildId && m.UserId == userId)
            .AnyAsync(ct);

        if (isAlreadyMember)
        {
            await invites.UpdateOneAsync(
                Builders<GuildInvite>.Filter.Eq(i => i.Id, request.InviteId),
                Builders<GuildInvite>.Update
                    .Set(i => i.Status, GuildInviteStatus.Accepted)
                    .Set(i => i.RespondedAt, DateTime.UtcNow),
                cancellationToken: ct);
            return Result.Fail("You are already a member of this guild");
        }

        var member = new GuildMember
        {
            GuildId = invite.GuildId,
            UserId = userId,
            Role = GuildMemberRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        await guildMembers.InsertOneAsync(member, cancellationToken: ct);

        await guilds.UpdateOneAsync(
            Builders<Guild>.Filter.Eq(g => g.Id, invite.GuildId),
            Builders<Guild>.Update
                .Inc(g => g.CurrentMemberCount, 1)
                .Set(g => g.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        await invites.UpdateOneAsync(
            Builders<GuildInvite>.Filter.Eq(i => i.Id, request.InviteId),
            Builders<GuildInvite>.Update
                .Set(i => i.Status, GuildInviteStatus.Accepted)
                .Set(i => i.RespondedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return Result.Success();
    }
}
