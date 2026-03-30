using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Elo.DTOs;

namespace NoobGg.Application.Features.Elo.Queries.GetEloHistory;

public record GetEloHistoryQuery : IRequest<Result<EloHistoryResponse>>
{
    public string UserId { get; init; } = string.Empty;
    public string GameId { get; init; } = string.Empty;
}
