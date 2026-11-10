using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using NoobGg.Api.BackgroundJobs;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Features.Profiles.Commands.AddGameProfile;
using NoobGg.Application.Features.Profiles.Commands.UpdateGameProfile;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;
using NoobGg.Infrastructure.Subscriptions;
using Xunit;

namespace NoobGg.Application.Tests;

public class CommercialReadinessRegressionTests : IAsyncLifetime
{
    private readonly MongoClient _client;
    private readonly TestMongo _mongo;
    private readonly string _databaseName = $"noobgg_test_{Guid.NewGuid():N}";

    public CommercialReadinessRegressionTests()
    {
        var uri = Environment.GetEnvironmentVariable("MONGO_TEST_URI");
        Assert.False(string.IsNullOrWhiteSpace(uri), "Set MONGO_TEST_URI to an isolated test MongoDB");
        _client = new MongoClient(uri);
        _mongo = new TestMongo(_client.GetDatabase(_databaseName));
    }
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => _client.DropDatabaseAsync(_databaseName);

    [Fact]
    public async Task Restart_preserves_customized_and_inactive_plans_and_adds_missing_plans()
    {
        var plans = _mongo.GetCollection<SubscriptionPlan>(CollectionNames.SubscriptionPlans);
        var edited = new SubscriptionPlan { Name = "Custom Plus", Tier = SubscriptionTier.Plus, Price = 123.45m,
            Currency = "TRY", MaxGameProfiles = 7, MaxRoomsPerDay = 8, Features = ["custom"], IsActive = false };
        await plans.InsertOneAsync(edited);
        var seed = new PlanSeedInitializer(_mongo, NullLogger<PlanSeedInitializer>.Instance);
        await seed.StartAsync(default);
        await seed.StartAsync(default);
        var actual = await plans.Find(p => p.Id == edited.Id).SingleAsync();
        Assert.Equal(123.45m, actual.Price);
        Assert.Equal("TRY", actual.Currency);
        Assert.Equal("Custom Plus", actual.Name);
        Assert.Equal(7, actual.MaxGameProfiles);
        Assert.False(actual.IsActive);
        Assert.Equal(edited.UpdatedAt.Ticks / TimeSpan.TicksPerMillisecond, actual.UpdatedAt.Ticks / TimeSpan.TicksPerMillisecond);
        Assert.Equal(3, await plans.CountDocumentsAsync(_ => true));
    }

