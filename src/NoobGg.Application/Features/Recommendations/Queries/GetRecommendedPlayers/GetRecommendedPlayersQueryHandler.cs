using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Recommendations.DTOs;
using NoobGg.Application.Features.Users.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Recommendations.Queries.GetRecommendedPlayers;

public class GetRecommendedPlayersQueryHandler
    : IRequestHandler<GetRecommendedPlayersQuery, Result<List<RecommendedPlayerResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IPresenceTracker _presenceTracker;

    public GetRecommendedPlayersQueryHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        IPresenceTracker presenceTracker)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _presenceTracker = presenceTracker;
    }

    public async Task<Result<List<RecommendedPlayerResponse>>> Handle(
        GetRecommendedPlayersQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<List<RecommendedPlayerResponse>>.Unauthorized();

        var myId = _currentUser.UserId;

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var gameProfiles = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var gamesCol = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var blocksCol = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);
        var settingsCol = _mongoContext.GetCollection<UserSettings>(CollectionNames.UserSettings);
        var friendshipsCol = _mongoContext.GetCollection<Friendship>(CollectionNames.Friendships);

        var myGameProfiles = await gameProfiles
            .Find(gp => gp.UserId == myId)
            .ToListAsync(ct);

        if (myGameProfiles.Count == 0)
            return Result<List<RecommendedPlayerResponse>>.Success([]);

        var myGameIds = myGameProfiles.Select(gp => gp.GameId).ToHashSet();
        var myRegions = myGameProfiles.Select(gp => gp.Region).ToHashSet();
        var myLanguages = myGameProfiles.SelectMany(gp => gp.Languages).ToHashSet();
        var myExperienceLevels = myGameProfiles
            .ToDictionary(gp => gp.GameId, gp => gp.ExperienceLevel);
        var myCommPrefs = myGameProfiles
            .Select(gp => gp.CommunicationPreference).ToHashSet();

        var blockList = await blocksCol
            .Find(b => b.BlockerId == myId || b.BlockedUserId == myId)
            .ToListAsync(ct);
        var excludeIds = new HashSet<string>(blockList.Select(b =>
            b.BlockerId == myId ? b.BlockedUserId : b.BlockerId))
        { myId };

        var acceptedFriends = await friendshipsCol
            .Find(f => f.Status == FriendshipStatus.Accepted &&
                       (f.RequesterId == myId || f.AddresseeId == myId))
            .ToListAsync(ct);
        var friendIds = new HashSet<string>(acceptedFriends.Select(f =>
            f.RequesterId == myId ? f.AddresseeId : f.RequesterId));
        excludeIds.UnionWith(friendIds);

        var deactivatedIds = new HashSet<string>(
            await settingsCol
                .Find(s => s.IsDeactivated)
                .Project(s => s.UserId)
                .ToListAsync(ct));
        excludeIds.UnionWith(deactivatedIds);

        var privateIds = new HashSet<string>(
            await settingsCol
                .Find(s => s.ProfileVisibility == ProfileVisibility.Private)
                .Project(s => s.UserId)
                .ToListAsync(ct));
        excludeIds.UnionWith(privateIds);

        var candidateGpFilter = Builders<UserGameProfile>.Filter.And(
            Builders<UserGameProfile>.Filter.Nin(gp => gp.UserId, excludeIds),
            Builders<UserGameProfile>.Filter.In(gp => gp.GameId,
                request.GameId != null ? [request.GameId] : myGameIds.ToList())
        );

        var candidateGps = await gameProfiles
            .Find(candidateGpFilter)
            .Limit(500)
            .ToListAsync(ct);

        var candidateUserIds = candidateGps.Select(gp => gp.UserId).Distinct().ToList();
        if (candidateUserIds.Count == 0)
            return Result<List<RecommendedPlayerResponse>>.Success([]);

        var userFilter = Builders<User>.Filter.And(
            Builders<User>.Filter.In(u => u.Id, candidateUserIds),
            Builders<User>.Filter.Eq(u => u.IsEmailVerified, true),
            Builders<User>.Filter.Eq(u => u.IsBanned, false)
        );
        var userList = await users.Find(userFilter).ToListAsync(ct);
        var userMap = userList.ToDictionary(u => u.Id);

        var profileList = await profiles
            .Find(Builders<UserProfile>.Filter.In(p => p.UserId, candidateUserIds))
            .ToListAsync(ct);
        var profileMap = profileList.ToDictionary(p => p.UserId);

        var allCandidateGps = await gameProfiles
            .Find(Builders<UserGameProfile>.Filter.In(gp => gp.UserId, candidateUserIds))
            .ToListAsync(ct);
        var gpByUser = allCandidateGps.GroupBy(gp => gp.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var gameIds = allCandidateGps.Select(gp => gp.GameId).Distinct().ToList();
        var gameDocs = gameIds.Count > 0
            ? await gamesCol.Find(Builders<Game>.Filter.In(g => g.Id, gameIds)).ToListAsync(ct)
            : new List<Game>();
        var gameMap = gameDocs.ToDictionary(g => g.Id);

        var onlineStatuses = _presenceTracker.GetOnlineStatuses(candidateUserIds);

        var scored = new List<(RecommendedPlayerResponse Response, double Score)>();

        foreach (var userId in candidateUserIds)
        {
            if (!userMap.TryGetValue(userId, out var user)) continue;
            if (!gpByUser.TryGetValue(userId, out var userGps)) continue;

            double score = 0;
            var reasons = new List<string>();

            var sharedGameIds = userGps
                .Where(gp => myGameIds.Contains(gp.GameId))
                .Select(gp => gp.GameId)
                .ToList();

            if (sharedGameIds.Count > 0)
            {
                score += 30;
                reasons.Add($"Plays {sharedGameIds.Count} shared game(s)");
            }
            else continue;

            var bestMatchGp = userGps
                .Where(gp => sharedGameIds.Contains(gp.GameId))
                .OrderByDescending(gp => gp.LookingForTeam)
                .ThenByDescending(gp => gp.HoursPlayed ?? 0)
                .First();

            if (myRegions.Contains(bestMatchGp.Region))
            {
                score += 20;
                reasons.Add($"Same region ({bestMatchGp.Region})");
            }

            if (myExperienceLevels.TryGetValue(bestMatchGp.GameId, out var myExp))
            {
                var diff = Math.Abs((int)myExp - (int)bestMatchGp.ExperienceLevel);
                if (diff == 0)
                {
                    score += 15;
                    reasons.Add("Same experience level");
                }
                else if (diff == 1)
                {
                    score += 8;
                    reasons.Add("Similar experience level");
                }
            }

            var sharedLangs = bestMatchGp.Languages
                .Where(l => myLanguages.Contains(l))
                .ToList();
            if (sharedLangs.Count > 0)
            {
                score += Math.Min(sharedLangs.Count * 10, 20);
                reasons.Add($"Speaks {string.Join(", ", sharedLangs)}");
            }

            if (myCommPrefs.Contains(bestMatchGp.CommunicationPreference) ||
                bestMatchGp.CommunicationPreference == CommunicationPreference.Both ||
                myCommPrefs.Contains(CommunicationPreference.Both))
            {
                score += 10;
                reasons.Add("Compatible communication style");
            }

            if (bestMatchGp.LookingForTeam)
            {
                score += 10;
                reasons.Add("Looking for team");
            }

            if (onlineStatuses.TryGetValue(userId, out var isOnline) && isOnline)
            {
                score += 5;
                reasons.Add("Currently online");
            }

            profileMap.TryGetValue(userId, out var profile);
            var playerGames = userGps.Select(gp =>
            {
                gameMap.TryGetValue(gp.GameId, out var game);
                return new PlayerGameInfo
                {
                    GameId = gp.GameId,
                    GameName = game?.Name ?? "Unknown",
                    GameImageUrl = game?.BackgroundImageUrl,
                    Rank = gp.Rank,
                    ExperienceLevel = gp.ExperienceLevel.ToString(),
                    LookingForTeam = gp.LookingForTeam
                };
            }).ToList();

            scored.Add((new RecommendedPlayerResponse
            {
                Id = user.Id,
                Username = user.Username,
                AvatarUrl = profile?.AvatarUrl,
                Bio = profile?.Bio,
                Country = profile?.Country,
                Games = playerGames,
                LookingForTeam = bestMatchGp.LookingForTeam,
                Region = bestMatchGp.Region.ToString(),
                ExperienceLevel = bestMatchGp.ExperienceLevel.ToString(),
                CommunicationPreference = bestMatchGp.CommunicationPreference.ToString(),
                Score = score,
                MatchReasons = reasons
            }, score));
        }

        var result = scored
            .OrderByDescending(s => s.Score)
            .ThenBy(_ => Guid.NewGuid())
            .Take(request.Limit)
            .Select(s => s.Response)
            .ToList();

        return Result<List<RecommendedPlayerResponse>>.Success(result);
    }
}
