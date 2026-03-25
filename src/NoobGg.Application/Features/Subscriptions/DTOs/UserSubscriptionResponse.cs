using NoobGg.Application.Common.Interfaces;

namespace NoobGg.Application.Features.Subscriptions.DTOs;

public record UserSubscriptionResponse
{
    public string? SubscriptionId { get; init; }
    public string Tier { get; init; } = "Free";
    public string PlanName { get; init; } = "Free";
    public string Status { get; init; } = "Active";
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public bool AutoRenew { get; init; }
    public EntitlementSnapshot Entitlements { get; init; } = new();
}
