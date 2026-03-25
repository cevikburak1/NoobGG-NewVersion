using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Infrastructure.Subscriptions;

public class EntitlementService : IEntitlementService
{
    private readonly IMongoContext _mongoContext;
    private readonly ICacheService _cacheService;
    private readonly ILogger<EntitlementService> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string CachePrefix = "entitlement:";

    public EntitlementService(
        IMongoContext mongoContext,
        ICacheService cacheService,
        ILogger<EntitlementService> logger)
    {
        _mongoContext = mongoContext;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<bool> HasFeatureAsync(string userId, string featureKey, CancellationToken ct = default)
    {
        var snapshot = await GetEntitlementsAsync(userId, ct);
        return snapshot.Features.Contains(featureKey);
    }

    public async Task<int> GetLimitAsync(string userId, string limitName, CancellationToken ct = default)
    {
        var snapshot = await GetEntitlementsAsync(userId, ct);

        return limitName switch
        {
            nameof(EntitlementSnapshot.MaxRoomsPerDay) => snapshot.MaxRoomsPerDay,
            nameof(EntitlementSnapshot.MaxGameProfiles) => snapshot.MaxGameProfiles,
            _ => 0
        };
    }

    public async Task<SubscriptionTier> GetActiveTierAsync(string userId, CancellationToken ct = default)
    {
        var snapshot = await GetEntitlementsAsync(userId, ct);
        return snapshot.Tier;
    }

    public async Task<EntitlementSnapshot> GetEntitlementsAsync(string userId, CancellationToken ct = default)
    {
        var cacheKey = CachePrefix + userId;

        var cached = await _cacheService.GetAsync<EntitlementSnapshot>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var snapshot = await BuildSnapshotAsync(userId, ct);

        await _cacheService.SetAsync(cacheKey, snapshot, CacheDuration, ct);

        return snapshot;
    }

    public async Task InvalidateCacheAsync(string userId, CancellationToken ct = default)
    {
        var cacheKey = CachePrefix + userId;
        await _cacheService.RemoveAsync(cacheKey, ct);
        _logger.LogDebug("Entitlement cache invalidated for user {UserId}", userId);
    }

    private async Task<EntitlementSnapshot> BuildSnapshotAsync(string userId, CancellationToken ct)
    {
        var subs = _mongoContext.GetCollection<UserSubscription>(CollectionNames.UserSubscriptions);
        var activeSub = await subs
            .Find(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .SortByDescending(s => s.EndDate)
            .FirstOrDefaultAsync(ct);

        if (activeSub is null || !activeSub.IsActive)
            return BuildFreeTierSnapshot();

        var plans = _mongoContext.GetCollection<SubscriptionPlan>(CollectionNames.SubscriptionPlans);
        var plan = await plans.Find(p => p.Id == activeSub.PlanId).FirstOrDefaultAsync(ct);

        if (plan is null)
        {
            _logger.LogWarning(
                "Active subscription {SubId} references missing plan {PlanId}",
                activeSub.Id, activeSub.PlanId);
            return BuildFreeTierSnapshot();
        }

        return new EntitlementSnapshot
        {
            Tier = plan.Tier,
            PlanName = plan.Name,
            Features = plan.Features,
            MaxRoomsPerDay = plan.MaxRoomsPerDay,
            MaxGameProfiles = plan.MaxGameProfiles,
            HasPremiumBadge = plan.Features.Contains(PremiumFeature.PremiumBadge)
        };
    }

    private static EntitlementSnapshot BuildFreeTierSnapshot()
    {
        var free = PlanDefaults.Free;
        return new EntitlementSnapshot
        {
            Tier = SubscriptionTier.Free,
            PlanName = free.Name,
            Features = free.Features,
            MaxRoomsPerDay = free.MaxRoomsPerDay,
            MaxGameProfiles = free.MaxGameProfiles,
            HasPremiumBadge = false
        };
    }
}
