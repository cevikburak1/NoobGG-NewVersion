using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Recent.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Recent.Queries.GetRecentPlayers;

public class GetRecentPlayersQueryHandler
    : IRequestHandler<GetRecentPlayersQuery, Result<List<RecentPlayerResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IPresenceTracker _presenceTracker;

    public GetRecentPlayersQueryHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        IPresenceTracker presenceTracker)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _presenceTracker = presenceTracker;
    }

    public async Task<Result<List<RecentPlayerResponse>>> Handle(
        GetRecentPlayersQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<List<RecentPlayerResponse>>.Fail("Authentication required", 401);

        var myId = _currentUser.UserId;
        var recentCol = _mongoContext.GetCollection<RecentActivity>(CollectionNames.RecentActivities);
        var usersCol = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profilesCol = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);

        var recentActivities = await recentCol
            .Find(r => r.UserId == myId && r.TargetType == RecentActivityTargetType.Player)
            .SortByDescending(r => r.SeenAt)
            .Limit(request.Limit)
            .ToListAsync(ct);

        if (recentActivities.Count == 0)
            return Result<List<RecentPlayerResponse>>.Success([]);

        var targetIds = recentActivities.Select(r => r.TargetId).ToList();

        var users = await usersCol
            .Find(Builders<User>.Filter.In(u => u.Id, targetIds))
            .ToListAsync(ct);
        var userMap = users.ToDictionary(u => u.Id);

        var profiles = await profilesCol
            .Find(Builders<UserProfile>.Filter.In(p => p.UserId, targetIds))
            .ToListAsync(ct);
        var profileMap = profiles.ToDictionary(p => p.UserId);

        var onlineStatuses = _presenceTracker.GetOnlineStatuses(targetIds.ToArray());

        var result = new List<RecentPlayerResponse>();
        foreach (var activity in recentActivities)
        {
            if (!userMap.TryGetValue(activity.TargetId, out var user))
                continue;

            profileMap.TryGetValue(activity.TargetId, out var profile);
            onlineStatuses.TryGetValue(activity.TargetId, out var isOnline);

            result.Add(new RecentPlayerResponse
            {
                Id = user.Id,
                Username = user.Username,
                AvatarUrl = profile?.AvatarUrl,
                Country = profile?.Country,
                IsOnline = isOnline,
                SeenAt = activity.SeenAt
            });
        }

        return Result<List<RecentPlayerResponse>>.Success(result);
    }
}
