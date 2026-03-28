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
            return Result<List<RecommendedRoomResponse>>.Unauthorized();

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
        var myRanksByGame = myGameProfiles.ToDictionary(gp => gp.GameId, gp => gp.Rank);

        var myRoomMemberships = await roomMembersCol
            .Find(rm => rm.UserId == myId)
            .Project(rm => rm.RoomId)
            .ToListAsync(ct);
        var myRoomIds = new HashSet<string>(myRoomMemberships);

        var roomFilters = new List<FilterDefinition<Room>>
        {
            Builders<Room>.Filter.Eq(r => r.Status, RoomStatus.Open),
            Builders<Room>.Filter.Eq(r => r.IsPublic, true),
            Builders<Room>.Filter.Ne(r => r.CreatorId, myId),
            Builders<Room>.Filter.In(r => r.GameId,
                request.GameId != null ? new[] { request.GameId } : myGameIds.ToArray())
        };

        var roomFilter = Builders<Room>.Filter.And(roomFilters);
        var candidateRooms = await roomsCol
            .Find(roomFilter)
            .SortByDescending(r => r.CreatedAt)
            .Limit(200)
            .ToListAsync(ct);

        candidateRooms = candidateRooms
            .Where(r => !myRoomIds.Contains(r.Id))
            .ToList();

        if (candidateRooms.Count == 0)
            return Result<List<RecommendedRoomResponse>>.Success([]);

        var gameIds = candidateRooms.Select(r => r.GameId).Distinct().ToList();
        var gameDocs = await gamesCol
            .Find(Builders<Game>.Filter.In(g => g.Id, gameIds))
            .ToListAsync(ct);
        var gameMap = gameDocs.ToDictionary(g => g.Id);

        var scored = new List<(RecommendedRoomResponse Response, double Score)>();
        var now = DateTime.UtcNow;

        foreach (var room in candidateRooms)
        {
            double score = 0;
            var reasons = new List<string>();

            if (myGameIds.Contains(room.GameId))
            {
                score += 30;
                gameMap.TryGetValue(room.GameId, out var game);
                reasons.Add($"You play {game?.Name ?? "this game"}");
            }

            if (myRegions.Contains(room.Region))
            {
                score += 20;
                reasons.Add($"Same region ({room.Region})");
            }

            if (myLanguages.Contains(room.Language))
            {
                score += 15;
                reasons.Add($"Speaks {room.Language}");
            }

            if (room.RankRange != null && myRanksByGame.TryGetValue(room.GameId, out var myRank))
            {
                var rankFit = IsRankInRange(myRank, room.RankRange.Min, room.RankRange.Max);
                if (rankFit)
                {
                    score += 10;
                    reasons.Add("Your rank fits the room range");
                }
            }

            var spotsLeft = room.MaxMembers - room.CurrentMemberCount;
            if (spotsLeft > 0)
            {
                var capacityRatio = (double)spotsLeft / room.MaxMembers;
                score += capacityRatio * 10;
                reasons.Add($"{spotsLeft} spot(s) available");
            }

            var ageHours = (now - room.CreatedAt).TotalHours;
            if (ageHours < 1)
            {
                score += 5;
                reasons.Add("Just created");
            }
            else if (ageHours < 6)
            {
                score += 3;
                reasons.Add("Recently created");
            }

            gameMap.TryGetValue(room.GameId, out var roomGame);

            scored.Add((new RecommendedRoomResponse
            {
                Id = room.Id,
                Title = room.Title,
                GameId = room.GameId,
                GameName = roomGame?.Name,
                GameImageUrl = roomGame?.BackgroundImageUrl,
                CreatorId = room.CreatorId,
                MaxMembers = room.MaxMembers,
                CurrentMemberCount = room.CurrentMemberCount,
                Region = room.Region.ToString(),
                Language = room.Language.ToString(),
                Tags = room.Tags,
                Status = room.Status.ToString(),
                CreatedAt = room.CreatedAt,
                Score = Math.Round(score, 1),
                MatchReasons = reasons
            }, score));
        }

        var result = scored
            .OrderByDescending(s => s.Score)
            .ThenBy(_ => Guid.NewGuid())
            .Take(request.Limit)
            .Select(s => s.Response)
            .ToList();

        return Result<List<RecommendedRoomResponse>>.Success(result);
    }

    /// <summary>
    /// Simple lexicographic rank comparison. Works for numeric ranks and
    /// alphabetically ordered rank names (e.g., Bronze < Gold < Silver).
    /// A proper implementation should use game-specific rank ordinals.
    /// </summary>
    private static bool IsRankInRange(string rank, string min, string max)
    {
        if (string.IsNullOrWhiteSpace(rank)) return true;
        if (string.IsNullOrWhiteSpace(min) && string.IsNullOrWhiteSpace(max)) return true;

        var cmp = StringComparer.OrdinalIgnoreCase;
        var aboveMin = string.IsNullOrWhiteSpace(min) || cmp.Compare(rank, min) >= 0;
        var belowMax = string.IsNullOrWhiteSpace(max) || cmp.Compare(rank, max) <= 0;
        return aboveMin && belowMax;
    }
}
