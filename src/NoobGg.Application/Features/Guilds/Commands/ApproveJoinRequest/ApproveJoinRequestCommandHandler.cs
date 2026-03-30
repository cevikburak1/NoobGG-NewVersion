using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guilds.Commands.ApproveJoinRequest;

public class ApproveJoinRequestCommandHandler : IRequestHandler<ApproveJoinRequestCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notificationService;

    public ApproveJoinRequestCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        INotificationService notificationService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result> Handle(ApproveJoinRequestCommand request, CancellationToken ct)
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
            return Result.Fail("Only guild owners and admins can approve join requests");

        if (guild.CurrentMemberCount >= guild.MaxMembers)
            return Result.Fail("Guild is full");

        var updateFilter = Builders<Guild>.Filter.And(
            Builders<Guild>.Filter.Eq(g => g.Id, guild.Id),
            Builders<Guild>.Filter.Where(g => g.CurrentMemberCount < g.MaxMembers));

        var updateDef = Builders<Guild>.Update
            .Inc(g => g.CurrentMemberCount, 1)
            .Set(g => g.UpdatedAt, DateTime.UtcNow);

        var updatedGuild = await guilds.FindOneAndUpdateAsync(
            updateFilter, updateDef,
            new FindOneAndUpdateOptions<Guild> { ReturnDocument = ReturnDocument.After },
            ct);

        if (updatedGuild is null)
            return Result.Fail("Guild is full or no longer accepting members");

        var member = new GuildMember
        {
            GuildId = guild.Id,
            UserId = joinRequest.UserId,
            Role = GuildMemberRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        try
        {
            await guildMembers.InsertOneAsync(member, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            await guilds.UpdateOneAsync(
                Builders<Guild>.Filter.Eq(g => g.Id, guild.Id),
                Builders<Guild>.Update.Inc(g => g.CurrentMemberCount, -1),
                cancellationToken: ct);

            return Result.Fail("User is already a member of this guild");
        }

        await joinRequests.UpdateOneAsync(
            Builders<GuildJoinRequest>.Filter.Eq(r => r.Id, joinRequest.Id),
            Builders<GuildJoinRequest>.Update
                .Set(r => r.Status, GuildJoinRequestStatus.Approved)
                .Set(r => r.ReviewedBy, userId)
                .Set(r => r.ReviewedAt, DateTime.UtcNow),
            cancellationToken: ct);

        await _notificationService.CreateAsync(
            joinRequest.UserId,
            NotificationType.GuildJoinRequestApproved,
            "Join request approved",
            $"Your request to join \"{guild.Name}\" has been approved!",
            new Dictionary<string, string> { { "guildId", guild.Id } },
            ct);

        return Result.Success();
    }
}
