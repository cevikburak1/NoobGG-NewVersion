using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Recommendations.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Recommendations.Queries.GetRecommendedRooms;

public class GetRecommendedRoomsQueryHandler
    : IRequestHandler<GetRecommendedRoomsQuery, Result<List<RecommendedRoomResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetRecommendedRoomsQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<List<RecommendedRoomResponse>>> Handle(
        GetRecommendedRoomsQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<List<RecommendedRoomResponse>>.Fail("Authentication required", 401);

        var myId = _currentUser.UserId;

        var gameProfiles = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var roomsCol = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var roomMembersCol = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        var gamesCol = _mongoContext.GetCollection<Game>(CollectionNames.Games);

        var myGameProfiles = await gameProfiles
            .Find(gp => gp.UserId == myId)
            .ToListAsync(ct);

        if (myGameProfiles.Count == 0)
            return Result<List<RecommendedRoomResponse>>.Success([]);

        var myGameIds = myGameProfiles.Select(gp => gp.GameId).ToHashSet();
        var myRegions = myGameProfiles.Select(gp => gp.Region).ToHashSet();
        var myLanguages = myGameProfiles.SelectMany(gp => gp.Languages).ToHashSet();
        var myRanks = myGameProfiles.ToDictionary(gp => gp.GameId, gp => gp.Rank);

        var myMemberships = await roomMembersCol
            .Find(rm => rm.UserId == myId)
            .Project(rm => rm.RoomId)
            .ToListAsync(ct);
        var myRoomIds = new HashSet<string>(myMemberships);

        var roomFilter = Builders<Room>.Filter.Eq(r => r.IsPublic, true)
            & Builders<Room>.Filter.Ne(r => r.Status, RoomStatus.Closed)
            & Builders<Room>.Filter.Nin(r => r.Id, myRoomIds);
        var candidateRooms = await roomsCol
            .Find(roomFilter)
            .SortByDescending(r => r.CreatedAt)
            .Limit(100)
            .ToListAsync(ct);

        if (candidateRooms.Count == 0)
            return Result<List<RecommendedRoomResponse>>.Success([]);

        var gameIds = candidateRooms.Select(r => r.GameId).Distinct().ToList();
        var gameDocs = await gamesCol
            .Find(Builders<Game>.Filter.In(g => g.Id, gameIds))
            .ToListAsync(ct);
        var gameMap = gameDocs.ToDictionary(g => g.Id);

        var now = DateTime.UtcNow;

        var scored = new List<(RecommendedRoomResponse Response, int Score)>();

        foreach (var room in candidateRooms)
        {
            var score = 0;
            var reasons = new List<string>();

            if (myGameIds.Contains(room.GameId))
            {
                score += 35;
                gameMap.TryGetValue(room.GameId, out var matchedGame);
                reasons.Add($"You play {matchedGame?.Name ?? "this game"}");
            }

            if (myRegions.Contains(room.Region))
            {
                score += 20;
                reasons.Add($"{room.Region} region");
            }

            if (myLanguages.Contains(room.Language))
            {
                score += 15;
                reasons.Add($"{room.Language} speaking");
            }

            if (room.Status == RoomStatus.Open)
            {
                var fillPct = room.MaxMembers > 0
                    ? (double)room.CurrentMemberCount / room.MaxMembers * 100
                    : 100;
                if (fillPct < 80)
                {
                    score += 15;
                    reasons.Add("Has open spots");
                }
                else
                {
                    score += 5;
                }
            }

            var ageHours = (now - room.CreatedAt).TotalHours;
            if (ageHours < 1)
            {
                score += 15;
                reasons.Add("Just created");
            }
            else if (ageHours < 6)
            {
                score += 10;
                reasons.Add("Recently created");
            }
            else if (ageHours < 24)
            {
                score += 5;
            }

            gameMap.TryGetValue(room.GameId, out var game);

            scored.Add((new RecommendedRoomResponse
            {
                Id = room.Id,
                Title = room.Title,
                GameId = room.GameId,
                GameName = game?.Name,
                GameImageUrl = game?.BackgroundImageUrl,
                CreatorId = room.CreatorId,
                MaxMembers = room.MaxMembers,
                CurrentMemberCount = room.CurrentMemberCount,
                Region = room.Region.ToString(),
                Language = room.Language.ToString(),
                Tags = room.Tags,
                Status = room.Status.ToString(),
                CreatedAt = room.CreatedAt,
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

        return Result<List<RecommendedRoomResponse>>.Success(result);
    }
}
