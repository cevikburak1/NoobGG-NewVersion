using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guilds.Commands.RejectJoinRequest;

public class RejectJoinRequestCommandHandler : IRequestHandler<RejectJoinRequestCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notificationService;

    public RejectJoinRequestCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        INotificationService notificationService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result> Handle(RejectJoinRequestCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var joinRequests = _mongoContext.GetCollection<GuildJoinRequest>(CollectionNames.GuildJoinRequests);
        var guilds = _mongoContext.GetCollection<Guild>(CollectionNames.Guilds);
        var guildMembers = _mongoContext.GetCollection<GuildMember>(CollectionNames.GuildMembers);

        var joinRequest = await joinRequests
            .Find(r => r.Id == request.JoinRequestId && r.Status == GuildJoinRequestStatus.Pending)
            .FirstOrDefaultAsync(ct);
        if (joinRequest is null)
            return Result.Fail("Join request not found or already processed", 404);

        var guild = await guilds.Find(g => g.Id == joinRequest.GuildId).FirstOrDefaultAsync(ct);
        if (guild is null)
            return Result.Fail("Guild not found", 404);

        var currentMember = await guildMembers
            .Find(m => m.GuildId == guild.Id && m.UserId == userId)
            .FirstOrDefaultAsync(ct);
        if (currentMember is null || (currentMember.Role != GuildMemberRole.Owner && currentMember.Role != GuildMemberRole.Admin))
            return Result.Fail("Only guild owners and admins can reject join requests");

        await joinRequests.UpdateOneAsync(
            Builders<GuildJoinRequest>.Filter.Eq(r => r.Id, joinRequest.Id),
            Builders<GuildJoinRequest>.Update
                .Set(r => r.Status, GuildJoinRequestStatus.Rejected)
                .Set(r => r.ReviewedBy, userId)
                .Set(r => r.ReviewedAt, DateTime.UtcNow),
            cancellationToken: ct);

        await _notificationService.CreateAsync(
            joinRequest.UserId,
            NotificationType.GuildJoinRequestRejected,
            "Join request declined",
            $"Your request to join \"{guild.Name}\" has been declined.",
            new Dictionary<string, string> { { "guildId", guild.Id } },
            ct);

        return Result.Success();
    }
}
