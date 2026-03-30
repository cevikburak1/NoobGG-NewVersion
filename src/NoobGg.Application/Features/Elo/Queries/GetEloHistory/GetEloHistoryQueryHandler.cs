using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Elo.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Elo.Queries.GetEloHistory;

public class GetEloHistoryQueryHandler
    : IRequestHandler<GetEloHistoryQuery, Result<EloHistoryResponse>>
{
    private readonly IMongoContext _mongoContext;

    public GetEloHistoryQueryHandler(IMongoContext mongoContext)
    {
        _mongoContext = mongoContext;
    }

    public async Task<Result<EloHistoryResponse>> Handle(GetEloHistoryQuery request, CancellationToken ct)
    {
        var gpCol = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var gamesCol = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var matchCol = _mongoContext.GetCollection<MatchResult>(CollectionNames.MatchResults);
        var usersCol = _mongoContext.GetCollection<User>(CollectionNames.Users);

        var gameProfile = await gpCol
            .Find(p => p.UserId == request.UserId && p.GameId == request.GameId)
            .FirstOrDefaultAsync(ct);

        if (gameProfile is null)
            return Result<EloHistoryResponse>.NotFound("Game profile not found");

        var game = await gamesCol.Find(g => g.Id == request.GameId).FirstOrDefaultAsync(ct);

        var history = gameProfile.EloHistory
            .OrderBy(s => s.RecordedAt)
            .Select(s => new EloSnapshotDto(s.Points, s.RecordedAt))
            .ToList();

        var matchFilter = Builders<MatchResult>.Filter.Eq(m => m.GameId, request.GameId) &
            (Builders<MatchResult>.Filter.Eq(m => m.Player1Id, request.UserId) |
             Builders<MatchResult>.Filter.Eq(m => m.Player2Id, request.UserId));

        var recentMatches = await matchCol.Find(matchFilter)
            .SortByDescending(m => m.CreatedAt)
            .Limit(20)
            .ToListAsync(ct);

        var opponentIds = recentMatches
            .Select(m => m.Player1Id == request.UserId ? m.Player2Id : m.Player1Id)
            .Distinct().ToList();

        var opponentUsers = opponentIds.Count > 0
            ? await usersCol.Find(Builders<User>.Filter.In(u => u.Id, opponentIds)).ToListAsync(ct)
            : [];
        var opUsernameMap = opponentUsers.ToDictionary(u => u.Id, u => u.Username);

        var matchDtos = recentMatches.Select(m =>
        {
            var isPlayer1 = m.Player1Id == request.UserId;
            var opponentId = isPlayer1 ? m.Player2Id : m.Player1Id;
            var won = m.WinnerId == request.UserId;
            var eloChange = isPlayer1 ? m.Player1EloChange : m.Player2EloChange;
            var eloBefore = isPlayer1 ? m.Player1EloBefore : m.Player2EloBefore;

            return new RecentMatchDto(
                m.Id,
                opponentId,
                opUsernameMap.GetValueOrDefault(opponentId, "Unknown"),
                won,
                eloChange,
                eloBefore,
                m.CreatedAt);
        }).ToList();

        return Result<EloHistoryResponse>.Success(new EloHistoryResponse(
            gameProfile.EloPoints,
            gameProfile.RankTier.ToString(),
            request.GameId,
            game?.Name ?? "Unknown",
            history,
            matchDtos));
    }
}
