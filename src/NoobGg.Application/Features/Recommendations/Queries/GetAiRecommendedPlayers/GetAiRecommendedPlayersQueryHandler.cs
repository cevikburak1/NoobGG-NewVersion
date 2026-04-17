using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Recommendations.DTOs;
using NoobGg.Application.Features.Recommendations.Queries.GetRecommendedPlayers;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Recommendations.Queries.GetAiRecommendedPlayers;

public class GetAiRecommendedPlayersQueryHandler
    : IRequestHandler<GetAiRecommendedPlayersQuery, Result<AiRecommendedPlayersResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IPresenceTracker _presenceTracker;
    private readonly IEmbeddingService _embeddingService;
    private readonly IMediator _mediator;

    private const string EmbeddingCachePrefix = "reco:emb:user:";

    public GetAiRecommendedPlayersQueryHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        IPresenceTracker presenceTracker,
        IEmbeddingService embeddingService,
        IMediator mediator)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _presenceTracker = presenceTracker;
        _embeddingService = embeddingService;
        _mediator = mediator;
    }

    public async Task<Result<AiRecommendedPlayersResponse>> Handle(
        GetAiRecommendedPlayersQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<AiRecommendedPlayersResponse>.Fail("Authentication required", 401);

        var myId = _currentUser.UserId;

        if (!_embeddingService.IsConfigured)
        {
            return await FallbackToRuleBasedAsync(request.Limit, ct);
        }

        try
        {
            var myProfileText = await BuildProfileTextAsync(myId, ct);
            if (string.IsNullOrWhiteSpace(myProfileText))
            {
                return await FallbackToRuleBasedAsync(request.Limit, ct);
            }

            var myEmbedding = await _embeddingService.GetCachedEmbeddingAsync(
                $"{EmbeddingCachePrefix}{myId}",
                myProfileText,
                ct);

            if (myEmbedding is null)
            {
                return await FallbackToRuleBasedAsync(request.Limit, ct);
            }

            var candidateIds = await GetCandidateUserIdsAsync(myId, ct);
            if (candidateIds.Count == 0)
            {
                return Result<AiRecommendedPlayersResponse>.Success(new AiRecommendedPlayersResponse
                {
                    Players = [],
                    UsedAi = true
                });
            }

            var similarities = new List<(string UserId, float Score)>();
            foreach (var candidateId in candidateIds)
            {
                var candidateProfileText = await BuildProfileTextAsync(candidateId, ct);
                if (string.IsNullOrWhiteSpace(candidateProfileText))
                    continue;

                var candidateEmbedding = await _embeddingService.GetCachedEmbeddingAsync(
                    $"{EmbeddingCachePrefix}{candidateId}",
                    candidateProfileText,
                    ct);

                if (candidateEmbedding is null)
                    continue;

                var similarity = CosineSimilarity(myEmbedding, candidateEmbedding);
                similarities.Add((candidateId, similarity));
            }

            var topCandidates = similarities
                .OrderByDescending(s => s.Score)
                .Take(request.Limit)
                .ToList();

            var players = await BuildPlayerResponsesAsync(topCandidates, myId, ct);

            return Result<AiRecommendedPlayersResponse>.Success(new AiRecommendedPlayersResponse
            {
                Players = players,
                UsedAi = true
            });
        }
        catch
        {
            return await FallbackToRuleBasedAsync(request.Limit, ct);
        }
    }

    private async Task<Result<AiRecommendedPlayersResponse>> FallbackToRuleBasedAsync(int limit, CancellationToken ct)
    {
        var ruleBasedResult = await _mediator.Send(new GetRecommendedPlayersQuery { Limit = limit }, ct);

        if (!ruleBasedResult.IsSuccess)
        {
            return Result<AiRecommendedPlayersResponse>.Fail(ruleBasedResult.Error ?? "Failed to get recommendations");
        }

        return Result<AiRecommendedPlayersResponse>.Success(new AiRecommendedPlayersResponse
        {
            Players = ruleBasedResult.Data ?? [],
            UsedAi = false
        });
    }

    private async Task<string> BuildProfileTextAsync(string userId, CancellationToken ct)
    {
        var gameProfiles = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);

        var userGps = await gameProfiles
            .Find(gp => gp.UserId == userId)
            .ToListAsync(ct);

        if (userGps.Count == 0)
            return string.Empty;

        var gameIds = userGps.Select(gp => gp.GameId).Distinct().ToList();
        var gameDocs = await games
            .Find(Builders<Game>.Filter.In(g => g.Id, gameIds))
            .ToListAsync(ct);
        var gameMap = gameDocs.ToDictionary(g => g.Id);

        var profile = await profiles.Find(p => p.UserId == userId).FirstOrDefaultAsync(ct);

        var parts = new List<string>();

        var gameNames = userGps
            .Select(gp => gameMap.TryGetValue(gp.GameId, out var g) ? g.Name : null)
            .Where(n => n is not null)
            .Distinct()
            .ToList();

        if (gameNames.Count > 0)
            parts.Add($"Games: {string.Join(", ", gameNames)}");

        var topGp = userGps
            .OrderByDescending(gp => gp.LookingForTeam)
            .ThenByDescending(gp => gp.HoursPlayed ?? 0)
            .First();

        parts.Add($"Region: {topGp.Region}");
        parts.Add($"Experience: {topGp.ExperienceLevel}");
        parts.Add($"Communication: {topGp.CommunicationPreference}");

        if (!string.IsNullOrWhiteSpace(topGp.Rank))
            parts.Add($"Rank: {topGp.Rank}");

        var languages = userGps
            .SelectMany(gp => gp.Languages)
            .Distinct()
            .ToList();

        if (languages.Count > 0)
            parts.Add($"Languages: {string.Join(", ", languages)}");

        if (userGps.Any(gp => gp.LookingForTeam))
            parts.Add("Looking for team: Yes");

        if (!string.IsNullOrWhiteSpace(profile?.Bio))
            parts.Add($"Bio: {profile.Bio}");

        return string.Join(". ", parts);
    }

    private async Task<List<string>> GetCandidateUserIdsAsync(string myId, CancellationToken ct)
    {
        var gameProfiles = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var blocksCol = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);
        var settingsCol = _mongoContext.GetCollection<UserSettings>(CollectionNames.UserSettings);
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);

        var myGameProfiles = await gameProfiles
            .Find(gp => gp.UserId == myId)
            .ToListAsync(ct);

        if (myGameProfiles.Count == 0)
            return [];

        var myGameIds = myGameProfiles.Select(gp => gp.GameId).ToHashSet();

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

        var candidateGpFilter = Builders<UserGameProfile>.Filter.In(gp => gp.GameId, myGameIds)
            & Builders<UserGameProfile>.Filter.Nin(gp => gp.UserId, excludeIds);
        var candidateGps = await gameProfiles.Find(candidateGpFilter).ToListAsync(ct);

        var candidateUserIds = candidateGps.Select(gp => gp.UserId).Distinct().ToList();
        if (candidateUserIds.Count == 0)
            return [];

        var userFilter = Builders<User>.Filter.In(u => u.Id, candidateUserIds)
            & Builders<User>.Filter.Eq(u => u.IsEmailVerified, true)
            & Builders<User>.Filter.Eq(u => u.IsBanned, false);
        var validUsers = await users.Find(userFilter).Project(u => u.Id).ToListAsync(ct);

        return validUsers.Take(100).ToList();
    }

    private async Task<List<RecommendedPlayerResponse>> BuildPlayerResponsesAsync(
        List<(string UserId, float Score)> candidates,
        string myId,
        CancellationToken ct)
    {
        var userIds = candidates.Select(c => c.UserId).ToList();
        if (userIds.Count == 0)
            return [];

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var gameProfiles = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var friendshipsCol = _mongoContext.GetCollection<Friendship>(CollectionNames.Friendships);

        var userDocs = await users
            .Find(Builders<User>.Filter.In(u => u.Id, userIds))
            .ToListAsync(ct);
        var userMap = userDocs.ToDictionary(u => u.Id);

        var profileDocs = await profiles
            .Find(Builders<UserProfile>.Filter.In(p => p.UserId, userIds))
            .ToListAsync(ct);
        var profileMap = profileDocs.ToDictionary(p => p.UserId);

        var gpDocs = await gameProfiles
            .Find(Builders<UserGameProfile>.Filter.In(gp => gp.UserId, userIds))
            .ToListAsync(ct);
        var gpMap = gpDocs.GroupBy(gp => gp.UserId).ToDictionary(g => g.Key, g => g.ToList());

        var allGameIds = gpDocs.Select(gp => gp.GameId).Distinct().ToList();
        var gameDocs = allGameIds.Count > 0
            ? await games.Find(Builders<Game>.Filter.In(g => g.Id, allGameIds)).ToListAsync(ct)
            : new List<Game>();
        var gameMap = gameDocs.ToDictionary(g => g.Id);

        var friendshipList = await friendshipsCol.Find(f =>
            (f.RequesterId == myId && userIds.Contains(f.AddresseeId)) ||
            (f.AddresseeId == myId && userIds.Contains(f.RequesterId))
        ).ToListAsync(ct);
        var friendshipMap = new Dictionary<string, Friendship>();
        foreach (var f in friendshipList)
        {
            var otherId = f.RequesterId == myId ? f.AddresseeId : f.RequesterId;
            friendshipMap[otherId] = f;
        }

        var result = new List<RecommendedPlayerResponse>();
        foreach (var (userId, score) in candidates)
        {
            if (!userMap.TryGetValue(userId, out var user))
                continue;

            gpMap.TryGetValue(userId, out var userGps);
            if (userGps is null || userGps.Count == 0)
                continue;

            profileMap.TryGetValue(userId, out var profile);

            var topGp = userGps
                .OrderByDescending(gp => gp.LookingForTeam)
                .ThenByDescending(gp => gp.HoursPlayed ?? 0)
                .First();

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

            result.Add(new RecommendedPlayerResponse
            {
                Id = userId,
                Username = user.Username,
                AvatarUrl = profile?.AvatarUrl,
                Bio = profile?.Bio,
                Country = profile?.Country,
                Games = playerGames,
                LookingForTeam = userGps.Any(gp => gp.LookingForTeam),
                Region = topGp.Region.ToString(),
                ExperienceLevel = topGp.ExperienceLevel.ToString(),
                CommunicationPreference = topGp.CommunicationPreference.ToString(),
                FriendshipStatus = friendshipStatus,
                Score = (int)(score * 100),
                MatchReasons = ["AI similarity match"]
            });
        }

        return result;
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0)
            return 0;

        float dotProduct = 0;
        float normA = 0;
        float normB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
            return 0;

        return dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }
}
