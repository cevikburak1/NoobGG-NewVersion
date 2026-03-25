using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Subscriptions.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Subscriptions.Queries.GetPlans;

public class GetPlansQueryHandler : IRequestHandler<GetPlansQuery, Result<PlanComparisonResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IEntitlementService _entitlementService;

    public GetPlansQueryHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        IEntitlementService entitlementService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _entitlementService = entitlementService;
    }

    public async Task<Result<PlanComparisonResponse>> Handle(GetPlansQuery request, CancellationToken ct)
    {
        var plans = _mongoContext.GetCollection<SubscriptionPlan>(CollectionNames.SubscriptionPlans);

        var activePlans = await plans
            .Find(p => p.IsActive)
            .SortBy(p => p.SortOrder)
            .ToListAsync(ct);

        var planResponses = activePlans.Select(p => new PlanResponse(
            p.Id,
            p.Name,
            p.Description,
            p.Tier.ToString(),
            p.Price,
            p.Currency,
            p.IntervalMonths,
            p.Features,
            p.MaxRoomsPerDay,
            p.MaxGameProfiles,
            p.IsHighlighted,
            p.SortOrder
        )).ToList();

        var currentTier = SubscriptionTier.Free;
        string? currentPlanId = null;

        if (_currentUser.IsAuthenticated && _currentUser.UserId is not null)
        {
            currentTier = await _entitlementService.GetActiveTierAsync(_currentUser.UserId, ct);

            var subs = _mongoContext.GetCollection<UserSubscription>(CollectionNames.UserSubscriptions);
            var activeSub = await subs
                .Find(s => s.UserId == _currentUser.UserId && s.Status == SubscriptionStatus.Active)
                .SortByDescending(s => s.EndDate)
                .FirstOrDefaultAsync(ct);

            currentPlanId = activeSub?.PlanId;
        }

        return Result<PlanComparisonResponse>.Success(new PlanComparisonResponse
        {
            Plans = planResponses,
            CurrentTier = currentTier.ToString(),
            CurrentPlanId = currentPlanId
        });
    }
}
