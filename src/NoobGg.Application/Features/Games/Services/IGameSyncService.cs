namespace NoobGg.Application.Features.Games.Services;

public interface IGameSyncService
{
    /// <summary>
    /// Pulls the full Steam app list and upserts into the games collection.
    /// Returns the number of games processed.
    /// </summary>
    Task<int> SyncCatalogAsync(CancellationToken ct = default);

    /// <summary>
    /// Fetches detailed info (genres, categories, images) from Steam store API
    /// for games that haven't been enriched recently. Rate-limited internally.
    /// Returns the number of games enriched.
    /// </summary>
    Task<int> EnrichGamesAsync(int batchSize = 50, CancellationToken ct = default);
}
