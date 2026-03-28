using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Features.Games.Services;
using NoobGg.Infrastructure.Auth;
using NoobGg.Infrastructure.Caching;
using NoobGg.Infrastructure.Chat;
using NoobGg.Infrastructure.Email;
using NoobGg.Infrastructure.Persistence;
using NoobGg.Infrastructure.Rawg;
using NoobGg.Infrastructure.Moderation;
using NoobGg.Infrastructure.Services;
using NoobGg.Infrastructure.Storage;
using NoobGg.Infrastructure.Subscriptions;
using StackExchange.Redis;

namespace NoobGg.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMongoDb(configuration);
        services.AddRedisCache(configuration);
        services.AddAuthServices(configuration);
        services.AddSteamServices(configuration);
        services.AddChatServices();
        services.AddSubscriptionServices();
        services.AddModerationServices();
        services.AddEmailServices(configuration);
        services.AddFileStorage(configuration);

        return services;
    }

    private static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));
        services.AddSingleton<IMongoContext, MongoContext>();

        return services;
    }

    private static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisSettings = configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>()!;
        services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.SectionName));

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisSettings.ConnectionString));

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

    private static IServiceCollection AddAuthServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        return services;
    }

    private static IServiceCollection AddChatServices(this IServiceCollection services)
    {
        services.AddSingleton<IChatPresenceService, ChatPresenceService>();
        services.AddSingleton<IPresenceTracker, InMemoryPresenceTracker>();
        return services;
    }

    private static IServiceCollection AddSubscriptionServices(this IServiceCollection services)
    {
        services.AddScoped<IEntitlementService, EntitlementService>();
        return services;
    }

    private static IServiceCollection AddModerationServices(this IServiceCollection services)
    {
        services.AddScoped<IBlockService, BlockService>();
        return services;
    }

    private static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddTransient<IEmailService, SmtpEmailService>();
        return services;
    }

    private static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageSettings>(configuration.GetSection(FileStorageSettings.SectionName));
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        return services;
    }

    private static IServiceCollection AddSteamServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RawgSettings>(configuration.GetSection(RawgSettings.SectionName));

        services.AddHttpClient<IRawgApiClient, RawgApiClient>(client =>
        {
            var settings = configuration.GetSection(RawgSettings.SectionName).Get<RawgSettings>() ?? new RawgSettings();
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<IGameSyncService, GameSyncService>();

        return services;
    }
}
