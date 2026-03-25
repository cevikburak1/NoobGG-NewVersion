using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Games.DTOs;

namespace NoobGg.Application.Features.Games.Queries.SearchGames;

public record SearchGamesQuery : IRequest<Result<List<GameResponse>>>
{
    public string SearchTerm { get; init; } = string.Empty;
    public int Limit { get; init; } = 10;
    public bool? IsMultiplayer { get; init; }
    public bool? IsCoop { get; init; }
    public string? Genre { get; init; }
}
