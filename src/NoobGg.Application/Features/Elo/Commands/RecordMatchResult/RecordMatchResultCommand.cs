using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Elo.Commands.RecordMatchResult;

public record RecordMatchResultCommand : IRequest<Result>
{
    public string GameId { get; init; } = string.Empty;
    public string OpponentId { get; init; } = string.Empty;
    public bool Won { get; init; }
}
