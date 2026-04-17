using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Recommendations.DTOs;

namespace NoobGg.Application.Features.Recommendations.Queries.GetAiRecommendedPlayers;

public record GetAiRecommendedPlayersQuery : IRequest<Result<AiRecommendedPlayersResponse>>
{
    public int Limit { get; init; } = 10;
}
