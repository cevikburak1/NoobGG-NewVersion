using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Tournaments.Commands.GenerateBracket;

public record GenerateBracketCommand : IRequest<Result>
{
    public string TournamentId { get; init; } = string.Empty;
}
