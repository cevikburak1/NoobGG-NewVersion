using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;

namespace NoobGg.Infrastructure.Subscriptions;

public sealed class UnconfiguredPaymentProvider : IPaymentProvider
{
    public bool IsConfigured => false;
    public string Name => "none";

    public Task<string> CreateCheckoutUrlAsync(BillingOrder order, SubscriptionPlan plan, CancellationToken ct)
        => throw new InvalidOperationException("Payment provider is not configured");

    public Task<VerifiedPaymentEvent?> VerifyEventAsync(string payload, string signature, CancellationToken ct)
        => Task.FromResult<VerifiedPaymentEvent?>(null);
}
