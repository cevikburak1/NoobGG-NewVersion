using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Tournaments.Commands.LeaveTournament;

public class LeaveTournamentCommandHandler : IRequestHandler<LeaveTournamentCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public LeaveTournamentCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(LeaveTournamentCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var tournaments = _mongoContext.GetCollection<Tournament>(CollectionNames.Tournaments);

        var tournament = await tournaments.Find(t => t.Id == request.TournamentId).FirstOrDefaultAsync(ct);
        if (tournament is null)
            return Result.Fail("Tournament not found", 404);

        if (tournament.Status != TournamentStatus.Registration)
            return Result.Fail("Can only leave a tournament during registration phase");

        var entries = _mongoContext.GetCollection<TournamentEntry>(CollectionNames.TournamentEntries);

        var deleteResult = await entries.DeleteOneAsync(
            e => e.TournamentId == request.TournamentId && e.ParticipantId == userId,
            ct);

        if (deleteResult.DeletedCount == 0)
            return Result.Fail("You are not registered for this tournament", 404);

        await tournaments.UpdateOneAsync(
            t => t.Id == request.TournamentId,
            Builders<Tournament>.Update
                .Inc(t => t.CurrentParticipants, -1)
                .Set(t => t.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return Result.Success();
    }
}
