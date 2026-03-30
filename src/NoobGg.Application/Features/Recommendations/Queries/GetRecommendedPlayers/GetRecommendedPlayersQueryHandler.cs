using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Recommendations.DTOs;
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
            return Result<List<RecommendedPlayerResponse>>.Fail("Authentication required", 401);

        var myId = _currentUser.UserId;

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var gameProfiles = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
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
        var myLevels = myGameProfiles.Select(gp => gp.ExperienceLevel).ToHashSet();
        var myCommPrefs = myGameProfiles.Select(gp => gp.CommunicationPreference).ToHashSet();
        var myIsLft = myGameProfiles.Any(gp => gp.LookingForTeam);
        var myPrimaryLevel = myGameProfiles
            .OrderByDescending(gp => gp.HoursPlayed ?? 0)
            .First().ExperienceLevel;
        var myEloByGame = myGameProfiles.ToDictionary(gp => gp.GameId, gp => gp.EloPoints);

        var blockList = await blocksCol
            .Find(b => b.BlockerId == myId || b.BlockedUserId == myId)
            .ToListAsync(ct);
        var excludeIds = new HashSet<string> { myId };
        foreach (var b in blockList)
        {
            excludeIds.Add(b.BlockerId == myId ? b.BlockedUserId : b.BlockerId);
        }

        var deactivatedIds = await settingsCol
            .Find(s => s.IsDeactivated)
            .Project(s => s.UserId)
            .ToListAsync(ct);
        excludeIds.UnionWith(deactivatedIds);

        var privateIds = await settingsCol
            .Find(s => s.ProfileVisibility == ProfileVisibility.Private)
            .Project(s => s.UserId)
            .ToListAsync(ct);
        excludeIds.UnionWith(privateIds);

        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        var myRoomMemberships = await roomMembers
            .Find(rm => rm.UserId == myId)
            .Project(rm => rm.RoomId)
            .ToListAsync(ct);
        var myRoomIds = new HashSet<string>(myRoomMemberships);

        var candidateGpFilter = Builders<UserGameProfile>.Filter.In(gp => gp.GameId, myGameIds)
            & Builders<UserGameProfile>.Filter.Nin(gp => gp.UserId, excludeIds);
        var candidateGps = await gameProfiles.Find(candidateGpFilter).ToListAsync(ct);

        var candidateUserIds = candidateGps.Select(gp => gp.UserId).Distinct().ToList();
        if (candidateUserIds.Count == 0)
            return Result<List<RecommendedPlayerResponse>>.Success([]);

        var userFilter = Builders<User>.Filter.In(u => u.Id, candidateUserIds)
            & Builders<User>.Filter.Eq(u => u.IsEmailVerified, true)
            & Builders<User>.Filter.Eq(u => u.IsBanned, false);
        var candidateUsers = await users.Find(userFilter).ToListAsync(ct);
        var userMap = candidateUsers.ToDictionary(u => u.Id);

        var allCandidateIds = candidateUsers.Select(u => u.Id).ToList();

        var profileList = await profiles
            .Find(Builders<UserProfile>.Filter.In(p => p.UserId, allCandidateIds))
            .ToListAsync(ct);
        var profileMap = profileList.ToDictionary(p => p.UserId);

        var allGpFilter = Builders<UserGameProfile>.Filter.In(gp => gp.UserId, allCandidateIds);
        var allGps = await gameProfiles.Find(allGpFilter).ToListAsync(ct);
        var gpMap = allGps.GroupBy(gp => gp.UserId).ToDictionary(g => g.Key, g => g.ToList());

        var allGameIds = allGps.Select(gp => gp.GameId).Distinct().ToList();
        var gameDocs = allGameIds.Count > 0
            ? await games.Find(Builders<Game>.Filter.In(g => g.Id, allGameIds)).ToListAsync(ct)
            : new List<Game>();
        var gameMap = gameDocs.ToDictionary(g => g.Id);

        var friendshipList = await friendshipsCol.Find(f =>
            (f.RequesterId == myId && allCandidateIds.Contains(f.AddresseeId)) ||
            (f.AddresseeId == myId && allCandidateIds.Contains(f.RequesterId))
        ).ToListAsync(ct);
        var friendshipMap = new Dictionary<string, Friendship>();
        foreach (var f in friendshipList)
        {
            var otherId = f.RequesterId == myId ? f.AddresseeId : f.RequesterId;
            friendshipMap[otherId] = f;
        }

        var onlineStatuses = _presenceTracker.GetOnlineStatuses(allCandidateIds.ToArray());

        var scored = new List<(RecommendedPlayerResponse Response, int Score)>();

        foreach (var userId in allCandidateIds)
        {
            if (!userMap.TryGetValue(userId, out var user)) continue;
            gpMap.TryGetValue(userId, out var userGps);
            if (userGps is null || userGps.Count == 0) continue;

            profileMap.TryGetValue(userId, out var profile);

            var score = 0;
            var reasons = new List<string>();

            var sharedGameIds = userGps.Where(gp => myGameIds.Contains(gp.GameId)).ToList();
            if (sharedGameIds.Count > 0)
            {
                score += 30;
                if (sharedGameIds.Count > 1)
                    score += Math.Min((sharedGameIds.Count - 1) * 5, 10);

                var sharedNames = sharedGameIds
                    .Select(gp => gameMap.TryGetValue(gp.GameId, out var g) ? g.Name : null)
                    .Where(n => n is not null)
                    .Take(2)
                    .ToList();
                reasons.Add($"Plays {string.Join(", ", sharedNames)}");
            }

            var topGp = userGps
                .OrderByDescending(gp => gp.LookingForTeam)
                .ThenByDescending(gp => gp.HoursPlayed ?? 0)
                .First();

            if (myRegions.Contains(topGp.Region))
            {
                score += 20;
                reasons.Add($"{topGp.Region} region");
            }

            var levelDiff = Math.Abs((int)myPrimaryLevel - (int)topGp.ExperienceLevel);
            if (levelDiff == 0)
            {
                score += 15;
                reasons.Add($"{topGp.ExperienceLevel} level");
            }
            else if (levelDiff == 1) score += 8;
            else if (levelDiff == 2) score += 3;

            foreach (var shared in sharedGameIds)
            {
                if (!myEloByGame.TryGetValue(shared.GameId, out var myElo)) continue;
                var eloDiff = Math.Abs(myElo - shared.EloPoints);
                if (eloDiff < 300)
                {
                    score += 20;
                    reasons.Add("Similar skill level");
                    break;
                }
                if (eloDiff < 500)
                {
                    score += 10;
                    break;
                }
            }

            if (myCommPrefs.Contains(topGp.CommunicationPreference))
            {
                score += 10;
            }
            else if (topGp.CommunicationPreference == CommunicationPreference.Both ||
                     myCommPrefs.Contains(CommunicationPreference.Both))
            {
                score += 6;
            }

            var candidateLft = userGps.Any(gp => gp.LookingForTeam);
            if (myIsLft && candidateLft)
            {
                score += 15;
                reasons.Add("Looking for team");
            }

            onlineStatuses.TryGetValue(userId, out var isOnline);
            if (isOnline)
            {
                score += 10;
                reasons.Add("Online now");
            }

            var playerGames = userGps.Select(gp =>
            {
                gameMap.TryGetValue(gp.GameId, out var game);
                return new RecommendedPlayerGameInfo
                {
                    GameId = gp.GameId,
                    GameName = game?.Name ?? "Unknown",
                    GameImageUrl = game?.BackgroundImageUrl,
                    Rank = gp.Rank,
                    ExperienceLevel = gp.ExperienceLevel.ToString(),
                    LookingForTeam = gp.LookingForTeam
                };
            }).ToList();

            friendshipMap.TryGetValue(userId, out var friendship);
            string? friendshipStatus = friendship?.Status switch
            {
                FriendshipStatus.Pending => "Pending",
                FriendshipStatus.Accepted => "Accepted",
                _ => null
            };

            scored.Add((new RecommendedPlayerResponse
            {
                Id = userId,
                Username = user.Username,
                AvatarUrl = profile?.AvatarUrl,
                Bio = profile?.Bio,
                Country = profile?.Country,
                Games = playerGames,
                LookingForTeam = candidateLft,
                Region = topGp.Region.ToString(),
                ExperienceLevel = topGp.ExperienceLevel.ToString(),
                CommunicationPreference = topGp.CommunicationPreference.ToString(),
                FriendshipStatus = friendshipStatus,
                Score = score,
                MatchReasons = reasons
            }, score));
        }

        var result = scored
            .OrderByDescending(s => s.Score)
            .ThenBy(_ => Random.Shared.Next())
            .Take(request.Limit)
            .Select(s => s.Response)
            .ToList();

        return Result<List<RecommendedPlayerResponse>>.Success(result);
    }
}
