using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;

namespace NoobGg.Api.BackgroundJobs;

/// <summary>
/// Adds missing default plans. Existing commercial terms must only change through
/// explicit migrations or administration, never as a side effect of restarting.
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
            _logger.LogInformation("Missing subscription plans seeded successfully");
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
                .SetOnInsert(p => p.Name, config.Name)
                .SetOnInsert(p => p.Description, config.Description)
                .SetOnInsert(p => p.Tier, config.Tier)
                .SetOnInsert(p => p.Price, config.Price)
                .SetOnInsert(p => p.Currency, "USD")
                .SetOnInsert(p => p.IntervalMonths, config.IntervalMonths)
                .SetOnInsert(p => p.Features, config.Features)
                .SetOnInsert(p => p.MaxRoomsPerDay, config.MaxRoomsPerDay)
                .SetOnInsert(p => p.MaxGameProfiles, config.MaxGameProfiles)
                .SetOnInsert(p => p.IsHighlighted, config.IsHighlighted)
                .SetOnInsert(p => p.SortOrder, config.SortOrder)
                .SetOnInsert(p => p.IsActive, true)
                .SetOnInsert(p => p.UpdatedAt, DateTime.UtcNow)
                .SetOnInsert(p => p.CreatedAt, DateTime.UtcNow)
                .SetOnInsert(p => p.Id, Guid.NewGuid().ToString());

            var options = new UpdateOptions { IsUpsert = true };
            var result = await collection.UpdateOneAsync(filter, update, options, ct);

            if (result.UpsertedId is not null)
                _logger.LogInformation("Seeded new plan: {PlanName} ({Tier})", config.Name, config.Tier);
        }
    }
}
