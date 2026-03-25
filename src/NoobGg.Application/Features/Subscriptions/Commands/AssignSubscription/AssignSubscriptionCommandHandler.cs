using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Subscriptions.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Subscriptions.Commands.AssignSubscription;

public class AssignSubscriptionCommandHandler
    : IRequestHandler<AssignSubscriptionCommand, Result<UserSubscriptionResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly IEntitlementService _entitlementService;
    private readonly ILogger<AssignSubscriptionCommandHandler> _logger;

    public AssignSubscriptionCommandHandler(
        IMongoContext mongoContext,
        IEntitlementService entitlementService,
        ILogger<AssignSubscriptionCommandHandler> logger)
    {
        _mongoContext = mongoContext;
        _entitlementService = entitlementService;
        _logger = logger;
    }

    public async Task<Result<UserSubscriptionResponse>> Handle(
        AssignSubscriptionCommand request, CancellationToken ct)
    {
        var plans = _mongoContext.GetCollection<SubscriptionPlan>(CollectionNames.SubscriptionPlans);
        var plan = await plans.Find(p => p.Id == request.PlanId && p.IsActive).FirstOrDefaultAsync(ct);

        if (plan is null)
            return Result<UserSubscriptionResponse>.NotFound("Plan not found or inactive");

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var userExists = await users.Find(u => u.Id == request.UserId).AnyAsync(ct);

        if (!userExists)
            return Result<UserSubscriptionResponse>.NotFound("User not found");

        var subs = _mongoContext.GetCollection<UserSubscription>(CollectionNames.UserSubscriptions);

        // Expire any currently active subscription for this user
        var activeFilter = Builders<UserSubscription>.Filter.And(
            Builders<UserSubscription>.Filter.Eq(s => s.UserId, request.UserId),
            Builders<UserSubscription>.Filter.Eq(s => s.Status, SubscriptionStatus.Active));

        await subs.UpdateManyAsync(
            activeFilter,
            Builders<UserSubscription>.Update
                .Set(s => s.Status, SubscriptionStatus.Expired)
                .Set(s => s.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        var durationMonths = request.DurationMonths ?? plan.IntervalMonths;
        if (durationMonths <= 0) durationMonths = 1;

        var now = DateTime.UtcNow;
        var subscription = new UserSubscription
        {
            UserId = request.UserId,
            PlanId = plan.Id,
            Tier = plan.Tier,
            Status = SubscriptionStatus.Active,
            StartDate = now,
            EndDate = now.AddMonths(durationMonths),
            AutoRenew = true,
            PaymentProvider = request.PaymentProvider,
            ExternalSubscriptionId = request.ExternalSubscriptionId
        };

        await subs.InsertOneAsync(subscription, cancellationToken: ct);
        await _entitlementService.InvalidateCacheAsync(request.UserId, ct);

        _logger.LogInformation(
            "Subscription assigned: user {UserId} → plan {PlanName} ({Tier}) until {EndDate}",
            request.UserId, plan.Name, plan.Tier, subscription.EndDate);

        var entitlements = await _entitlementService.GetEntitlementsAsync(request.UserId, ct);

        return Result<UserSubscriptionResponse>.Created(new UserSubscriptionResponse
        {
            SubscriptionId = subscription.Id,
            Tier = subscription.Tier.ToString(),
            PlanName = plan.Name,
            Status = subscription.Status.ToString(),
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            AutoRenew = subscription.AutoRenew,
            Entitlements = entitlements
        });
    }
}
