using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Subscriptions.Commands.CancelSubscription;

public record CancelSubscriptionCommand : IRequest<Result>
{
    /// <summary>
    /// When null, cancels the current user's active subscription.
    /// When set, admin can cancel a specific user's subscription.
    /// </summary>
    public string? TargetUserId { get; init; }

    /// <summary>
    /// If true, subscription ends immediately. If false, remains active until EndDate.
    /// </summary>
    public bool Immediate { get; init; }
}
