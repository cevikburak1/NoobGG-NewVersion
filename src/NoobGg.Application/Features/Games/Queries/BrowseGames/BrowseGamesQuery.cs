using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Games.DTOs;

namespace NoobGg.Application.Features.Games.Queries.BrowseGames;

public record BrowseGamesQuery : IRequest<Result<PagedResult<GameResponse>>>
{
    public string? Search { get; init; }
    public string? Genre { get; init; }
    public string? Platform { get; init; }
    public bool? IsMultiplayer { get; init; }
    public bool? IsCoop { get; init; }
    public bool? IsPvp { get; init; }
    public bool? IsFreeToPlay { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
}
