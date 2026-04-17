using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Recent.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Recent.Queries.GetRecentRooms;

public class GetRecentRoomsQueryHandler
    : IRequestHandler<GetRecentRoomsQuery, Result<List<RecentRoomResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetRecentRoomsQueryHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<List<RecentRoomResponse>>> Handle(
        GetRecentRoomsQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<List<RecentRoomResponse>>.Fail("Authentication required", 401);

        var myId = _currentUser.UserId;
        var recentCol = _mongoContext.GetCollection<RecentActivity>(CollectionNames.RecentActivities);
        var roomsCol = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var gamesCol = _mongoContext.GetCollection<Game>(CollectionNames.Games);

        var recentActivities = await recentCol
            .Find(r => r.UserId == myId && r.TargetType == RecentActivityTargetType.Room)
            .SortByDescending(r => r.SeenAt)
            .Limit(request.Limit)
            .ToListAsync(ct);

        if (recentActivities.Count == 0)
            return Result<List<RecentRoomResponse>>.Success([]);

        var targetIds = recentActivities.Select(r => r.TargetId).ToList();

        var rooms = await roomsCol
            .Find(Builders<Room>.Filter.In(r => r.Id, targetIds))
            .ToListAsync(ct);
        var roomMap = rooms.ToDictionary(r => r.Id);

        var gameIds = rooms.Select(r => r.GameId).Distinct().ToList();
        var games = gameIds.Count > 0
            ? await gamesCol.Find(Builders<Game>.Filter.In(g => g.Id, gameIds)).ToListAsync(ct)
            : new List<Game>();
        var gameMap = games.ToDictionary(g => g.Id);

        var result = new List<RecentRoomResponse>();
        foreach (var activity in recentActivities)
        {
            if (!roomMap.TryGetValue(activity.TargetId, out var room))
                continue;

            gameMap.TryGetValue(room.GameId, out var game);

            result.Add(new RecentRoomResponse
            {
                Id = room.Id,
                Title = room.Title,
                GameName = game?.Name,
                GameImageUrl = game?.BackgroundImageUrl,
                Status = room.Status.ToString(),
                CurrentMemberCount = room.CurrentMemberCount,
                MaxMembers = room.MaxMembers,
                SeenAt = activity.SeenAt
            });
        }

        return Result<List<RecentRoomResponse>>.Success(result);
    }
}
