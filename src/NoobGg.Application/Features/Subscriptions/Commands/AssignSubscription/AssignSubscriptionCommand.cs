using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Subscriptions.DTOs;

namespace NoobGg.Application.Features.Subscriptions.Commands.AssignSubscription;

/// <summary>
/// Assigns a subscription plan to a user.
/// Intended for: admin dashboard, payment webhook handlers, internal use.
/// </summary>
public record AssignSubscriptionCommand : IRequest<Result<UserSubscriptionResponse>>
{
    public string UserId { get; init; } = string.Empty;
    public string PlanId { get; init; } = string.Empty;
    public string? PaymentProvider { get; init; }
    public string? ExternalSubscriptionId { get; init; }
    public int? DurationMonths { get; init; }
}
