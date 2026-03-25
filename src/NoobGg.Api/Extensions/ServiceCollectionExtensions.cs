using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NoobGg.Api.Services;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Infrastructure.Auth;

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

        services.AddSignalR()
            .AddStackExchangeRedis(redisConnectionString, options =>
            {
                options.Configuration.ChannelPrefix = new StackExchange.Redis.RedisChannel("NoobGg", StackExchange.Redis.RedisChannel.PatternMode.Literal);
            });

        return services;
    }
}
