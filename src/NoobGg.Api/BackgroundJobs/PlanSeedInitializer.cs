using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;

namespace NoobGg.Api.BackgroundJobs;

/// <summary>
/// Seeds/upserts the three default subscription plans on startup.
/// Existing plans are updated with latest defaults (new fields, features, limits).
/// Plan IDs are preserved on update to avoid breaking existing subscriptions.
/// </summary>
public class PlanSeedInitializer : IHostedService
{
    private readonly IMongoContext _mongoContext;
    private readonly ILogger<PlanSeedInitializer> _logger;

    public PlanSeedInitializer(IMongoContext mongoContext, ILogger<PlanSeedInitializer> logger)
    {
        _mongoContext = mongoContext;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            await UpsertPlansAsync(ct);
            _logger.LogInformation("Subscription plans seeded/updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed subscription plans");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private async Task UpsertPlansAsync(CancellationToken ct)
    {
        var collection = _mongoContext.GetCollection<SubscriptionPlan>(CollectionNames.SubscriptionPlans);

        foreach (var config in PlanDefaults.All)
        {
            var filter = Builders<SubscriptionPlan>.Filter.Eq(p => p.Tier, config.Tier);

            var update = Builders<SubscriptionPlan>.Update
                .Set(p => p.Name, config.Name)
                .Set(p => p.Description, config.Description)
                .Set(p => p.Tier, config.Tier)
                .Set(p => p.Price, config.Price)
                .Set(p => p.Currency, "USD")
                .Set(p => p.IntervalMonths, config.IntervalMonths)
                .Set(p => p.Features, config.Features)
                .Set(p => p.MaxRoomsPerDay, config.MaxRoomsPerDay)
                .Set(p => p.MaxGameProfiles, config.MaxGameProfiles)
                .Set(p => p.IsHighlighted, config.IsHighlighted)
                .Set(p => p.SortOrder, config.SortOrder)
                .Set(p => p.IsActive, true)
                .Set(p => p.UpdatedAt, DateTime.UtcNow)
                .SetOnInsert(p => p.CreatedAt, DateTime.UtcNow)
                .SetOnInsert(p => p.Id, Guid.NewGuid().ToString());

            var options = new UpdateOptions { IsUpsert = true };
            var result = await collection.UpdateOneAsync(filter, update, options, ct);

            if (result.UpsertedId is not null)
                _logger.LogInformation("Seeded new plan: {PlanName} ({Tier})", config.Name, config.Tier);
            else if (result.ModifiedCount > 0)
                _logger.LogInformation("Updated plan: {PlanName} ({Tier})", config.Name, config.Tier);
        }
    }
}
