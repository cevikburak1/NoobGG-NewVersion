using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Features.Games.Services;
using NoobGg.Domain.Entities;

namespace NoobGg.Infrastructure.Rawg;

public class GameSyncService : IGameSyncService
{
    private readonly IRawgApiClient _rawgClient;
    private readonly IMongoContext _mongoContext;
    private readonly RawgSettings _settings;
    private readonly ILogger<GameSyncService> _logger;

    public GameSyncService(
        IRawgApiClient rawgClient,
        IMongoContext mongoContext,
        IOptions<RawgSettings> settings,
        ILogger<GameSyncService> logger)
    {
        _rawgClient = rawgClient;
        _mongoContext = mongoContext;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<int> SyncCatalogAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting RAWG catalog sync (max {MaxPages} pages, {PageSize}/page)",
            _settings.MaxSyncPages, _settings.PageSize);

        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var totalProcessed = 0;

        for (var page = 1; page <= _settings.MaxSyncPages; page++)
        {
            if (ct.IsCancellationRequested) break;

            var pageResult = await _rawgClient.GetGamesPageAsync(page, _settings.PageSize, "-added", ct);

            if (pageResult.Results.Count == 0)
            {
                if (page == 1)
                    _logger.LogWarning("No games returned from RAWG — check API key configuration");
                break;
            }

            var bulkOps = pageResult.Results.Select(app =>
            {
                var filter = Builders<Game>.Filter.Eq(g => g.RawgId, app.Id);
                var update = Builders<Game>.Update
                    .Set(g => g.Slug, app.Slug)
                    .Set(g => g.Name, app.Name)
                    .Set(g => g.NameNormalized, app.Name.Trim().ToLowerInvariant())
                    .Set(g => g.BackgroundImageUrl, app.BackgroundImage)
                    .Set(g => g.ReleasedAt, app.Released)
                    .Set(g => g.Rating, app.Rating)
                    .Set(g => g.Metacritic, app.Metacritic)
                    .Set(g => g.Genres, app.Genres)
                    .Set(g => g.Tags, app.Tags)
                    .Set(g => g.Platforms, app.Platforms)
                    .Set(g => g.IsMultiplayer, app.IsMultiplayer)
                    .Set(g => g.IsCoop, app.IsCoop)
                    .Set(g => g.IsPvp, app.IsPvp)
                    .Set(g => g.IsFreeToPlay, app.IsFreeToPlay)
                    .Set(g => g.UpdatedAt, DateTime.UtcNow)
                    .SetOnInsert(g => g.Id, Guid.NewGuid().ToString())
                    .SetOnInsert(g => g.CreatedAt, DateTime.UtcNow)
                    .SetOnInsert(g => g.IsActive, true);

                return new UpdateOneModel<Game>(filter, update) { IsUpsert = true };
            }).ToList();

            var result = await games.BulkWriteAsync(bulkOps, cancellationToken: ct);
            totalProcessed += pageResult.Results.Count;

            _logger.LogDebug("Page {Page}: {New} inserted, {Updated} updated (total: {Total})",
                page, result.Upserts.Count, result.ModifiedCount, totalProcessed);

            if (!pageResult.HasNextPage) break;

            await Task.Delay(_settings.SyncDelayMs, ct);
        }

        _logger.LogInformation("RAWG catalog sync complete: {Total} games processed", totalProcessed);
        return totalProcessed;
    }

    public async Task<int> EnrichGamesAsync(int batchSize = 50, CancellationToken ct = default)
    {
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var enrichmentThreshold = DateTime.UtcNow.AddDays(-30);

        var filter = Builders<Game>.Filter.And(
            Builders<Game>.Filter.Eq(g => g.IsActive, true),
            Builders<Game>.Filter.Or(
                Builders<Game>.Filter.Eq(g => g.LastEnrichedAt, null),
                Builders<Game>.Filter.Lt(g => g.LastEnrichedAt, enrichmentThreshold)));

        var gamesToEnrich = await games.Find(filter)
            .Limit(batchSize)
            .ToListAsync(ct);

        if (gamesToEnrich.Count == 0)
        {
            _logger.LogInformation("No games need enrichment");
            return 0;
        }

        _logger.LogInformation("Enriching {Count} games via RAWG details API", gamesToEnrich.Count);
        var enriched = 0;

        foreach (var game in gamesToEnrich)
        {
            if (ct.IsCancellationRequested) break;

            var detail = await _rawgClient.GetGameDetailAsync(game.RawgId, ct);

            if (detail is null)
            {
                await games.UpdateOneAsync(
                    Builders<Game>.Filter.Eq(g => g.Id, game.Id),
                    Builders<Game>.Update
                        .Set(g => g.LastEnrichedAt, DateTime.UtcNow)
                        .Set(g => g.UpdatedAt, DateTime.UtcNow),
                    cancellationToken: ct);
                continue;
            }

            var update = Builders<Game>.Update
                .Set(g => g.Description, detail.DescriptionRaw)
                .Set(g => g.LastEnrichedAt, DateTime.UtcNow)
                .Set(g => g.UpdatedAt, DateTime.UtcNow);

            await games.UpdateOneAsync(
                Builders<Game>.Filter.Eq(g => g.Id, game.Id),
                update,
                cancellationToken: ct);

            enriched++;
            await Task.Delay(_settings.EnrichmentDelayMs, ct);
        }

        _logger.LogInformation("Enrichment complete: {Enriched}/{Total} games enriched", enriched, gamesToEnrich.Count);
        return enriched;
    }
}
