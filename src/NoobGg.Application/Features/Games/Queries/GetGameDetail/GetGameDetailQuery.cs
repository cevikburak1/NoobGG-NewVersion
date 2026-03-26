using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Games.DTOs;

namespace NoobGg.Application.Features.Games.Queries.GetGameDetail;

public record GetGameDetailQuery : IRequest<Result<GameResponse>>
{
    public string GameId { get; init; } = string.Empty;
}