    [Fact]
    public async Task New_game_profile_persists_the_name_returned_to_the_client()
    {
        var game = new Game { Name = "Test game", IsActive = true };
        await _mongo.GetCollection<Game>(CollectionNames.Games).InsertOneAsync(game);
        var result = await new AddGameProfileCommandHandler(_mongo, new TestUser())
            .Handle(new AddGameProfileCommand { GameId = game.Id, InGameName = "  Burak#TR  " }, default);
        Assert.True(result.IsSuccess, result.Error);
        var stored = await _mongo.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles)
            .Find(p => p.Id == result.Data!.Id).SingleAsync();
        Assert.Equal("Burak#TR", stored.InGameName);
        Assert.Equal(stored.InGameName, result.Data!.InGameName);
        var updated = await new UpdateGameProfileCommandHandler(_mongo, new TestUser())
            .Handle(new UpdateGameProfileCommand { Id = stored.Id, InGameName = "  NewName#EU  " }, default);
        Assert.True(updated.IsSuccess, updated.Error);
        Assert.Equal("NewName#EU", updated.Data!.InGameName);
        Assert.Equal("NewName#EU", (await _mongo.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles)
            .Find(p => p.Id == stored.Id).SingleAsync()).InGameName);
        await new UpdateGameProfileCommandHandler(_mongo, new TestUser())
            .Handle(new UpdateGameProfileCommand { Id = stored.Id, InGameName = "" }, default);
        Assert.Equal("", (await _mongo.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles)
            .Find(p => p.Id == stored.Id).SingleAsync()).InGameName);
    }

    [Fact]
    public async Task Paid_cache_expires_at_subscription_end_even_if_cache_keeps_old_value()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));
        var plan = new SubscriptionPlan { Name = "Plus", Tier = SubscriptionTier.Plus, MaxGameProfiles = 5 };
        await _mongo.GetCollection<SubscriptionPlan>(CollectionNames.SubscriptionPlans).InsertOneAsync(plan);
        await _mongo.GetCollection<UserSubscription>(CollectionNames.UserSubscriptions).InsertOneAsync(new UserSubscription
        {
            UserId = "player", PlanId = plan.Id, Status = SubscriptionStatus.Cancelled,
            StartDate = clock.Now.UtcDateTime.AddDays(-1), EndDate = clock.Now.UtcDateTime.AddSeconds(20)
        });
        var cache = new TestCache();
        var service = new EntitlementService(_mongo, cache, NullLogger<EntitlementService>.Instance, clock);
        Assert.Equal(SubscriptionTier.Plus, (await service.GetEntitlementsAsync("player")).Tier);
        Assert.Equal(TimeSpan.FromSeconds(20), cache.LastExpiry);
        clock.Now = clock.Now.AddSeconds(21);
        Assert.Equal(SubscriptionTier.Free, (await service.GetEntitlementsAsync("player")).Tier);
    }

    [Theory]
    [InlineData(SubscriptionTier.Pro)]
    [InlineData(SubscriptionTier.Free)]
    public async Task Legacy_snapshot_without_expiry_is_revalidated(SubscriptionTier tier)
    {
        var cache = new TestCache { Value = new EntitlementSnapshot { Tier = tier, MaxRoomsPerDay = 999 } };
        var service = new EntitlementService(_mongo, cache, NullLogger<EntitlementService>.Instance);
        var actual = await service.GetEntitlementsAsync("player");
        Assert.Equal(SubscriptionTier.Free, actual.Tier);
        Assert.Equal(PlanDefaults.Free.MaxRoomsPerDay, actual.MaxRoomsPerDay);
    }

    [Fact]
    public async Task Free_entitlements_use_configured_plan_limits_and_features()
    {
        await _mongo.GetCollection<SubscriptionPlan>(CollectionNames.SubscriptionPlans).InsertOneAsync(new SubscriptionPlan
        { Name = "Starter", Tier = SubscriptionTier.Free, MaxRoomsPerDay = 4, MaxGameProfiles = 3, Features = ["starter-feature"] });
        var service = new EntitlementService(_mongo, new TestCache(), NullLogger<EntitlementService>.Instance);
        var snapshot = await service.GetEntitlementsAsync("player");
        Assert.Equal("Starter", snapshot.PlanName);
        Assert.Equal(4, snapshot.MaxRoomsPerDay);
        Assert.Equal(3, snapshot.MaxGameProfiles);
        Assert.Contains("starter-feature", snapshot.Features);
    }

    private sealed class TestMongo(IMongoDatabase database) : IMongoContext
    {
        public IMongoDatabase Database => database;
        public IMongoCollection<T> GetCollection<T>(string name) => database.GetCollection<T>(name);
    }
    private sealed class TestUser : ICurrentUser
    {
        public string? UserId => "player";
        public string? Username => "Player";
        public string? Role => "User";
        public bool IsAuthenticated => true;
    }
    private sealed class TestClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    // Deliberately retains values after TTL so the service must enforce its own boundary.
    private sealed class TestCache : ICacheService
    {
        public object? Value { get; set; }
        public TimeSpan? LastExpiry { get; private set; }
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => Task.FromResult(Value is T value ? value : default);
        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
        { Value = value; LastExpiry = expiry; return Task.CompletedTask; }
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) { Value = null; return Task.CompletedTask; }
        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(Value != null);
    }
}
