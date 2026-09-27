using NoobGg.Domain.Entities;

namespace NoobGg.Application.Common.Interfaces;

public interface IPaymentProvider
{
    bool IsConfigured { get; }
    string Name { get; }
    Task<string> CreateCheckoutUrlAsync(BillingOrder order, SubscriptionPlan plan, CancellationToken ct);
    Task<VerifiedPaymentEvent?> VerifyEventAsync(string payload, string signature, CancellationToken ct);
}

public record VerifiedPaymentEvent(string OrderId, string EventId, bool Succeeded, string? ExternalSubscriptionId);
