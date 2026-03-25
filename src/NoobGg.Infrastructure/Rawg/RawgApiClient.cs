using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Features.Games.DTOs;

namespace NoobGg.Infrastructure.Rawg;

public class RawgApiClient : IRawgApiClient
{
    private readonly HttpClient _httpClient;
    private readonly RawgSettings _settings;
    private readonly ILogger<RawgApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> MultiplayerSlugs = ["multiplayer", "online-multiplayer"];
    private static readonly HashSet<string> CoopSlugs = ["co-op", "online-co-op", "local-co-op", "cooperative"];
    private static readonly HashSet<string> PvpSlugs = ["pvp", "online-pvp"];
    private static readonly HashSet<string> FreeToPlaySlugs = ["free-to-play"];

    public RawgApiClient(HttpClient httpClient, IOptions<RawgSettings> settings, ILogger<RawgApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<RawgPageResult> GetGamesPageAsync(int page, int pageSize = 40, string? ordering = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("RAWG API key is not configured — sync will be skipped");
            return new RawgPageResult(0, [], false);
        }

        var url = $"games?key={_settings.ApiKey}&page={page}&page_size={pageSize}";
        if (!string.IsNullOrWhiteSpace(ordering))
            url += $"&ordering={ordering}";

        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var result = await JsonSerializer.DeserializeAsync<RawgApiResponse>(stream, JsonOptions, ct);

        if (result?.Results is null)
            return new RawgPageResult(0, [], false);

        var items = result.Results
            .Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .Select(MapToListItem)
            .ToList();

        return new RawgPageResult(
            TotalCount: result.Count,
            Results: items,
            HasNextPage: !string.IsNullOrWhiteSpace(result.Next));
    }

    public async Task<RawgGameDetail?> GetGameDetailAsync(int rawgId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            return null;

        try
        {
            var url = $"games/{rawgId}?key={_settings.ApiKey}";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<RawgApiGameDetail>(stream, JsonOptions, ct);

            if (data is null)
                return null;

            return new RawgGameDetail(
                Id: data.Id,
                Slug: data.Slug ?? string.Empty,
                Name: data.Name ?? string.Empty,
                DescriptionRaw: data.DescriptionRaw,
                Released: data.Released,
                BackgroundImage: data.BackgroundImage,
                Rating: data.Rating,
                Metacritic: data.Metacritic,
                Genres: data.Genres?.Select(g => g.Name).ToList() ?? [],
                Tags: data.Tags?.Select(t => t.Name).ToList() ?? [],
                Platforms: data.ParentPlatforms?.Select(p => p.Platform?.Name ?? "").Where(n => n != "").ToList() ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch RAWG game details for id {RawgId}", rawgId);
            return null;
        }
    }

    private static RawgGameListItem MapToListItem(RawgApiGameResult r)
    {
        var tagSlugs = r.Tags?.Select(t => t.Slug ?? "").ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        return new RawgGameListItem(
            Id: r.Id,
            Slug: r.Slug ?? string.Empty,
            Name: r.Name ?? string.Empty,
            Released: r.Released,
            BackgroundImage: r.BackgroundImage,
            Rating: r.Rating,
            Metacritic: r.Metacritic,
            Genres: r.Genres?.Select(g => g.Name).ToList() ?? [],
            Tags: r.Tags?.Select(t => t.Name).ToList() ?? [],
            Platforms: r.ParentPlatforms?.Select(p => p.Platform?.Name ?? "").Where(n => n != "").ToList() ?? [],
            IsMultiplayer: tagSlugs.Overlaps(MultiplayerSlugs),
            IsCoop: tagSlugs.Overlaps(CoopSlugs),
            IsPvp: tagSlugs.Overlaps(PvpSlugs),
            IsFreeToPlay: tagSlugs.Overlaps(FreeToPlaySlugs));
    }

    #region RAWG API response DTOs

    private class RawgApiResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("results")]
        public List<RawgApiGameResult>? Results { get; set; }
    }

    private class RawgApiGameResult
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("released")]
        public string? Released { get; set; }

        [JsonPropertyName("background_image")]
        public string? BackgroundImage { get; set; }

        [JsonPropertyName("rating")]
        public double? Rating { get; set; }

        [JsonPropertyName("metacritic")]
        public int? Metacritic { get; set; }

        [JsonPropertyName("genres")]
        public List<RawgNamedItem>? Genres { get; set; }

        [JsonPropertyName("tags")]
        public List<RawgTagItem>? Tags { get; set; }

        [JsonPropertyName("parent_platforms")]
        public List<RawgParentPlatform>? ParentPlatforms { get; set; }
    }

    private class RawgApiGameDetail
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description_raw")]
        public string? DescriptionRaw { get; set; }

        [JsonPropertyName("released")]
        public string? Released { get; set; }

        [JsonPropertyName("background_image")]
        public string? BackgroundImage { get; set; }

        [JsonPropertyName("rating")]
        public double? Rating { get; set; }

        [JsonPropertyName("metacritic")]
        public int? Metacritic { get; set; }

        [JsonPropertyName("genres")]
        public List<RawgNamedItem>? Genres { get; set; }

        [JsonPropertyName("tags")]
        public List<RawgTagItem>? Tags { get; set; }

        [JsonPropertyName("parent_platforms")]
        public List<RawgParentPlatform>? ParentPlatforms { get; set; }
    }

    private class RawgNamedItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("slug")]
        public string? Slug { get; set; }
    }

    private class RawgTagItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("slug")]
        public string? Slug { get; set; }
    }

    private class RawgParentPlatform
    {
        [JsonPropertyName("platform")]
        public RawgNamedItem? Platform { get; set; }
    }

    #endregion
}
