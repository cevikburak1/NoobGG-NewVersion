using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class UserSubscription : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? PaymentProvider { get; set; }
    public string? ExternalSubscriptionId { get; set; }

    public bool IsActive => Status == SubscriptionStatus.Active && DateTime.UtcNow < EndDate;
}
