using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Tournaments.Commands.ReportMatchResult;

public record ReportTournamentMatchResultCommand : IRequest<Result>
{
    public string MatchId { get; init; } = string.Empty;
    public string WinnerId { get; init; } = string.Empty;
}
