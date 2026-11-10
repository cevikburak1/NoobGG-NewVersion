using System.Text.Json;
using NoobGg.Application.Common.Interfaces;
using StackExchange.Redis;

namespace NoobGg.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(key);
        if (value.IsNullOrEmpty)
            return default;

        return JsonSerializer.Deserialize<T>(value!, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var serialized = JsonSerializer.Serialize(value, JsonOptions);
        // Value and TTL must be committed together: a crash between SET and EXPIRE
        // could otherwise leave authorization snapshots cached indefinitely.
        await _database.StringSetAsync(key, serialized, expiry.HasValue ? new Expiration(expiry.Value) : Expiration.Default);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _database.KeyExistsAsync(key);
    }
}
