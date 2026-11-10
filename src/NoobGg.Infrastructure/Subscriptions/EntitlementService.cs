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
    private readonly TimeProvider _clock;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string CachePrefix = "entitlement:";

    public EntitlementService(
        IMongoContext mongoContext,
        ICacheService cacheService,
        ILogger<EntitlementService> logger,
        TimeProvider? clock = null)
    {
        _mongoContext = mongoContext;
        _cacheService = cacheService;
        _logger = logger;
        _clock = clock ?? TimeProvider.System;
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
        if (cached is not null && cached.ValidUntil > _clock.GetUtcNow().UtcDateTime)
            return cached;

        var snapshot = await BuildSnapshotAsync(userId, ct);
        if (snapshot.Tier != SubscriptionTier.Free && snapshot.ValidUntil <= _clock.GetUtcNow().UtcDateTime)
            snapshot = await BuildFreeTierSnapshotAsync(ct);

        var ttl = CacheDuration;
        if (snapshot.ValidUntil is { } expires)
            ttl = expires - _clock.GetUtcNow().UtcDateTime;
        if (ttl > CacheDuration) ttl = CacheDuration;
        if (ttl > TimeSpan.Zero)
            await _cacheService.SetAsync(cacheKey, snapshot, ttl, ct);

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
        var now = _clock.GetUtcNow().UtcDateTime;
        var activeSub = await subs
            .Find(s => s.UserId == userId && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Cancelled)
                && s.StartDate <= now && s.EndDate > now)
            .SortByDescending(s => s.EndDate)
            .FirstOrDefaultAsync(ct);

        if (activeSub is null)
            return await BuildFreeTierSnapshotAsync(ct);

        var plans = _mongoContext.GetCollection<SubscriptionPlan>(CollectionNames.SubscriptionPlans);
        var plan = await plans.Find(p => p.Id == activeSub.PlanId).FirstOrDefaultAsync(ct);

        if (plan is null)
        {
            _logger.LogWarning(
                "Active subscription {SubId} references missing plan {PlanId}",
                activeSub.Id, activeSub.PlanId);
            return await BuildFreeTierSnapshotAsync(ct);
        }

        return new EntitlementSnapshot
        {
            ValidUntil = activeSub.EndDate,
            Tier = plan.Tier,
            PlanName = plan.Name,
            Features = plan.Features,
            MaxRoomsPerDay = plan.MaxRoomsPerDay,
            MaxGameProfiles = plan.MaxGameProfiles,
            HasPremiumBadge = plan.Features.Contains(PremiumFeature.PremiumBadge)
        };
    }

    private async Task<EntitlementSnapshot> BuildFreeTierSnapshotAsync(CancellationToken ct)
    {
        var plan = await _mongoContext.GetCollection<SubscriptionPlan>(CollectionNames.SubscriptionPlans)
            .Find(p => p.Tier == SubscriptionTier.Free && p.IsActive).FirstOrDefaultAsync(ct);
        var free = PlanDefaults.Free;
        return new EntitlementSnapshot
        {
            Tier = SubscriptionTier.Free,
            ValidUntil = _clock.GetUtcNow().UtcDateTime.Add(CacheDuration),
            PlanName = plan?.Name ?? free.Name,
            Features = plan?.Features ?? free.Features,
            MaxRoomsPerDay = plan?.MaxRoomsPerDay ?? free.MaxRoomsPerDay,
            MaxGameProfiles = plan?.MaxGameProfiles ?? free.MaxGameProfiles,
            HasPremiumBadge = false
        };
    }
}
