using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Elo.DTOs;

namespace NoobGg.Application.Features.Elo.Queries.GetLeaderboard;

public record GetLeaderboardQuery : IRequest<Result<PagedResult<LeaderboardEntryResponse>>>
{
    public string GameId { get; init; } = string.Empty;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
