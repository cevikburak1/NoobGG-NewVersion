using System.Text.RegularExpressions;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Users.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Users.Queries.DiscoverPlayers;

public class DiscoverPlayersQueryHandler : IRequestHandler<DiscoverPlayersQuery, Result<PagedResult<DiscoverPlayerResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public DiscoverPlayersQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<DiscoverPlayerResponse>>> Handle(DiscoverPlayersQuery request, CancellationToken ct)
    {
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var gameProfiles = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var blocksCol = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);
        var settingsCol = _mongoContext.GetCollection<UserSettings>(CollectionNames.UserSettings);

        var blockedByMeIds = new HashSet<string>();
        var blockedMeIds = new HashSet<string>();
        if (_currentUser.IsAuthenticated && _currentUser.UserId is not null)
        {
            var myId = _currentUser.UserId;
            var blockList = await blocksCol.Find(b =>
                b.BlockerId == myId || b.BlockedUserId == myId
            ).ToListAsync(ct);

            foreach (var b in blockList)
            {
                if (b.BlockerId == myId)
                    blockedByMeIds.Add(b.BlockedUserId);
                else
                    blockedMeIds.Add(b.BlockerId);
            }
        }

        var deactivatedSettings = await settingsCol
            .Find(s => s.IsDeactivated)
            .Project(s => s.UserId)
            .ToListAsync(ct);
        var deactivatedIds = new HashSet<string>(deactivatedSettings);

        var privateSettings = await settingsCol
            .Find(s => s.ProfileVisibility == ProfileVisibility.Private)
            .Project(s => s.UserId)
            .ToListAsync(ct);
        var privateIds = new HashSet<string>(privateSettings);

        var userFilters = new List<FilterDefinition<User>>
        {
            Builders<User>.Filter.Eq(u => u.IsEmailVerified, true),
            Builders<User>.Filter.Eq(u => u.IsBanned, false)
        };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = Regex.Escape(request.Search.Trim().ToLowerInvariant());
            userFilters.Add(Builders<User>.Filter.Regex(u => u.Username, new BsonRegularExpression(term, "i")));
        }

        var excludeIds = new HashSet<string>(blockedMeIds);
        excludeIds.UnionWith(deactivatedIds);
        excludeIds.UnionWith(privateIds);

        if (_currentUser.IsAuthenticated && _currentUser.UserId is not null)
            excludeIds.Add(_currentUser.UserId);

        if (excludeIds.Count > 0)
            userFilters.Add(Builders<User>.Filter.Nin(u => u.Id, excludeIds));

        var userFilter = Builders<User>.Filter.And(userFilters);
        var totalCount = await users.CountDocumentsAsync(userFilter, cancellationToken: ct);

        var skip = (request.Page - 1) * request.PageSize;
        var userList = await users.Find(userFilter)
            .SortByDescending(u => u.CreatedAt)
            .Skip(skip)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        var userIds = userList.Select(u => u.Id).ToList();

        var profileFilter = Builders<UserProfile>.Filter.In(p => p.UserId, userIds);
        var profileList = await profiles.Find(profileFilter).ToListAsync(ct);
        var profileMap = profileList.ToDictionary(p => p.UserId);

        var gpFilter = Builders<UserGameProfile>.Filter.In(gp => gp.UserId, userIds);
        var gpList = await gameProfiles.Find(gpFilter).ToListAsync(ct);
        var gpMap = gpList.GroupBy(gp => gp.UserId).ToDictionary(g => g.Key, g => g.ToList());

        var gameIds = gpList.Select(gp => gp.GameId).Distinct().ToList();
        var gameFilter = Builders<Game>.Filter.In(g => g.Id, gameIds);
        var gameList = gameIds.Count > 0
            ? await games.Find(gameFilter).ToListAsync(ct)
            : [];
        var gameMap = gameList.ToDictionary(g => g.Id);

        var userSettingsFilter = Builders<UserSettings>.Filter.In(s => s.UserId, userIds);
        var userSettingsList = await settingsCol.Find(userSettingsFilter).ToListAsync(ct);
        var settingsMap = userSettingsList.ToDictionary(s => s.UserId);

        var friendshipMap = new Dictionary<string, Friendship>();
        if (_currentUser.IsAuthenticated && _currentUser.UserId is not null)
        {
            var myId = _currentUser.UserId;
            var friendshipsCol = _mongoContext.GetCollection<Friendship>(CollectionNames.Friendships);
            var friendshipList = await friendshipsCol.Find(f =>
                (f.RequesterId == myId && userIds.Contains(f.AddresseeId)) ||
                (f.AddresseeId == myId && userIds.Contains(f.RequesterId))
            ).ToListAsync(ct);

            foreach (var f in friendshipList)
            {
                var otherId = f.RequesterId == myId ? f.AddresseeId : f.RequesterId;
                friendshipMap[otherId] = f;
            }
        }

        var items = new List<DiscoverPlayerResponse>();

        foreach (var user in userList)
        {
            profileMap.TryGetValue(user.Id, out var profile);
            gpMap.TryGetValue(user.Id, out var userGameProfiles);
            settingsMap.TryGetValue(user.Id, out var userSettings);

            var topGameProfile = userGameProfiles?
                .OrderByDescending(gp => gp.LookingForTeam)
                .ThenByDescending(gp => gp.HoursPlayed ?? 0)
                .FirstOrDefault();

            var playerGames = (userGameProfiles ?? []).Select(gp =>
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

            var gpLft = userGameProfiles?.Any(gp => gp.LookingForTeam) ?? false;
            var defaultLft = userSettings?.DefaultLookingForTeam ?? false;
            var isLft = gpLft || defaultLft;

            if (!string.IsNullOrWhiteSpace(request.GameId))
            {
                var hasGame = userGameProfiles?.Any(gp => gp.GameId == request.GameId) ?? false;
                if (!hasGame) continue;
            }

            if (request.LookingForTeam == true && !isLft) continue;

            if (request.Region.HasValue && topGameProfile?.Region != request.Region.Value)
            {
                if (topGameProfile is null) continue;
            }

            if (request.ExperienceLevel.HasValue && topGameProfile?.ExperienceLevel != request.ExperienceLevel.Value)
            {
                if (topGameProfile is null) continue;
            }

            friendshipMap.TryGetValue(user.Id, out var friendship);
            string? friendshipStatus = friendship?.Status switch
            {
                Domain.Enums.FriendshipStatus.Pending => "Pending",
                Domain.Enums.FriendshipStatus.Accepted => "Accepted",
                _ => null
            };

            items.Add(new DiscoverPlayerResponse
            {
                Id = user.Id,
                Username = user.Username,
                AvatarUrl = profile?.AvatarUrl,
                Bio = profile?.Bio,
                Country = profile?.Country,
                Games = playerGames,
                LookingForTeam = isLft,
                Region = topGameProfile?.Region.ToString(),
                ExperienceLevel = topGameProfile?.ExperienceLevel.ToString(),
                CommunicationPreference = topGameProfile?.CommunicationPreference.ToString(),
                IsBlockedByMe = blockedByMeIds.Contains(user.Id),
                FriendshipStatus = friendshipStatus
            });
        }

        var result = new PagedResult<DiscoverPlayerResponse>
        {
            Items = items,
            TotalCount = (int)totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result<PagedResult<DiscoverPlayerResponse>>.Success(result);
    }
}
