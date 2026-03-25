using NoobGg.Application.Features.Games.DTOs;

namespace NoobGg.Application.Common.Interfaces;

public interface IRawgApiClient
{
    Task<RawgPageResult> GetGamesPageAsync(int page, int pageSize = 40, string? ordering = null, CancellationToken ct = default);
    Task<RawgGameDetail?> GetGameDetailAsync(int rawgId, CancellationToken ct = default);
}
