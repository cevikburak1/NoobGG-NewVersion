using MongoDB.Bson;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;

namespace NoobGg.Api.BackgroundJobs;

/// <summary>
/// Runs idempotent schema migrations on startup.
/// Must run BEFORE PlanSeedInitializer.
/// </summary>
public class DatabaseMigrationRunner : IHostedService
{
    private readonly IMongoContext _mongoContext;
    private readonly ILogger<DatabaseMigrationRunner> _logger;

    public DatabaseMigrationRunner(IMongoContext mongoContext, ILogger<DatabaseMigrationRunner> logger)
    {
        _mongoContext = mongoContext;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            await MigrateSubscriptionTierRenameAsync(ct);
            await BackfillSubscriptionPlanFieldsAsync(ct);
            await BackfillUserSubscriptionFieldsAsync(ct);
            await BackfillMessageFieldsAsync(ct);
            await BackfillReportFieldsAsync(ct);

            _logger.LogInformation("Database migrations completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database migration failed");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// Handles SubscriptionTier enum rename: Premium → Plus.
    /// Works for both string-serialized ("Premium") and integer-serialized (1) enums.
    /// </summary>
    private async Task MigrateSubscriptionTierRenameAsync(CancellationToken ct)
    {
        var db = _mongoContext.Database;

        // SubscriptionPlan.Tier: "Premium" → "Plus" (string representation)
        var planCol = db.GetCollection<BsonDocument>(CollectionNames.SubscriptionPlans);
        var planResult = await planCol.UpdateManyAsync(
            Builders<BsonDocument>.Filter.Eq("Tier", "Premium"),
            Builders<BsonDocument>.Update.Set("Tier", "Plus"),
            cancellationToken: ct);

        if (planResult.ModifiedCount > 0)
            _logger.LogInformation("Migrated {Count} subscription plans: Tier Premium → Plus", planResult.ModifiedCount);

        // UserSubscription.Tier: "Premium" → "Plus" (string representation)
        var subCol = db.GetCollection<BsonDocument>(CollectionNames.UserSubscriptions);
        var subResult = await subCol.UpdateManyAsync(
            Builders<BsonDocument>.Filter.Eq("Tier", "Premium"),
            Builders<BsonDocument>.Update.Set("Tier", "Plus"),
            cancellationToken: ct);

        if (subResult.ModifiedCount > 0)
            _logger.LogInformation("Migrated {Count} user subscriptions: Tier Premium → Plus", subResult.ModifiedCount);
    }

    /// <summary>
    /// Backfill new fields on SubscriptionPlan documents that predate the schema change.
    /// </summary>
    private async Task BackfillSubscriptionPlanFieldsAsync(CancellationToken ct)
    {
        var col = _mongoContext.Database.GetCollection<BsonDocument>(CollectionNames.SubscriptionPlans);

        // Backfill Description where missing
        var missingDesc = Builders<BsonDocument>.Filter.Not(
            Builders<BsonDocument>.Filter.Exists("Description"));

        var descResult = await col.UpdateManyAsync(
            missingDesc,
            Builders<BsonDocument>.Update.Set("Description", ""),
            cancellationToken: ct);

        // Backfill IsHighlighted where missing
        var missingHighlight = Builders<BsonDocument>.Filter.Not(
            Builders<BsonDocument>.Filter.Exists("IsHighlighted"));

        var highlightResult = await col.UpdateManyAsync(
            missingHighlight,
            Builders<BsonDocument>.Update.Set("IsHighlighted", false),
            cancellationToken: ct);

        // Backfill SortOrder where missing
        var missingSortOrder = Builders<BsonDocument>.Filter.Not(
            Builders<BsonDocument>.Filter.Exists("SortOrder"));

        var sortResult = await col.UpdateManyAsync(
            missingSortOrder,
            Builders<BsonDocument>.Update.Set("SortOrder", 0),
            cancellationToken: ct);

        var total = descResult.ModifiedCount + highlightResult.ModifiedCount + sortResult.ModifiedCount;
        if (total > 0)
            _logger.LogInformation("Backfilled {Count} field updates on subscription plans", total);
    }

    /// <summary>
    /// Backfill new fields on UserSubscription documents that predate the schema change.
    /// </summary>
    private async Task BackfillUserSubscriptionFieldsAsync(CancellationToken ct)
    {
        var col = _mongoContext.Database.GetCollection<BsonDocument>(CollectionNames.UserSubscriptions);

        // Backfill Tier where missing — default to Free (0 as int, or "Free" as string)
        var missingTier = Builders<BsonDocument>.Filter.Not(
            Builders<BsonDocument>.Filter.Exists("Tier"));

        var tierResult = await col.UpdateManyAsync(
            missingTier,
            Builders<BsonDocument>.Update.Set("Tier", "Free"),
            cancellationToken: ct);

        // Backfill AutoRenew where missing
        var missingAutoRenew = Builders<BsonDocument>.Filter.Not(
            Builders<BsonDocument>.Filter.Exists("AutoRenew"));

        var autoRenewResult = await col.UpdateManyAsync(
            missingAutoRenew,
            Builders<BsonDocument>.Update.Set("AutoRenew", true),
            cancellationToken: ct);

        var total = tierResult.ModifiedCount + autoRenewResult.ModifiedCount;
        if (total > 0)
            _logger.LogInformation("Backfilled {Count} field updates on user subscriptions", total);
    }

    /// <summary>
    /// Backfill new fields on Message documents from the chat feature.
    /// </summary>
    private async Task BackfillMessageFieldsAsync(CancellationToken ct)
    {
        var col = _mongoContext.Database.GetCollection<BsonDocument>(CollectionNames.Messages);

        // Backfill IsEdited where missing
        var missingIsEdited = Builders<BsonDocument>.Filter.Not(
            Builders<BsonDocument>.Filter.Exists("IsEdited"));

        var editResult = await col.UpdateManyAsync(
            missingIsEdited,
            Builders<BsonDocument>.Update.Set("IsEdited", false),
            cancellationToken: ct);

        // Backfill IsDeleted where missing
        var missingIsDeleted = Builders<BsonDocument>.Filter.Not(
            Builders<BsonDocument>.Filter.Exists("IsDeleted"));

        var deleteResult = await col.UpdateManyAsync(
            missingIsDeleted,
            Builders<BsonDocument>.Update.Set("IsDeleted", false),
            cancellationToken: ct);

        var total = editResult.ModifiedCount + deleteResult.ModifiedCount;
        if (total > 0)
            _logger.LogInformation("Backfilled {Count} field updates on messages", total);
    }

    /// <summary>
    /// Backfill TargetType on Report documents that predate the field.
    /// Infers type from RoomId presence: has RoomId → Room, otherwise → User.
    /// </summary>
    private async Task BackfillReportFieldsAsync(CancellationToken ct)
    {
        var col = _mongoContext.Database.GetCollection<BsonDocument>(CollectionNames.Reports);

        var missingTargetType = Builders<BsonDocument>.Filter.Not(
            Builders<BsonDocument>.Filter.Exists("TargetType"));

        // Reports with RoomId → Room target type
        var withRoom = Builders<BsonDocument>.Filter.And(
            missingTargetType,
            Builders<BsonDocument>.Filter.Ne("RoomId", BsonNull.Value),
            Builders<BsonDocument>.Filter.Ne("RoomId", ""));

        var roomResult = await col.UpdateManyAsync(
            withRoom,
            Builders<BsonDocument>.Update.Set("TargetType", "Room"),
            cancellationToken: ct);

        // Remaining without TargetType → User target type
        var withoutRoom = Builders<BsonDocument>.Filter.Not(
            Builders<BsonDocument>.Filter.Exists("TargetType"));

        var userResult = await col.UpdateManyAsync(
            withoutRoom,
            Builders<BsonDocument>.Update.Set("TargetType", "User"),
            cancellationToken: ct);

        var total = roomResult.ModifiedCount + userResult.ModifiedCount;
        if (total > 0)
            _logger.LogInformation("Backfilled TargetType on {Count} reports", total);
    }
}
