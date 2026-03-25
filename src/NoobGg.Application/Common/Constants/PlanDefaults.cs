using NoobGg.Domain.Enums;

namespace NoobGg.Application.Common.Constants;

/// <summary>
/// Default limits per tier. Used by PlanSeedInitializer and as fallback.
/// </summary>
public static class PlanDefaults
{
    public static readonly PlanConfig Free = new()
    {
        Name = "Free",
        Description = "Get started with the essentials",
        Tier = SubscriptionTier.Free,
        Price = 0m,
        IntervalMonths = 0,
        MaxRoomsPerDay = 3,
        MaxGameProfiles = 2,
        SortOrder = 0,
        IsHighlighted = false,
        Features = []
    };

    public static readonly PlanConfig Plus = new()
    {
        Name = "Plus",
        Description = "Enhanced experience for active gamers",
        Tier = SubscriptionTier.Plus,
        Price = 4.99m,
        IntervalMonths = 1,
        MaxRoomsPerDay = 10,
        MaxGameProfiles = 5,
        SortOrder = 1,
        IsHighlighted = true,
        Features =
        [
            PremiumFeature.AdvancedFilters,
            PremiumFeature.ProfileBoost,
            PremiumFeature.PremiumBadge,
            PremiumFeature.CustomRoomTags,
            PremiumFeature.ExtendedHistory
        ]
    };

    public static readonly PlanConfig Pro = new()
    {
        Name = "Pro",
        Description = "The ultimate competitive edge",
        Tier = SubscriptionTier.Pro,
        Price = 9.99m,
        IntervalMonths = 1,
        MaxRoomsPerDay = 50,
        MaxGameProfiles = 15,
        SortOrder = 2,
        IsHighlighted = false,
        Features =
        [
            PremiumFeature.AdvancedFilters,
            PremiumFeature.ProfileBoost,
            PremiumFeature.EnhancedVisibility,
            PremiumFeature.ProfileCustomization,
            PremiumFeature.PremiumBadge,
            PremiumFeature.ExtendedRoomCreation,
            PremiumFeature.AnalyticsAccess,
            PremiumFeature.PriorityMatchmaking,
            PremiumFeature.CustomRoomTags,
            PremiumFeature.ExtendedHistory
        ]
    };

    public static readonly PlanConfig[] All = [Free, Plus, Pro];
}

public class PlanConfig
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public SubscriptionTier Tier { get; init; }
    public decimal Price { get; init; }
    public int IntervalMonths { get; init; }
    public int MaxRoomsPerDay { get; init; }
    public int MaxGameProfiles { get; init; }
    public int SortOrder { get; init; }
    public bool IsHighlighted { get; init; }
    public List<string> Features { get; init; } = [];
}
