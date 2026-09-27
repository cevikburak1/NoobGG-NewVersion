using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;
using NoobGg.Application.Features.Subscriptions.Commands.AssignSubscription;
using NoobGg.Application.Features.Subscriptions.Commands.CancelSubscription;
using NoobGg.Application.Features.Subscriptions.Queries.GetMySubscription;
using NoobGg.Application.Features.Subscriptions.Queries.GetPlans;

namespace NoobGg.Api.Controllers;

[Route("api/subscriptions")]
public class SubscriptionsController(IPaymentProvider payment, IMongoContext mongo, ICurrentUser currentUser) : ApiControllerBase
{
    [HttpGet("checkout/status")]
    [AllowAnonymous]
    public IActionResult CheckoutStatus() => Ok(new { available = payment.IsConfigured });

    [HttpGet("orders/mine")]
    [Authorize]
    public async Task<IActionResult> MyOrders(CancellationToken ct)
    {
        var orders = mongo.GetCollection<BillingOrder>(CollectionNames.BillingOrders);
        return Ok(await orders.Find(o => o.UserId == currentUser.UserId)
            .SortByDescending(o => o.CreatedAt).Limit(30).ToListAsync(ct));
    }

    [HttpPost("checkout")]
    [Authorize]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        if (!payment.IsConfigured)
            return StatusCode(503, new { title = "Checkout is not configured" });
        var plans = mongo.GetCollection<SubscriptionPlan>(CollectionNames.SubscriptionPlans);
        var plan = await plans.Find(p => p.Id == request.PlanId && p.IsActive && p.Price > 0).FirstOrDefaultAsync(ct);
        if (plan is null) return BadRequest("Plan is unavailable");
        var order = new BillingOrder { UserId = currentUser.UserId!, PlanId = plan.Id };
        var orders = mongo.GetCollection<BillingOrder>(CollectionNames.BillingOrders);
        await orders.InsertOneAsync(order, cancellationToken: ct);
        try
        {
            var url = await payment.CreateCheckoutUrlAsync(order, plan, ct);
            if (!Uri.TryCreate(url, UriKind.Absolute, out var checkoutUrl) || checkoutUrl.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("Payment provider returned an invalid checkout URL");
            return Ok(new { orderId = order.Id, checkoutUrl = url });
        }
        catch
        {
            await orders.UpdateOneAsync(o => o.Id == order.Id,
                Builders<BillingOrder>.Update.Set(o => o.Status, "Failed"), cancellationToken: ct);
            throw;
        }
    }

    [HttpPost("checkout/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        if (!payment.IsConfigured) return StatusCode(503);
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(ct);
        var signature = Request.Headers["X-Payment-Signature"].ToString();
        var verified = await payment.VerifyEventAsync(payload, signature, ct);
        if (verified is null) return Unauthorized();

        var orders = mongo.GetCollection<BillingOrder>(CollectionNames.BillingOrders);
        var order = await orders.Find(o => o.Id == verified.OrderId).FirstOrDefaultAsync(ct);
        if (order is null) return NotFound();
        if (order.Status == "Paid" && order.ProviderEventId == verified.EventId) return Ok();
        if (order.Status != "Pending") return Conflict();
        var claim = await orders.UpdateOneAsync(o => o.Id == order.Id && o.Status == "Pending",
            Builders<BillingOrder>.Update.Set(o => o.Status, "Processing")
                .Set(o => o.ProviderEventId, verified.EventId), cancellationToken: ct);
        if (claim.ModifiedCount == 0) return Conflict();
        if (!verified.Succeeded)
        {
            await orders.UpdateOneAsync(o => o.Id == order.Id,
                Builders<BillingOrder>.Update.Set(o => o.Status, "Failed"), cancellationToken: ct);
            return Ok();
        }
        var assigned = await Mediator.Send(new AssignSubscriptionCommand
        {
            UserId = order.UserId,
            PlanId = order.PlanId,
            PaymentProvider = payment.Name,
            ExternalSubscriptionId = verified.ExternalSubscriptionId ?? verified.EventId
        }, ct);
        if (!assigned.IsSuccess)
        {
            await orders.UpdateOneAsync(o => o.Id == order.Id,
                Builders<BillingOrder>.Update.Set(o => o.Status, "Pending")
                    .Set(o => o.ProviderEventId, (string?)null), cancellationToken: ct);
            return StatusCode(500);
        }
        await orders.UpdateOneAsync(o => o.Id == order.Id,
            Builders<BillingOrder>.Update.Set(o => o.Status, "Paid")
                .Set(o => o.ExternalSubscriptionId, verified.ExternalSubscriptionId), cancellationToken: ct);
        return Ok();
    }
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

public record CheckoutRequest(string PlanId);
