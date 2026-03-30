using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guilds.Commands.LeaveGuild;

public class LeaveGuildCommandHandler : IRequestHandler<LeaveGuildCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notificationService;

    public LeaveGuildCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        INotificationService notificationService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result> Handle(LeaveGuildCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var guildMembers = _mongoContext.GetCollection<GuildMember>(CollectionNames.GuildMembers);

        var membership = await guildMembers.Find(m =>
                m.GuildId == request.GuildId && m.UserId == userId)
            .FirstOrDefaultAsync(ct);

        if (membership is null)
            return Result.Fail("You are not a member of this guild", 404);

        if (membership.Role == GuildMemberRole.Owner)
            return Result.Fail("Guild owners cannot leave. Transfer ownership or disband the guild.");

        await guildMembers.DeleteOneAsync(
            Builders<GuildMember>.Filter.Eq(m => m.Id, membership.Id), ct);

        var guilds = _mongoContext.GetCollection<Guild>(CollectionNames.Guilds);
        await guilds.UpdateOneAsync(
            Builders<Guild>.Filter.Eq(g => g.Id, request.GuildId),
            Builders<Guild>.Update
                .Inc(g => g.CurrentMemberCount, -1)
                .Set(g => g.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        var guild = await guilds.Find(g => g.Id == request.GuildId).FirstOrDefaultAsync(ct);
        if (guild is not null)
        {
            await _notificationService.CreateAsync(
                guild.CreatorId,
                NotificationType.GuildLeft,
                "Member left your guild",
                $"{_currentUser.Username ?? "Someone"} left \"{guild.Name}\"",
                new Dictionary<string, string> { { "guildId", request.GuildId } },
                ct);
        }

        return Result.Success();
    }
}
