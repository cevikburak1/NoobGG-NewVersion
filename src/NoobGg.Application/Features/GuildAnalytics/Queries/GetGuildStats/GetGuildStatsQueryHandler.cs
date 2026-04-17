using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.GuildAnalytics.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.GuildAnalytics.Queries.GetGuildStats;

public class GetGuildStatsQueryHandler : IRequestHandler<GetGuildStatsQuery, Result<GuildStatsResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetGuildStatsQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<GuildStatsResponse>> Handle(GetGuildStatsQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<GuildStatsResponse>.Unauthorized();

        var guilds = _mongoContext.GetCollection<Guild>(CollectionNames.Guilds);
        var guild = await guilds.Find(g => g.Id == request.GuildId).FirstOrDefaultAsync(ct);
        if (guild is null)
            return Result<GuildStatsResponse>.NotFound("Guild not found");

        var members = _mongoContext.GetCollection<GuildMember>(CollectionNames.GuildMembers);
        var guildMembers = await members
            .Find(m => m.GuildId == request.GuildId)
            .ToListAsync(ct);

        var memberUserIds = guildMembers.Select(m => m.UserId).ToList();

        var matchResults = _mongoContext.GetCollection<MatchResult>(CollectionNames.MatchResults);
        var since = DateTime.UtcNow.AddDays(-request.Days);

        var matchFilter = Builders<MatchResult>.Filter.And(
            Builders<MatchResult>.Filter.Gte(m => m.CreatedAt, since),
            Builders<MatchResult>.Filter.Or(
                Builders<MatchResult>.Filter.In(m => m.Player1Id, memberUserIds),
                Builders<MatchResult>.Filter.In(m => m.Player2Id, memberUserIds)));

        if (request.GameId is not null)
        {
            matchFilter = Builders<MatchResult>.Filter.And(
                matchFilter,
                Builders<MatchResult>.Filter.Eq(m => m.GameId, request.GameId));
        }

        var matches = await matchResults.Find(matchFilter).ToListAsync(ct);
        var memberIdSet = memberUserIds.ToHashSet();

        var playerStats = new Dictionary<string, (int Total, int Wins)>();
        foreach (var match in matches)
        {
            if (memberIdSet.Contains(match.Player1Id))
            {
                var prev = playerStats.GetValueOrDefault(match.Player1Id);
                playerStats[match.Player1Id] = (prev.Total + 1, prev.Wins + (match.WinnerId == match.Player1Id ? 1 : 0));
            }
            if (memberIdSet.Contains(match.Player2Id))
            {
                var prev = playerStats.GetValueOrDefault(match.Player2Id);
                playerStats[match.Player2Id] = (prev.Total + 1, prev.Wins + (match.WinnerId == match.Player2Id ? 1 : 0));
            }
        }

        var totalMatches = playerStats.Values.Sum(s => s.Total);
        var totalWins = playerStats.Values.Sum(s => s.Wins);
        var overallWinRate = totalMatches > 0 ? Math.Round((double)totalWins / totalMatches * 100, 2) : 0;

        var gameProfiles = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var profileFilter = Builders<UserGameProfile>.Filter.In(p => p.UserId, memberUserIds);
        if (request.GameId is not null)
            profileFilter = Builders<UserGameProfile>.Filter.And(
                profileFilter,
                Builders<UserGameProfile>.Filter.Eq(p => p.GameId, request.GameId));

        var profiles = await gameProfiles.Find(profileFilter).ToListAsync(ct);
        var profileLookup = profiles
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.EloPoints).First());

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var userList = await users
            .Find(Builders<User>.Filter.In(u => u.Id, memberUserIds))
            .ToListAsync(ct);
        var userLookup = userList.ToDictionary(u => u.Id);

        var userProfiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var avatarList = await userProfiles
            .Find(Builders<UserProfile>.Filter.In(p => p.UserId, memberUserIds))
            .ToListAsync(ct);
        var avatarLookup = avatarList.ToDictionary(p => p.UserId);

        var topPlayers = memberUserIds
            .Where(uid => profileLookup.ContainsKey(uid))
            .Select(uid =>
            {
                var profile = profileLookup[uid];
                var stats = playerStats.GetValueOrDefault(uid);
                var username = userLookup.GetValueOrDefault(uid)?.Username ?? "Unknown";
                var avatarUrl = avatarLookup.GetValueOrDefault(uid)?.AvatarUrl;
                var winRate = stats.Total > 0 ? Math.Round((double)stats.Wins / stats.Total * 100, 2) : 0;

                return new GuildTopPlayerResponse(
                    uid, username, avatarUrl,
                    profile.EloPoints, profile.RankTier.ToString(),
                    stats.Total, stats.Wins, winRate);
            })
            .OrderByDescending(p => p.EloPoints)
            .Take(10)
            .ToList();

        var activityTimeline = BuildActivityTimeline(matches, guildMembers, memberIdSet, since, request.Days);

        var response = new GuildStatsResponse(
            guild.Id, guild.Name, guildMembers.Count,
            totalMatches, totalWins, overallWinRate,
            topPlayers, activityTimeline);

        return Result<GuildStatsResponse>.Success(response);
    }

    private static List<GuildActivityPoint> BuildActivityTimeline(
        List<MatchResult> matches,
        List<GuildMember> guildMembers,
        HashSet<string> memberIdSet,
        DateTime since,
        int days)
    {
        var matchesByDate = matches
            .Where(m => memberIdSet.Contains(m.Player1Id) || memberIdSet.Contains(m.Player2Id))
            .GroupBy(m => m.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var membersByDate = guildMembers
            .Where(m => m.JoinedAt >= since)
            .GroupBy(m => m.JoinedAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var timeline = new List<GuildActivityPoint>();
        for (var i = 0; i < days; i++)
        {
            var date = since.Date.AddDays(i);
            var matchCount = matchesByDate.GetValueOrDefault(date);
            var memberCount = membersByDate.GetValueOrDefault(date);
            timeline.Add(new GuildActivityPoint(date.ToString("yyyy-MM-dd"), matchCount, memberCount));
        }

        return timeline;
    }
}
