using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Tournaments.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Tournaments.Queries.GetTournamentDetail;

public class GetTournamentDetailQueryHandler
    : IRequestHandler<GetTournamentDetailQuery, Result<TournamentDetailResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetTournamentDetailQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<TournamentDetailResponse>> Handle(GetTournamentDetailQuery request, CancellationToken ct)
    {
        var tournaments = _mongoContext.GetCollection<Tournament>(CollectionNames.Tournaments);

        var tournament = await tournaments.Find(t => t.Id == request.TournamentId).FirstOrDefaultAsync(ct);
        if (tournament is null)
            return Result<TournamentDetailResponse>.NotFound("Tournament not found");

        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var game = await games.Find(g => g.Id == tournament.GameId).FirstOrDefaultAsync(ct);
        var gameName = game?.Name ?? "Unknown";

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var organizer = await users.Find(u => u.Id == tournament.OrganizerId).FirstOrDefaultAsync(ct);
        var organizerUsername = organizer?.Username ?? "Unknown";

        var entriesCollection = _mongoContext.GetCollection<TournamentEntry>(CollectionNames.TournamentEntries);
        var entries = await entriesCollection
            .Find(e => e.TournamentId == request.TournamentId)
            .ToListAsync(ct);

        var matchesCollection = _mongoContext.GetCollection<TournamentMatch>(CollectionNames.TournamentMatches);
        var matches = await matchesCollection
            .Find(m => m.TournamentId == request.TournamentId)
            .SortBy(m => m.Round).ThenBy(m => m.MatchNumber)
            .ToListAsync(ct);

        // Resolve participant names
        var participantIds = entries.Select(e => e.ParticipantId)
            .Union(matches.Where(m => m.Participant1Id != null).Select(m => m.Participant1Id!))
            .Union(matches.Where(m => m.Participant2Id != null).Select(m => m.Participant2Id!))
            .Distinct().ToList();

        var participantDocs = await users
            .Find(Builders<User>.Filter.In(u => u.Id, participantIds))
            .ToListAsync(ct);
        var nameMap = participantDocs.ToDictionary(u => u.Id, u => u.Username);

        var entryResponses = entries.Select(e => new TournamentEntryResponse(
            e.Id, e.ParticipantId, nameMap.GetValueOrDefault(e.ParticipantId, "Unknown"),
            e.EntryType.ToString(), e.GuildId, e.Seed, e.IsEliminated,
            e.Placement, e.EarnedBadges
        )).ToList();

        var matchResponses = matches.Select(m => new TournamentMatchResponse(
            m.Id, m.Round, m.MatchNumber,
            m.Participant1Id, m.Participant1Id is not null ? nameMap.GetValueOrDefault(m.Participant1Id, "Unknown") : null,
            m.Participant2Id, m.Participant2Id is not null ? nameMap.GetValueOrDefault(m.Participant2Id, "Unknown") : null,
            m.WinnerId, m.Status.ToString(), m.NextMatchId
        )).ToList();

        var isParticipant = _currentUser.IsAuthenticated && _currentUser.UserId is not null &&
                            entries.Any(e => e.ParticipantId == _currentUser.UserId);

        var response = new TournamentDetailResponse(
            tournament.Id, tournament.Name, tournament.Description,
            tournament.GameId, gameName,
            tournament.OrganizerId, organizerUsername, tournament.GuildId,
            tournament.Format.ToString(), tournament.Status.ToString(),
            tournament.MaxParticipants, tournament.CurrentParticipants,
            tournament.RegistrationDeadline, tournament.StartsAt,
            tournament.CurrentRound, tournament.TotalRounds, tournament.PrizeBadges,
            entryResponses, matchResponses,
            isParticipant, tournament.CreatedAt);

        return Result<TournamentDetailResponse>.Success(response);
    }
}
