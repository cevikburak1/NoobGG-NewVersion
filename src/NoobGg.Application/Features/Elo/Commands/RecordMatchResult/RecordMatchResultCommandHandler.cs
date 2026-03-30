using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Elo.Helpers;
using NoobGg.Domain.Entities;
using NoobGg.Domain.ValueObjects;

namespace NoobGg.Application.Features.Elo.Commands.RecordMatchResult;

public class RecordMatchResultCommandHandler : IRequestHandler<RecordMatchResultCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public RecordMatchResultCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RecordMatchResultCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var myId = _currentUser.UserId;
        if (myId == request.OpponentId)
            return Result.Fail("Cannot record a match against yourself");

        var gpCol = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);

        var myProfile = await gpCol
            .Find(p => p.UserId == myId && p.GameId == request.GameId)
            .FirstOrDefaultAsync(ct);
        if (myProfile is null)
            return Result.Fail("You don't have a game profile for this game", 404);

        var opponentProfile = await gpCol
            .Find(p => p.UserId == request.OpponentId && p.GameId == request.GameId)
            .FirstOrDefaultAsync(ct);
        if (opponentProfile is null)
            return Result.Fail("Opponent doesn't have a game profile for this game", 404);

        var (change1, change2) = EloCalculator.Calculate(
            myProfile.EloPoints, opponentProfile.EloPoints, request.Won);

        var now = DateTime.UtcNow;
        var myNewElo = Math.Max(0, myProfile.EloPoints + change1);
        var opNewElo = Math.Max(0, opponentProfile.EloPoints + change2);

        var mySnapshot = new EloSnapshot { Points = myNewElo, RecordedAt = now };
        var opSnapshot = new EloSnapshot { Points = opNewElo, RecordedAt = now };

        var myUpdate = Builders<UserGameProfile>.Update
            .Set(p => p.EloPoints, myNewElo)
            .Set(p => p.RankTier, EloCalculator.GetTier(myNewElo))
            .Set(p => p.Rank, EloCalculator.GetTier(myNewElo).ToString())
            .Push(p => p.EloHistory, mySnapshot)
            .Set(p => p.UpdatedAt, now);

        var opUpdate = Builders<UserGameProfile>.Update
            .Set(p => p.EloPoints, opNewElo)
            .Set(p => p.RankTier, EloCalculator.GetTier(opNewElo))
            .Set(p => p.Rank, EloCalculator.GetTier(opNewElo).ToString())
            .Push(p => p.EloHistory, opSnapshot)
            .Set(p => p.UpdatedAt, now);

        await gpCol.UpdateOneAsync(p => p.Id == myProfile.Id, myUpdate, cancellationToken: ct);
        await gpCol.UpdateOneAsync(p => p.Id == opponentProfile.Id, opUpdate, cancellationToken: ct);

        await TrimEloHistory(gpCol, myProfile.Id, ct);
        await TrimEloHistory(gpCol, opponentProfile.Id, ct);

        var matchResult = new MatchResult
        {
            GameId = request.GameId,
            Player1Id = myId,
            Player2Id = request.OpponentId,
            WinnerId = request.Won ? myId : request.OpponentId,
            Player1EloBefore = myProfile.EloPoints,
            Player2EloBefore = opponentProfile.EloPoints,
            Player1EloChange = change1,
            Player2EloChange = change2,
            CreatedAt = now
        };

        var matchCol = _mongoContext.GetCollection<MatchResult>(CollectionNames.MatchResults);
        await matchCol.InsertOneAsync(matchResult, cancellationToken: ct);

        return Result.Success();
    }

    private static async Task TrimEloHistory(
        IMongoCollection<UserGameProfile> col, string profileId, CancellationToken ct)
    {
        var profile = await col.Find(p => p.Id == profileId)
            .Project(p => new { p.EloHistory })
            .FirstOrDefaultAsync(ct);

        if (profile?.EloHistory is { Count: > 30 })
        {
            var trimmed = profile.EloHistory
                .OrderByDescending(s => s.RecordedAt)
                .Take(30)
                .OrderBy(s => s.RecordedAt)
                .ToList();

            await col.UpdateOneAsync(
                p => p.Id == profileId,
                Builders<UserGameProfile>.Update.Set(p => p.EloHistory, trimmed),
                cancellationToken: ct);
        }
    }
}
