using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Favorites.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Favorites.Queries.GetMyFavorites;

public class GetMyFavoritesQueryHandler : IRequestHandler<GetMyFavoritesQuery, Result<List<FavoritePlayerResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IPresenceTracker _presenceTracker;

    public GetMyFavoritesQueryHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        IPresenceTracker presenceTracker)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _presenceTracker = presenceTracker;
    }

    public async Task<Result<List<FavoritePlayerResponse>>> Handle(GetMyFavoritesQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<List<FavoritePlayerResponse>>.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var favorites = _mongoContext.GetCollection<Favorite>(CollectionNames.Favorites);

        var favList = await favorites
            .Find(f => f.UserId == userId)
            .SortByDescending(f => f.CreatedAt)
            .ToListAsync(ct);

        if (favList.Count == 0)
            return Result<List<FavoritePlayerResponse>>.Success([]);

        var favUserIds = favList.Select(f => f.FavoriteUserId).ToList();

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var userList = await users.Find(u => favUserIds.Contains(u.Id)).ToListAsync(ct);
        var userMap = userList.ToDictionary(u => u.Id);

        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var profileList = await profiles.Find(p => favUserIds.Contains(p.UserId)).ToListAsync(ct);
        var profileMap = profileList.ToDictionary(p => p.UserId);

        var onlineStatuses = _presenceTracker.GetOnlineStatuses(favUserIds);

        var result = favList
            .Where(f => userMap.ContainsKey(f.FavoriteUserId))
            .Select(f =>
            {
                userMap.TryGetValue(f.FavoriteUserId, out var user);
                profileMap.TryGetValue(f.FavoriteUserId, out var profile);
                onlineStatuses.TryGetValue(f.FavoriteUserId, out var isOnline);

                return new FavoritePlayerResponse(
                    UserId: f.FavoriteUserId,
                    Username: user?.Username ?? "Unknown",
                    AvatarUrl: profile?.AvatarUrl,
                    IsOnline: isOnline,
                    FavoritedAt: f.CreatedAt);
            }).ToList();

        return Result<List<FavoritePlayerResponse>>.Success(result);
    }
}
