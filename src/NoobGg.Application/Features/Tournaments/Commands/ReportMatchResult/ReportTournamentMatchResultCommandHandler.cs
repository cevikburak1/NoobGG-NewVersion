using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Tournaments.Commands.ReportMatchResult;

public class ReportTournamentMatchResultCommandHandler
    : IRequestHandler<ReportTournamentMatchResultCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public ReportTournamentMatchResultCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ReportTournamentMatchResultCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var matchesCollection = _mongoContext.GetCollection<TournamentMatch>(CollectionNames.TournamentMatches);

        var match = await matchesCollection.Find(m => m.Id == request.MatchId).FirstOrDefaultAsync(ct);
        if (match is null)
            return Result.Fail("Match not found", 404);

        var tournaments = _mongoContext.GetCollection<Tournament>(CollectionNames.Tournaments);
        var tournament = await tournaments.Find(t => t.Id == match.TournamentId).FirstOrDefaultAsync(ct);
        if (tournament is null)
            return Result.Fail("Tournament not found", 404);

        if (tournament.OrganizerId != userId)
            return Result.Fail("Only the organizer can report match results", 403);

        if (match.Status == TournamentMatchStatus.Completed)
            return Result.Fail("Match result has already been reported");

        if (match.Status == TournamentMatchStatus.Bye)
            return Result.Fail("Cannot report result for a bye match");

        if (request.WinnerId != match.Participant1Id && request.WinnerId != match.Participant2Id)
            return Result.Fail("Winner must be one of the match participants");

        var loserId = request.WinnerId == match.Participant1Id
            ? match.Participant2Id
            : match.Participant1Id;

        await matchesCollection.UpdateOneAsync(
            m => m.Id == request.MatchId,
            Builders<TournamentMatch>.Update
                .Set(m => m.WinnerId, request.WinnerId)
                .Set(m => m.Status, TournamentMatchStatus.Completed)
                .Set(m => m.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        // Advance winner to next match
        if (match.NextMatchId is not null)
        {
            var nextMatch = await matchesCollection.Find(m => m.Id == match.NextMatchId).FirstOrDefaultAsync(ct);
            if (nextMatch is not null)
            {
                var update = nextMatch.Participant1Id is null
                    ? Builders<TournamentMatch>.Update.Set(m => m.Participant1Id, request.WinnerId)
                    : Builders<TournamentMatch>.Update.Set(m => m.Participant2Id, request.WinnerId);

                await matchesCollection.UpdateOneAsync(
                    m => m.Id == match.NextMatchId,
                    Builders<TournamentMatch>.Update.Combine(
                        update, Builders<TournamentMatch>.Update.Set(m => m.UpdatedAt, DateTime.UtcNow)),
                    cancellationToken: ct);
            }
        }

        // Handle loser: advance to loser bracket or eliminate
        var entriesCollection = _mongoContext.GetCollection<TournamentEntry>(CollectionNames.TournamentEntries);

        if (match.LoserNextMatchId is not null && loserId is not null)
        {
            var loserNextMatch = await matchesCollection
                .Find(m => m.Id == match.LoserNextMatchId)
                .FirstOrDefaultAsync(ct);

            if (loserNextMatch is not null)
            {
                var loserUpdate = loserNextMatch.Participant1Id is null
                    ? Builders<TournamentMatch>.Update.Set(m => m.Participant1Id, loserId)
                    : Builders<TournamentMatch>.Update.Set(m => m.Participant2Id, loserId);

                await matchesCollection.UpdateOneAsync(
                    m => m.Id == match.LoserNextMatchId,
                    Builders<TournamentMatch>.Update.Combine(
                        loserUpdate, Builders<TournamentMatch>.Update.Set(m => m.UpdatedAt, DateTime.UtcNow)),
                    cancellationToken: ct);
            }
        }
        else if (loserId is not null)
        {
            await entriesCollection.UpdateOneAsync(
                e => e.TournamentId == tournament.Id && e.ParticipantId == loserId,
                Builders<TournamentEntry>.Update
                    .Set(e => e.IsEliminated, true)
                    .Set(e => e.UpdatedAt, DateTime.UtcNow),
                cancellationToken: ct);
        }

        // Check if all matches in the current round are completed
        var currentRoundPending = await matchesCollection.CountDocumentsAsync(
            m => m.TournamentId == tournament.Id &&
                 m.Round == tournament.CurrentRound &&
                 m.Status != TournamentMatchStatus.Completed &&
                 m.Status != TournamentMatchStatus.Bye,
            cancellationToken: ct);

        if (currentRoundPending == 0)
        {
            // Check if this was the final round
            var anyPendingMatches = await matchesCollection.CountDocumentsAsync(
                m => m.TournamentId == tournament.Id &&
                     m.Status != TournamentMatchStatus.Completed &&
                     m.Status != TournamentMatchStatus.Bye,
                cancellationToken: ct);

            if (anyPendingMatches == 0)
            {
                // Tournament is complete
                await tournaments.UpdateOneAsync(
                    t => t.Id == tournament.Id,
                    Builders<Tournament>.Update
                        .Set(t => t.Status, TournamentStatus.Completed)
                        .Set(t => t.UpdatedAt, DateTime.UtcNow),
                    cancellationToken: ct);

                // Set winner placement and distribute badges
                await entriesCollection.UpdateOneAsync(
                    e => e.TournamentId == tournament.Id && e.ParticipantId == request.WinnerId,
                    Builders<TournamentEntry>.Update
                        .Set(e => e.Placement, 1)
                        .Set(e => e.EarnedBadges, tournament.PrizeBadges)
                        .Set(e => e.UpdatedAt, DateTime.UtcNow),
                    cancellationToken: ct);

                // Set runner-up placement
                if (loserId is not null)
                {
                    await entriesCollection.UpdateOneAsync(
                        e => e.TournamentId == tournament.Id && e.ParticipantId == loserId,
                        Builders<TournamentEntry>.Update
                            .Set(e => e.Placement, 2)
                            .Set(e => e.IsEliminated, true)
                            .Set(e => e.UpdatedAt, DateTime.UtcNow),
                        cancellationToken: ct);
                }
            }
            else
            {
                await tournaments.UpdateOneAsync(
                    t => t.Id == tournament.Id,
                    Builders<Tournament>.Update
                        .Inc(t => t.CurrentRound, 1)
                        .Set(t => t.UpdatedAt, DateTime.UtcNow),
                    cancellationToken: ct);
            }
        }

        return Result.Success();
    }
}
