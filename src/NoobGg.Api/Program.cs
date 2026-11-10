using System.Text.Json.Serialization;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NoobGg.Api.BackgroundJobs;
using NoobGg.Api.Extensions;
using NoobGg.Api.Hubs;
using NoobGg.Api.Middleware;
using NoobGg.Application;
using NoobGg.Infrastructure;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Api.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    var requiredEnvironmentVariables = new[]
    {
        "MongoDb__ConnectionString", "Redis__ConnectionString", "Jwt__Secret",
        "Email__SmtpHost", "Email__SmtpUsername", "Email__SmtpPassword",
        "Email__FromEmail", "Cors__AllowedOrigins__0"
    };
    var missing = requiredEnvironmentVariables.Where(name =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name))).ToArray();
    if (missing.Length > 0)
        throw new InvalidOperationException($"Missing production environment variables: {string.Join(", ", missing)}");
}

if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Secret"]))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Jwt:Secret must be configured outside Development.");

    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Jwt:Secret"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
    });
}

// Serilog
var mongoConnectionString = builder.Configuration["MongoDb:ConnectionString"] ?? "mongodb://localhost:27017";
var mongoDatabaseName = builder.Configuration["MongoDb:DatabaseName"] ?? "noobgg";
var mongoLogUrl = $"{mongoConnectionString.TrimEnd('/')}/{mongoDatabaseName}";

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.File("logs/noobgg-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .WriteTo.MongoDBBson(cfg =>
    {
        cfg.SetMongoUrl(mongoLogUrl);
        cfg.SetCollectionName("logs");
        cfg.SetBatchPeriod(TimeSpan.FromSeconds(3));
        cfg.SetExpireTTL(TimeSpan.FromDays(30));
    })
    .CreateLogger();

builder.Host.UseSerilog();

var webRootPath = builder.Environment.WebRootPath
    ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRootPath);
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["FileStorage:BasePath"] = webRootPath
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
if (builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(builder.Configuration["Email:SmtpPassword"]))
{
    builder.Services.RemoveAll<IEmailService>();
    builder.Services.AddTransient<IEmailService, DevelopmentEmailService>();
}
builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddNoobGgCors(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddHostedService<DatabaseMigrationRunner>();
var coherentDemo = builder.Environment.IsDevelopment() && builder.Configuration["DemoSeed:Scenario"] == "coherent-200";
if (!coherentDemo) builder.Services.AddHostedService<CommunityBoardBackfillInitializer>();
builder.Services.AddHostedService<MongoIndexInitializer>();
builder.Services.AddHostedService<PlanSeedInitializer>();
if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("DemoSeed:Enabled"))
{
    if (coherentDemo)
    {
        builder.Configuration["Rawg:EnableSync"] = "false";
        builder.Services.AddHostedService<CoherentDemoSeeder>();
    }
    else
    {
    builder.Services.AddHostedService<FakeDataSeeder>();
    builder.Services.AddHostedService<EloDataSeeder>();
    builder.Services.AddHostedService<GuildLeaderboardRoomDemoSeeder>();
    builder.Services.AddHostedService<CommunityFeaturesDemoSeeder>();
    builder.Services.AddHostedService<RoomMemberProfileSeeder>();
    }
}
builder.Services.AddHostedService<GameCatalogSyncJob>();
builder.Services.AddHostedService<GuildEventReminderJob>();
builder.Services.AddHostedService<MatchmakingRecoveryJob>();

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "NoobGg API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseStaticFiles();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors(CorsExtensions.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<RoomHub>("/hubs/room");
app.MapHub<DirectMessageHub>("/hubs/dm");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<GuildChatHub>("/hubs/guild-chat");

Log.Information("NoobGg API starting up");
app.Run();
