using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Microsoft.OpenApi.Models;
using NoobGg.Api.Services;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Infrastructure.Auth;
using NoobGg.Application.Common.Constants;
using NoobGg.Domain.Entities;
using NoobGg.Api.Hubs;

namespace NoobGg.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddJwtAuthentication(configuration);
        services.AddSwaggerDocumentation();
        services.AddSignalRServices(configuration);
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddSingleton<IRoomNotificationService, RoomNotificationService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ClockSkew = TimeSpan.Zero
            };

            // SignalR receives JWT from query string during WebSocket handshake
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        context.Fail("Missing user identity");
                        return;
                    }

                    var mongo = context.HttpContext.RequestServices.GetRequiredService<IMongoContext>();
                    var users = mongo.GetCollection<User>(CollectionNames.Users);
                    var user = await users.Find(u => u.Id == userId)
                        .FirstOrDefaultAsync(context.HttpContext.RequestAborted);
                    if (user is null || user.IsBanned || user.SuspendedUntil > DateTime.UtcNow)
                        context.Fail("Account is restricted");
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy("RequireModerator", policy =>
                policy.RequireRole("Moderator", "Admin"));

            options.AddPolicy("RequireUser", policy =>
                policy.RequireAuthenticatedUser());
        });

        return services;
    }

    private static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "NoobGg API",
                Version = "v1",
                Description = "Gaming teammate finder platform API"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    private static IServiceCollection AddSignalRServices(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";

        services.AddSignalR(options => options.AddFilter(typeof(RestrictedUserHubFilter)))
            .AddStackExchangeRedis(redisConnectionString, options =>
            {
                options.Configuration.ChannelPrefix = new StackExchange.Redis.RedisChannel("NoobGg", StackExchange.Redis.RedisChannel.PatternMode.Literal);
            });

        return services;
    }
}
