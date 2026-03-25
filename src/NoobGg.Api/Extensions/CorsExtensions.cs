namespace NoobGg.Api.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "NoobGgCorsPolicy";

    public static IServiceCollection AddNoobGgCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                             ?? ["http://localhost:5173"];

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, builder =>
            {
                builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
