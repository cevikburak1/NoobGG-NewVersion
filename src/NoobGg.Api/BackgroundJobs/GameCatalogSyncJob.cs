using Microsoft.Extensions.Options;
using NoobGg.Application.Features.Games.Services;
using NoobGg.Infrastructure.Rawg;

namespace NoobGg.Api.BackgroundJobs;

public class GameCatalogSyncJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RawgSettings _settings;
    private readonly ILogger<GameCatalogSyncJob> _logger;

    public GameCatalogSyncJob(
        IServiceScopeFactory scopeFactory,
        IOptions<RawgSettings> settings,
        ILogger<GameCatalogSyncJob> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_settings.EnableSync)
        {
            _logger.LogInformation("Game catalog sync is disabled via configuration");
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(15), ct);

        while (!ct.IsCancellationRequested)
        {
            await RunSyncCycle(ct);

            _logger.LogInformation("Next game catalog sync in {Hours} hours", _settings.SyncIntervalHours);
            await Task.Delay(TimeSpan.FromHours(_settings.SyncIntervalHours), ct);
        }
    }

    private async Task RunSyncCycle(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IGameSyncService>();

            _logger.LogInformation("Starting game catalog sync cycle (RAWG)");
            var catalogCount = await syncService.SyncCatalogAsync(ct);
            _logger.LogInformation("Catalog sync finished: {Count} games processed", catalogCount);

            _logger.LogInformation("Starting game enrichment (batch: {Size})", _settings.EnrichmentBatchSize);
            var enrichedCount = await syncService.EnrichGamesAsync(_settings.EnrichmentBatchSize, ct);
            _logger.LogInformation("Enrichment finished: {Count} games enriched", enrichedCount);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("Game catalog sync cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Game catalog sync cycle failed");
        }
    }
}
