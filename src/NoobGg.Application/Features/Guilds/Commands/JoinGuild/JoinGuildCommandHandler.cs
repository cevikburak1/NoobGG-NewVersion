using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guilds.Commands.JoinGuild;

public class JoinGuildCommandHandler : IRequestHandler<JoinGuildCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notificationService;

    public JoinGuildCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        INotificationService notificationService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result> Handle(JoinGuildCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var guilds = _mongoContext.GetCollection<Guild>(CollectionNames.Guilds);
        var guildMembers = _mongoContext.GetCollection<GuildMember>(CollectionNames.GuildMembers);
        var joinRequests = _mongoContext.GetCollection<GuildJoinRequest>(CollectionNames.GuildJoinRequests);

        var guild = await guilds.Find(g => g.Id == request.GuildId).FirstOrDefaultAsync(ct);
        if (guild is null)
            return Result.Fail("Guild not found", 404);

        if (!guild.IsPublic)
            return Result.Fail("This guild is private. You need an invitation to join.");

        if (guild.CurrentMemberCount >= guild.MaxMembers)
            return Result.Fail("Guild is full");

        var alreadyMember = await guildMembers
            .Find(m => m.GuildId == request.GuildId && m.UserId == userId)
            .AnyAsync(ct);
        if (alreadyMember)
            return Result.Fail("You are already a member of this guild");

        var pendingRequest = await joinRequests
            .Find(r => r.GuildId == request.GuildId
                        && r.UserId == userId
                        && r.Status == GuildJoinRequestStatus.Pending)
            .AnyAsync(ct);
        if (pendingRequest)
            return Result.Fail("You already have a pending join request for this guild");

        var joinRequest = new GuildJoinRequest
        {
            GuildId = request.GuildId,
            UserId = userId,
            Message = request.Message,
            Status = GuildJoinRequestStatus.Pending
        };

        await joinRequests.InsertOneAsync(joinRequest, cancellationToken: ct);

        var username = _currentUser.Username ?? "Unknown";
        await _notificationService.CreateAsync(
            guild.CreatorId,
            NotificationType.GuildJoinRequestReceived,
            "New join request",
            $"{username} wants to join your guild \"{guild.Name}\"",
            new Dictionary<string, string>
            {
                { "guildId", request.GuildId },
                { "joinRequestId", joinRequest.Id }
            },
            ct);

        return Result.Success();
    }
}
