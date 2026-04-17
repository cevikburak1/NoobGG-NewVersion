using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Tournaments.Commands.JoinTournament;

public class JoinTournamentCommandHandler : IRequestHandler<JoinTournamentCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public JoinTournamentCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(JoinTournamentCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;
        var tournaments = _mongoContext.GetCollection<Tournament>(CollectionNames.Tournaments);

        var tournament = await tournaments.Find(t => t.Id == request.TournamentId).FirstOrDefaultAsync(ct);
        if (tournament is null)
            return Result.Fail("Tournament not found", 404);

        if (tournament.Status != TournamentStatus.Registration)
            return Result.Fail("Tournament is not accepting registrations");

        if (tournament.RegistrationDeadline < DateTime.UtcNow)
            return Result.Fail("Registration deadline has passed");

        var entries = _mongoContext.GetCollection<TournamentEntry>(CollectionNames.TournamentEntries);

        var alreadyRegistered = await entries
            .Find(e => e.TournamentId == request.TournamentId && e.ParticipantId == userId)
            .AnyAsync(ct);
        if (alreadyRegistered)
            return Result.Fail("You are already registered for this tournament");

        if (tournament.CurrentParticipants >= tournament.MaxParticipants)
            return Result.Fail("Tournament is full");

        var entry = new TournamentEntry
        {
            TournamentId = request.TournamentId,
            ParticipantId = userId,
            EntryType = TournamentEntryType.Player,
            Seed = tournament.CurrentParticipants + 1,
            IsEliminated = false,
            Placement = 0,
            EarnedBadges = [],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await entries.InsertOneAsync(entry, cancellationToken: ct);

        await tournaments.UpdateOneAsync(
            t => t.Id == request.TournamentId,
            Builders<Tournament>.Update
                .Inc(t => t.CurrentParticipants, 1)
                .Set(t => t.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        return Result.Success();
    }
}
