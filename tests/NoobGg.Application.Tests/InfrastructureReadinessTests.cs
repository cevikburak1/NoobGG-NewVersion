using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using NoobGg.Api.Controllers;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Infrastructure.Caching;
using StackExchange.Redis;
using Xunit;

namespace NoobGg.Application.Tests;

public class InfrastructureReadinessTests
{
    private static async Task<ConnectionMultiplexer> ConnectRedis()
    {
        var uri = Environment.GetEnvironmentVariable("REDIS_TEST_URI");
        Assert.False(string.IsNullOrWhiteSpace(uri), "Set REDIS_TEST_URI to an isolated test Redis");
        return await ConnectionMultiplexer.ConnectAsync(uri!);
    }

    [Fact]
    public async Task Cache_writes_have_expiry_and_persistent_replacement_removes_it()
    {
        using var redis = await ConnectRedis();
        var key = $"noobgg-test:{Guid.NewGuid():N}";
        try
        {
            var cache = new RedisCacheService(redis);
            await cache.SetAsync(key, "first", TimeSpan.FromSeconds(30));
            var ttl = await redis.GetDatabase().KeyTimeToLiveAsync(key);
            Assert.NotNull(ttl);
            Assert.True(ttl > TimeSpan.Zero && ttl <= TimeSpan.FromSeconds(30));
            Assert.Equal("first", await cache.GetAsync<string>(key));
            await cache.SetAsync(key, "replacement");
            Assert.Null(await redis.GetDatabase().KeyTimeToLiveAsync(key));
            Assert.Equal("replacement", await cache.GetAsync<string>(key));
        }
        finally { await redis.GetDatabase().KeyDeleteAsync(key); }
    }

    [Fact]
    public async Task Readiness_reports_actual_dependency_failure_without_provider_details()
    {
        using var redis = await ConnectRedis();
        var uri = Environment.GetEnvironmentVariable("MONGO_TEST_URI");
        Assert.False(string.IsNullOrWhiteSpace(uri), "Set MONGO_TEST_URI");
        var healthy = new HealthController(new TestMongo(new MongoClient(uri).GetDatabase("noobgg_test_health")), redis);
        Assert.IsType<OkObjectResult>(await healthy.Ready(default));

        var unavailable = new MongoClient(new MongoClientSettings
        {
            Server = new MongoServerAddress("127.0.0.1", 1),
            ServerSelectionTimeout = TimeSpan.FromMilliseconds(250),
            ConnectTimeout = TimeSpan.FromMilliseconds(100)
        });
        var unhealthy = new HealthController(new TestMongo(unavailable.GetDatabase("noobgg_test_health")), redis);
        var response = Assert.IsType<ObjectResult>(await unhealthy.Ready(default));
        Assert.Equal(503, response.StatusCode);
        Assert.Equal("{ status = not_ready }", response.Value!.ToString());

        await redis.CloseAsync();
        Assert.Equal(503, Assert.IsType<ObjectResult>(await healthy.Ready(default)).StatusCode);
    }

    private sealed class TestMongo(IMongoDatabase database) : IMongoContext
    {
        public IMongoDatabase Database => database;
        public IMongoCollection<T> GetCollection<T>(string name) => database.GetCollection<T>(name);
    }
}
