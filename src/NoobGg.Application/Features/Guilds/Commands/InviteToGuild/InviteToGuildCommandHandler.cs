using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guilds.Commands.InviteToGuild;

public class InviteToGuildCommandHandler : IRequestHandler<InviteToGuildCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notificationService;

    public InviteToGuildCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        INotificationService notificationService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result> Handle(InviteToGuildCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var inviterId = _currentUser.UserId;

        if (inviterId == request.InvitedUserId)
            return Result.Fail("You cannot invite yourself");

        var guilds = _mongoContext.GetCollection<Guild>(CollectionNames.Guilds);
        var guild = await guilds.Find(g => g.Id == request.GuildId).FirstOrDefaultAsync(ct);
        if (guild is null)
            return Result.Fail("Guild not found", 404);

        var guildMembers = _mongoContext.GetCollection<GuildMember>(CollectionNames.GuildMembers);

        var callerMembership = await guildMembers.Find(m =>
            m.GuildId == request.GuildId && m.UserId == inviterId).FirstOrDefaultAsync(ct);

        if (callerMembership is null)
            return Result.Fail("You must be a guild member to invite players");

        if (callerMembership.Role == GuildMemberRole.Member)
            return Result.Fail("Only guild owners and admins can send invites", 403);

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var targetExists = await users.Find(u => u.Id == request.InvitedUserId).AnyAsync(ct);
        if (!targetExists)
            return Result.Fail("User not found", 404);

        var isAlreadyMember = await guildMembers
            .Find(m => m.GuildId == request.GuildId && m.UserId == request.InvitedUserId)
            .AnyAsync(ct);

        if (isAlreadyMember)
            return Result.Fail("User is already a member of this guild");

        var invites = _mongoContext.GetCollection<GuildInvite>(CollectionNames.GuildInvites);
        var hasPending = await invites.Find(i =>
            i.GuildId == request.GuildId &&
            i.InvitedUserId == request.InvitedUserId &&
            i.Status == GuildInviteStatus.Pending
        ).AnyAsync(ct);

        if (hasPending)
            return Result.Fail("An invite is already pending for this user");

        var invite = new GuildInvite
        {
            GuildId = request.GuildId,
            InviterId = inviterId,
            InvitedUserId = request.InvitedUserId
        };

        await invites.InsertOneAsync(invite, cancellationToken: ct);

        var inviterName = _currentUser.Username ?? "Someone";
        await _notificationService.CreateAsync(
            request.InvitedUserId,
            NotificationType.GuildInvite,
            "Guild Invite",
            $"{inviterName} invited you to guild \"{guild.Name}\"",
            new Dictionary<string, string>
            {
                { "guildId", request.GuildId },
                { "inviteId", invite.Id },
                { "inviterId", inviterId }
            },
            ct);

        return Result.Success();
    }
}
