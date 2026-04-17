using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Tournaments.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Tournaments.Commands.CreateTournament;

public class CreateTournamentCommandHandler : IRequestHandler<CreateTournamentCommand, Result<TournamentDetailResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public CreateTournamentCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<TournamentDetailResponse>> Handle(CreateTournamentCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<TournamentDetailResponse>.Unauthorized();

        var userId = _currentUser.UserId;
        var username = _currentUser.Username ?? "Unknown";

        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var game = await games.Find(g => g.Id == request.GameId && g.IsActive).FirstOrDefaultAsync(ct);
        if (game is null)
            return Result<TournamentDetailResponse>.NotFound("Game not found or inactive");

        if (request.GuildId is not null)
        {
            var guildMembers = _mongoContext.GetCollection<GuildMember>(CollectionNames.GuildMembers);
            var member = await guildMembers.Find(gm =>
                    gm.GuildId == request.GuildId && gm.UserId == userId)
                .FirstOrDefaultAsync(ct);

            if (member is null)
                return Result<TournamentDetailResponse>.Forbidden("You are not a member of this guild");

            if (member.Role != GuildMemberRole.Owner && member.Role != GuildMemberRole.Admin)
                return Result<TournamentDetailResponse>.Forbidden("Only guild admins or owners can create guild tournaments");
        }

        var totalRounds = CalculateTotalRounds(request.MaxParticipants, request.Format);

        var tournament = new Tournament
        {
            Name = request.Name,
            Description = request.Description,
            GameId = request.GameId,
            OrganizerId = userId,
            GuildId = request.GuildId,
            Format = request.Format,
            Status = TournamentStatus.Registration,
            MaxParticipants = request.MaxParticipants,
            CurrentParticipants = 0,
            RegistrationDeadline = request.RegistrationDeadline,
            StartsAt = request.StartsAt,
            CurrentRound = 0,
            TotalRounds = totalRounds,
            PrizeBadges = request.PrizeBadges,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var tournaments = _mongoContext.GetCollection<Tournament>(CollectionNames.Tournaments);
        await tournaments.InsertOneAsync(tournament, cancellationToken: ct);

        var response = new TournamentDetailResponse(
            tournament.Id, tournament.Name, tournament.Description,
            tournament.GameId, game.Name,
            tournament.OrganizerId, username, tournament.GuildId,
            tournament.Format.ToString(), tournament.Status.ToString(),
            tournament.MaxParticipants, tournament.CurrentParticipants,
            tournament.RegistrationDeadline, tournament.StartsAt,
            tournament.CurrentRound, tournament.TotalRounds, tournament.PrizeBadges,
            [], [],
            false, tournament.CreatedAt);

        return Result<TournamentDetailResponse>.Created(response);
    }

    private static int CalculateTotalRounds(int maxParticipants, TournamentFormat format)
    {
        var rounds = (int)Math.Ceiling(Math.Log2(maxParticipants));
        return format == TournamentFormat.DoubleElimination ? rounds * 2 : rounds;
    }
}
