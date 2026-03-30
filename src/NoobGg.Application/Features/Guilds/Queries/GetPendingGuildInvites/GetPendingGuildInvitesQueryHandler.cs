using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Guilds.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guilds.Queries.GetPendingGuildInvites;

public class GetPendingGuildInvitesQueryHandler : IRequestHandler<GetPendingGuildInvitesQuery, Result<List<GuildInviteResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetPendingGuildInvitesQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<List<GuildInviteResponse>>> Handle(GetPendingGuildInvitesQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<List<GuildInviteResponse>>.Unauthorized();

        var userId = _currentUser.UserId;
        var invites = _mongoContext.GetCollection<GuildInvite>(CollectionNames.GuildInvites);

        var pendingInvites = await invites
            .Find(i => i.InvitedUserId == userId && i.Status == GuildInviteStatus.Pending)
            .SortByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        if (pendingInvites.Count == 0)
            return Result<List<GuildInviteResponse>>.Success([]);

        var guildIds = pendingInvites.Select(i => i.GuildId).Distinct().ToList();
        var guilds = _mongoContext.GetCollection<Guild>(CollectionNames.Guilds);
        var guildDocs = await guilds.Find(Builders<Guild>.Filter.In(g => g.Id, guildIds)).ToListAsync(ct);
        var guildMap = guildDocs.ToDictionary(g => g.Id, g => (g.Name, g.Tag));

        var inviterIds = pendingInvites.Select(i => i.InviterId).Distinct().ToList();
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var inviterDocs = await users.Find(Builders<User>.Filter.In(u => u.Id, inviterIds)).ToListAsync(ct);
        var inviterMap = inviterDocs.ToDictionary(u => u.Id, u => u.Username);

        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var inviterProfiles = await profiles.Find(Builders<UserProfile>.Filter.In(p => p.UserId, inviterIds)).ToListAsync(ct);
        var avatarMap = inviterProfiles.ToDictionary(p => p.UserId, p => p.AvatarUrl);

        var responses = pendingInvites.Select(i =>
        {
            guildMap.TryGetValue(i.GuildId, out var guildInfo);
            return new GuildInviteResponse(
                i.Id,
                i.GuildId,
                guildInfo.Name ?? "Unknown",
                guildInfo.Tag ?? "???",
                i.InviterId,
                inviterMap.GetValueOrDefault(i.InviterId, "Unknown"),
                avatarMap.GetValueOrDefault(i.InviterId),
                i.Status.ToString(),
                i.CreatedAt);
        }).ToList();

        return Result<List<GuildInviteResponse>>.Success(responses);
    }
}
