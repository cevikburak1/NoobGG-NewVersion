using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guilds.Commands.KickGuildMember;

public class KickGuildMemberCommandHandler : IRequestHandler<KickGuildMemberCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public KickGuildMemberCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(KickGuildMemberCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var callerId = _currentUser.UserId;
        var guilds = _mongoContext.GetCollection<Guild>(CollectionNames.Guilds);
        var guildMembers = _mongoContext.GetCollection<GuildMember>(CollectionNames.GuildMembers);

        var callerMembership = await guildMembers.Find(m =>
            m.GuildId == request.GuildId && m.UserId == callerId).FirstOrDefaultAsync(ct);

        if (callerMembership is null)
            return Result.Fail("You are not a member of this guild", 403);

        if (callerMembership.Role != GuildMemberRole.Owner && callerMembership.Role != GuildMemberRole.Admin)
            return Result.Fail("Only guild owners and admins can kick members", 403);

        if (request.UserId == callerId)
            return Result.Fail("You cannot kick yourself. Use leave instead.");

        var targetMembership = await guildMembers.Find(m =>
            m.GuildId == request.GuildId && m.UserId == request.UserId).FirstOrDefaultAsync(ct);

        if (targetMembership is null)
            return Result.Fail("User is not a member of this guild", 404);

        if (targetMembership.Role == GuildMemberRole.Owner)
            return Result.Fail("Cannot kick the guild owner");

        if (callerMembership.Role == GuildMemberRole.Admin && targetMembership.Role == GuildMemberRole.Admin)
            return Result.Fail("Admins cannot kick other admins");

        await guildMembers.DeleteOneAsync(
            Builders<GuildMember>.Filter.Eq(m => m.Id, targetMembership.Id), ct);

        await guilds.UpdateOneAsync(
            Builders<Guild>.Filter.Eq(g => g.Id, request.GuildId),
            Builders<Guild>.Update
                .Inc(g => g.CurrentMemberCount, -1)
                .Set(g => g.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return Result.Success();
    }
}
