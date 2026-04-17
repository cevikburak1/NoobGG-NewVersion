using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Tournaments.Commands.LeaveTournament;

public record LeaveTournamentCommand : IRequest<Result>
{
    public string TournamentId { get; init; } = string.Empty;
}
