using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guilds.Commands.UpdateGuildMemberRole;

public class UpdateGuildMemberRoleCommandHandler : IRequestHandler<UpdateGuildMemberRoleCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public UpdateGuildMemberRoleCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateGuildMemberRoleCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var callerId = _currentUser.UserId;
        var guildMembers = _mongoContext.GetCollection<GuildMember>(CollectionNames.GuildMembers);

        var callerMembership = await guildMembers.Find(m =>
            m.GuildId == request.GuildId && m.UserId == callerId).FirstOrDefaultAsync(ct);

        if (callerMembership is null || callerMembership.Role != GuildMemberRole.Owner)
            return Result.Fail("Only the guild owner can change member roles", 403);

        if (request.UserId == callerId)
            return Result.Fail("You cannot change your own role");

        if (request.NewRole == GuildMemberRole.Owner)
            return Result.Fail("Use the transfer ownership feature to transfer ownership");

        var targetMembership = await guildMembers.Find(m =>
            m.GuildId == request.GuildId && m.UserId == request.UserId).FirstOrDefaultAsync(ct);

        if (targetMembership is null)
            return Result.Fail("User is not a member of this guild", 404);

        await guildMembers.UpdateOneAsync(
            Builders<GuildMember>.Filter.Eq(m => m.Id, targetMembership.Id),
            Builders<GuildMember>.Update
                .Set(m => m.Role, request.NewRole)
                .Set(m => m.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return Result.Success();
    }
}
