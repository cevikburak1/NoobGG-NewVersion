using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Subscriptions.Commands.CancelSubscription;

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IEntitlementService _entitlementService;
    private readonly ILogger<CancelSubscriptionCommandHandler> _logger;

    public CancelSubscriptionCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        IEntitlementService entitlementService,
        ILogger<CancelSubscriptionCommandHandler> logger)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _entitlementService = entitlementService;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelSubscriptionCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var userId = request.TargetUserId ?? _currentUser.UserId;

        // Only admins can cancel other users' subscriptions
        if (request.TargetUserId is not null && _currentUser.Role != "Admin")
            return Result.Fail("Forbidden", 403);

        var subs = _mongoContext.GetCollection<UserSubscription>(CollectionNames.UserSubscriptions);
        var activeSub = await subs
            .Find(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .SortByDescending(s => s.EndDate)
            .FirstOrDefaultAsync(ct);

        if (activeSub is null)
            return Result.Fail("No active subscription found", 404);

        var now = DateTime.UtcNow;
        var update = Builders<UserSubscription>.Update
            .Set(s => s.CancelledAt, now)
            .Set(s => s.AutoRenew, false)
            .Set(s => s.UpdatedAt, now);

        if (request.Immediate)
        {
            update = update
                .Set(s => s.Status, SubscriptionStatus.Cancelled)
                .Set(s => s.EndDate, now);
        }
        else
        {
            // Stay active until period ends, but don't renew
            update = update.Set(s => s.Status, SubscriptionStatus.Cancelled);
        }

        await subs.UpdateOneAsync(
            s => s.Id == activeSub.Id,
            update,
            cancellationToken: ct);

        await _entitlementService.InvalidateCacheAsync(userId, ct);

        _logger.LogInformation(
            "Subscription cancelled: user {UserId}, immediate={Immediate}",
            userId, request.Immediate);

        return Result.Success();
    }
}
