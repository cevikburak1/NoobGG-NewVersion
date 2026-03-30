using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Elo.Helpers;
using NoobGg.Domain.Entities;
using NoobGg.Domain.ValueObjects;

namespace NoobGg.Application.Features.Elo.Commands.SubmitSessionResults;

public class SubmitSessionResultsCommandHandler : IRequestHandler<SubmitSessionResultsCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public SubmitSessionResultsCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(SubmitSessionResultsCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var myId = _currentUser.UserId;

        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var room = await rooms.Find(r => r.Id == request.RoomId).FirstOrDefaultAsync(ct);
        if (room is null)
            return Result.Fail("Room not found", 404);

        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        var isMember = await roomMembers
            .Find(m => m.RoomId == request.RoomId && m.UserId == myId)
            .AnyAsync(ct);
        if (!isMember)
            return Result.Fail("You are not a member of this room", 403);

        var gpCol = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);

        var myProfile = await gpCol
            .Find(p => p.UserId == myId && p.GameId == room.GameId)
            .FirstOrDefaultAsync(ct);
        if (myProfile is null)
            return Result.Fail("You don't have a game profile for this game", 404);

        var otherMemberIds = await roomMembers
            .Find(m => m.RoomId == request.RoomId && m.UserId != myId)
            .Project(m => m.UserId)
            .ToListAsync(ct);

        var avgOpponentElo = myProfile.EloPoints;
        if (otherMemberIds.Count > 0)
        {
            var opponentProfiles = await gpCol
                .Find(p => otherMemberIds.Contains(p.UserId) && p.GameId == room.GameId)
                .ToListAsync(ct);

            if (opponentProfiles.Count > 0)
                avgOpponentElo = (int)Math.Round(opponentProfiles.Average(p => p.EloPoints));
        }

        var currentElo = myProfile.EloPoints;
        var totalChange = 0;
        var now = DateTime.UtcNow;

        for (var i = 0; i < request.Wins; i++)
        {
            var (change, _) = EloCalculator.Calculate(currentElo, avgOpponentElo, true);
            currentElo = Math.Max(0, currentElo + change);
            totalChange += change;
        }

        for (var i = 0; i < request.Losses; i++)
        {
            var (change, _) = EloCalculator.Calculate(currentElo, avgOpponentElo, false);
            currentElo = Math.Max(0, currentElo + change);
            totalChange += change;
        }

        var snapshot = new EloSnapshot { Points = currentElo, RecordedAt = now };

        var update = Builders<UserGameProfile>.Update
            .Set(p => p.EloPoints, currentElo)
            .Set(p => p.RankTier, EloCalculator.GetTier(currentElo))
            .Set(p => p.Rank, EloCalculator.GetTier(currentElo).ToString())
            .Push(p => p.EloHistory, snapshot)
            .Set(p => p.UpdatedAt, now);

        await gpCol.UpdateOneAsync(p => p.Id == myProfile.Id, update, cancellationToken: ct);
        await TrimEloHistory(gpCol, myProfile.Id, ct);

        var matchCol = _mongoContext.GetCollection<MatchResult>(CollectionNames.MatchResults);
        var matchResult = new MatchResult
        {
            GameId = room.GameId,
            Player1Id = myId,
            Player2Id = "room:" + request.RoomId,
            WinnerId = totalChange >= 0 ? myId : "room:" + request.RoomId,
            Player1EloBefore = myProfile.EloPoints,
            Player2EloBefore = avgOpponentElo,
            Player1EloChange = totalChange,
            Player2EloChange = 0,
            CreatedAt = now
        };
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
