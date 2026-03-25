using NoobGg.Domain.Enums;

namespace NoobGg.Application.Common.Interfaces;

public interface IEntitlementService
{
    Task<bool> HasFeatureAsync(string userId, string featureKey, CancellationToken ct = default);
    Task<int> GetLimitAsync(string userId, string limitName, CancellationToken ct = default);
    Task<EntitlementSnapshot> GetEntitlementsAsync(string userId, CancellationToken ct = default);
    Task<SubscriptionTier> GetActiveTierAsync(string userId, CancellationToken ct = default);
    Task InvalidateCacheAsync(string userId, CancellationToken ct = default);
}

public class EntitlementSnapshot
{
    public SubscriptionTier Tier { get; init; }
    public string PlanName { get; init; } = "Free";
    public List<string> Features { get; init; } = [];
    public int MaxRoomsPerDay { get; init; }
    public int MaxGameProfiles { get; init; }
    public bool HasPremiumBadge { get; init; }
}
