using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SubscriptionTier Tier { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public int IntervalMonths { get; set; } = 1;
    public List<string> Features { get; set; } = [];
    public int MaxRoomsPerDay { get; set; }
    public int MaxGameProfiles { get; set; }
    public bool IsHighlighted { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
