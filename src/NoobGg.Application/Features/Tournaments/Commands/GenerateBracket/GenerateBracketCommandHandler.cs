using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Tournaments.Commands.GenerateBracket;

public class GenerateBracketCommandHandler : IRequestHandler<GenerateBracketCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GenerateBracketCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(GenerateBracketCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var tournaments = _mongoContext.GetCollection<Tournament>(CollectionNames.Tournaments);

        var tournament = await tournaments.Find(t => t.Id == request.TournamentId).FirstOrDefaultAsync(ct);
        if (tournament is null)
            return Result.Fail("Tournament not found", 404);

        if (tournament.OrganizerId != userId)
            return Result.Fail("Only the organizer can generate the bracket", 403);

        if (tournament.Status != TournamentStatus.Registration)
            return Result.Fail("Bracket can only be generated during registration phase");

        var entriesCollection = _mongoContext.GetCollection<TournamentEntry>(CollectionNames.TournamentEntries);
        var entries = await entriesCollection
            .Find(e => e.TournamentId == request.TournamentId)
            .ToListAsync(ct);

        if (entries.Count < 2)
            return Result.Fail("At least 2 participants are required to generate a bracket");

        ShuffleEntries(entries);
        for (var i = 0; i < entries.Count; i++)
        {
            entries[i].Seed = i + 1;
            await entriesCollection.UpdateOneAsync(
                e => e.Id == entries[i].Id,
                Builders<TournamentEntry>.Update
                    .Set(e => e.Seed, entries[i].Seed)
                    .Set(e => e.UpdatedAt, DateTime.UtcNow),
                cancellationToken: ct);
        }

        var matchesCollection = _mongoContext.GetCollection<TournamentMatch>(CollectionNames.TournamentMatches);

        await matchesCollection.DeleteManyAsync(
            m => m.TournamentId == request.TournamentId, ct);

        if (tournament.Format == TournamentFormat.SingleElimination)
            await GenerateSingleEliminationBracket(matchesCollection, tournament, entries, ct);
        else
            await GenerateDoubleEliminationBracket(matchesCollection, tournament, entries, ct);

        var totalRounds = tournament.Format == TournamentFormat.DoubleElimination
            ? (int)Math.Ceiling(Math.Log2(entries.Count)) * 2
            : (int)Math.Ceiling(Math.Log2(entries.Count));

        await tournaments.UpdateOneAsync(
            t => t.Id == request.TournamentId,
            Builders<Tournament>.Update
                .Set(t => t.Status, TournamentStatus.InProgress)
                .Set(t => t.CurrentRound, 1)
                .Set(t => t.TotalRounds, totalRounds)
                .Set(t => t.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return Result.Success();
    }

    private static async Task GenerateSingleEliminationBracket(
        IMongoCollection<TournamentMatch> matchesCollection,
        Tournament tournament,
        List<TournamentEntry> entries,
        CancellationToken ct)
    {
        var totalRounds = (int)Math.Ceiling(Math.Log2(entries.Count));
        var bracketSize = (int)Math.Pow(2, totalRounds);
        var byeCount = bracketSize - entries.Count;

        var allMatches = new List<TournamentMatch>();

        // Pre-create all matches for every round so we can wire NextMatchId
        for (var round = 1; round <= totalRounds; round++)
        {
            var matchesInRound = bracketSize / (int)Math.Pow(2, round);
            for (var m = 1; m <= matchesInRound; m++)
            {
                allMatches.Add(new TournamentMatch
                {
                    TournamentId = tournament.Id,
                    Round = round,
                    MatchNumber = m,
                    Status = TournamentMatchStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        // Wire NextMatchId: match N in round R feeds into match ceil(N/2) in round R+1
        foreach (var match in allMatches.Where(m => m.Round < totalRounds))
        {
            var nextMatchNumber = (int)Math.Ceiling(match.MatchNumber / 2.0);
            var nextMatch = allMatches.First(nm => nm.Round == match.Round + 1 && nm.MatchNumber == nextMatchNumber);
            match.NextMatchId = nextMatch.Id;
        }

        // Fill round 1 with participants
        var round1Matches = allMatches.Where(m => m.Round == 1).OrderBy(m => m.MatchNumber).ToList();
        var entryIndex = 0;

        for (var i = 0; i < round1Matches.Count; i++)
        {
            var match = round1Matches[i];

            if (entryIndex < entries.Count)
                match.Participant1Id = entries[entryIndex++].ParticipantId;

            if (entryIndex < entries.Count)
            {
                match.Participant2Id = entries[entryIndex++].ParticipantId;
            }
            else
            {
                // Bye: participant 1 auto-advances
                match.Status = TournamentMatchStatus.Bye;
                match.WinnerId = match.Participant1Id;

                if (match.NextMatchId is not null)
                {
                    var nextMatch = allMatches.First(nm => nm.Id == match.NextMatchId);
                    if (nextMatch.Participant1Id is null)
                        nextMatch.Participant1Id = match.Participant1Id;
                    else
                        nextMatch.Participant2Id = match.Participant1Id;
                }
            }
        }

        if (allMatches.Count > 0)
            await matchesCollection.InsertManyAsync(allMatches, cancellationToken: ct);
    }

    private static async Task GenerateDoubleEliminationBracket(
        IMongoCollection<TournamentMatch> matchesCollection,
        Tournament tournament,
        List<TournamentEntry> entries,
        CancellationToken ct)
    {
        var winnerRounds = (int)Math.Ceiling(Math.Log2(entries.Count));
        var bracketSize = (int)Math.Pow(2, winnerRounds);
        var loserRounds = winnerRounds; // Loser bracket has roughly the same number of rounds

        var allMatches = new List<TournamentMatch>();
        var roundOffset = 0;

        // Winner bracket matches
        for (var round = 1; round <= winnerRounds; round++)
        {
            var matchesInRound = bracketSize / (int)Math.Pow(2, round);
            for (var m = 1; m <= matchesInRound; m++)
            {
                allMatches.Add(new TournamentMatch
                {
                    TournamentId = tournament.Id,
                    Round = round,
                    MatchNumber = m,
                    Status = TournamentMatchStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        roundOffset = winnerRounds;

        // Loser bracket matches
        for (var round = 1; round <= loserRounds; round++)
        {
            var matchesInRound = Math.Max(1, bracketSize / (int)Math.Pow(2, (round + 1) / 2 + 1));
            for (var m = 1; m <= matchesInRound; m++)
            {
                allMatches.Add(new TournamentMatch
                {
                    TournamentId = tournament.Id,
                    Round = roundOffset + round,
                    MatchNumber = m,
                    Status = TournamentMatchStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        // Grand final
        allMatches.Add(new TournamentMatch
        {
            TournamentId = tournament.Id,
            Round = roundOffset + loserRounds + 1,
            MatchNumber = 1,
            Status = TournamentMatchStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Wire winner bracket NextMatchId
        var winnerMatches = allMatches.Where(m => m.Round <= winnerRounds).ToList();
        foreach (var match in winnerMatches.Where(m => m.Round < winnerRounds))
        {
            var nextMatchNumber = (int)Math.Ceiling(match.MatchNumber / 2.0);
            var nextMatch = winnerMatches.First(nm => nm.Round == match.Round + 1 && nm.MatchNumber == nextMatchNumber);
            match.NextMatchId = nextMatch.Id;
        }

        // Winner bracket final feeds into grand final
        var winnerFinal = winnerMatches.First(m => m.Round == winnerRounds);
        var grandFinal = allMatches.First(m => m.Round == roundOffset + loserRounds + 1);
        winnerFinal.NextMatchId = grandFinal.Id;

        // Wire loser bracket NextMatchId
        var loserMatches = allMatches
            .Where(m => m.Round > winnerRounds && m.Round <= roundOffset + loserRounds)
            .ToList();

        for (var i = 0; i < loserMatches.Count; i++)
        {
            var match = loserMatches[i];
            var nextRoundMatches = loserMatches.Where(lm => lm.Round == match.Round + 1).ToList();

            if (nextRoundMatches.Count > 0)
            {
                var nextMatchNumber = Math.Min((int)Math.Ceiling(match.MatchNumber / 2.0), nextRoundMatches.Count);
                var nextMatch = nextRoundMatches.First(nm => nm.MatchNumber == nextMatchNumber);
                match.NextMatchId = nextMatch.Id;
            }
        }

        // Loser bracket final feeds into grand final
        var loserFinal = loserMatches.LastOrDefault();
        if (loserFinal is not null)
            loserFinal.NextMatchId = grandFinal.Id;

        // Wire LoserNextMatchId: round 1 winner bracket losers go to loser bracket round 1
        var winnerRound1 = winnerMatches.Where(m => m.Round == 1).OrderBy(m => m.MatchNumber).ToList();
        var loserRound1 = loserMatches.Where(m => m.Round == winnerRounds + 1).OrderBy(m => m.MatchNumber).ToList();

        for (var i = 0; i < winnerRound1.Count && i < loserRound1.Count; i++)
        {
            winnerRound1[i].LoserNextMatchId = loserRound1[i].Id;
        }

        // Subsequent winner bracket rounds also feed losers into the loser bracket
        for (var round = 2; round <= winnerRounds; round++)
        {
            var roundMatches = winnerMatches.Where(m => m.Round == round).OrderBy(m => m.MatchNumber).ToList();
            var targetLoserRound = loserMatches
                .Where(m => m.Round == winnerRounds + round)
                .OrderBy(m => m.MatchNumber).ToList();

            for (var i = 0; i < roundMatches.Count && i < targetLoserRound.Count; i++)
            {
                roundMatches[i].LoserNextMatchId = targetLoserRound[i].Id;
            }
        }

        // Fill round 1 with participants
        var round1Matches = winnerRound1;
        var entryIndex = 0;

        for (var i = 0; i < round1Matches.Count; i++)
        {
            var match = round1Matches[i];

            if (entryIndex < entries.Count)
                match.Participant1Id = entries[entryIndex++].ParticipantId;

            if (entryIndex < entries.Count)
            {
                match.Participant2Id = entries[entryIndex++].ParticipantId;
            }
            else
            {
                match.Status = TournamentMatchStatus.Bye;
                match.WinnerId = match.Participant1Id;

                if (match.NextMatchId is not null)
                {
                    var nextMatch = allMatches.First(nm => nm.Id == match.NextMatchId);
                    if (nextMatch.Participant1Id is null)
                        nextMatch.Participant1Id = match.Participant1Id;
                    else
                        nextMatch.Participant2Id = match.Participant1Id;
                }
            }
        }

        if (allMatches.Count > 0)
            await matchesCollection.InsertManyAsync(allMatches, cancellationToken: ct);
    }

    private static void ShuffleEntries(List<TournamentEntry> entries)
    {
        var rng = Random.Shared;
        for (var i = entries.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (entries[i], entries[j]) = (entries[j], entries[i]);
        }
    }
}
