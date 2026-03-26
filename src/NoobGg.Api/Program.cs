using System.Text.Json.Serialization;
using NoobGg.Api.BackgroundJobs;
using NoobGg.Api.Extensions;
using NoobGg.Api.Hubs;
using NoobGg.Api.Middleware;
using NoobGg.Application;
using NoobGg.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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

// Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddNoobGgCors(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddHostedService<DatabaseMigrationRunner>();
builder.Services.AddHostedService<MongoIndexInitializer>();
builder.Services.AddHostedService<PlanSeedInitializer>();
builder.Services.AddHostedService<GameCatalogSyncJob>();

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

Log.Information("NoobGg API starting up");
app.Run();
