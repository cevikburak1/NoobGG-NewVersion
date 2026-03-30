using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Elo.Commands.SubmitSessionResults;

public record SubmitSessionResultsCommand : IRequest<Result>
{
    public string RoomId { get; init; } = string.Empty;
    public int Wins { get; init; }
    public int Losses { get; init; }
}
