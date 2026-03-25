using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Subscriptions.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Subscriptions.Queries.GetMySubscription;

public class GetMySubscriptionQueryHandler
    : IRequestHandler<GetMySubscriptionQuery, Result<UserSubscriptionResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IEntitlementService _entitlementService;

    public GetMySubscriptionQueryHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        IEntitlementService entitlementService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _entitlementService = entitlementService;
    }

    public async Task<Result<UserSubscriptionResponse>> Handle(
        GetMySubscriptionQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<UserSubscriptionResponse>.Unauthorized();

        var userId = _currentUser.UserId;
        var entitlements = await _entitlementService.GetEntitlementsAsync(userId, ct);

        var subs = _mongoContext.GetCollection<UserSubscription>(CollectionNames.UserSubscriptions);
        var activeSub = await subs
            .Find(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .SortByDescending(s => s.EndDate)
            .FirstOrDefaultAsync(ct);

        if (activeSub is null || !activeSub.IsActive)
        {
            return Result<UserSubscriptionResponse>.Success(new UserSubscriptionResponse
            {
                Tier = SubscriptionTier.Free.ToString(),
                PlanName = "Free",
                Status = "Active",
                Entitlements = entitlements
            });
        }

        return Result<UserSubscriptionResponse>.Success(new UserSubscriptionResponse
        {
            SubscriptionId = activeSub.Id,
            Tier = activeSub.Tier.ToString(),
            PlanName = entitlements.PlanName,
            Status = activeSub.Status.ToString(),
            StartDate = activeSub.StartDate,
            EndDate = activeSub.EndDate,
            AutoRenew = activeSub.AutoRenew,
            Entitlements = entitlements
        });
    }
}
