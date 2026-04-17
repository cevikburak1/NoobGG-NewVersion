namespace NoobGg.Application.Features.Recommendations.DTOs;

public record AiRecommendedPlayersResponse
{
    public List<RecommendedPlayerResponse> Players { get; init; } = [];
    public bool UsedAi { get; init; }
}
