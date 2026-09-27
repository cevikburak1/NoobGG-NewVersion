namespace NoobGg.Domain.Entities;

public class BillingOrder : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? ProviderEventId { get; set; }
    public string? ExternalSubscriptionId { get; set; }
}
