using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Recommendations.DTOs;

namespace NoobGg.Application.Features.Recommendations.Queries.GetRecommendedPlayers;

public record GetRecommendedPlayersQuery : IRequest<Result<List<RecommendedPlayerResponse>>>
{
    public int Limit { get; init; } = 6;
}
