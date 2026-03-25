namespace NoobGg.Application.Features.Subscriptions.DTOs;

public record PlanComparisonResponse
{
    public List<PlanResponse> Plans { get; init; } = [];
    public string CurrentTier { get; init; } = "Free";
    public string? CurrentPlanId { get; init; }
}
