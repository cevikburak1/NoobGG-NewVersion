using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Tournaments.Commands.JoinTournament;

public record JoinTournamentCommand : IRequest<Result>
{
    public string TournamentId { get; init; } = string.Empty;
}
