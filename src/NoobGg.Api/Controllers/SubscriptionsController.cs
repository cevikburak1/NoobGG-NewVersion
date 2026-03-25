using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Subscriptions.Commands.AssignSubscription;
using NoobGg.Application.Features.Subscriptions.Commands.CancelSubscription;
using NoobGg.Application.Features.Subscriptions.Queries.GetMySubscription;
using NoobGg.Application.Features.Subscriptions.Queries.GetPlans;

namespace NoobGg.Api.Controllers;

[Route("api/subscriptions")]
public class SubscriptionsController : ApiControllerBase
{
    /// <summary>
    /// List all available plans with comparison data.
    /// Anonymous users see plans without current-tier info.
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans()
    {
        var result = await Mediator.Send(new GetPlansQuery());
        return HandleResult(result);
    }

    /// <summary>
    /// Get current user's subscription and entitlements.
    /// Free tier returned when no active subscription exists.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMySubscription()
    {
        var result = await Mediator.Send(new GetMySubscriptionQuery());
        return HandleResult(result);
    }

    /// <summary>
    /// Assign a plan to a user. Admin-only or used by payment webhooks.
    /// </summary>
    [HttpPost("assign")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Assign([FromBody] AssignSubscriptionCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Cancel current user's subscription.
    /// </summary>
    [HttpPost("cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel([FromBody] CancelSubscriptionCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
