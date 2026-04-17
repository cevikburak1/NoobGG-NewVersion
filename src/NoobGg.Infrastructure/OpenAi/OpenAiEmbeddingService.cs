using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoobGg.Application.Common.Interfaces;

namespace NoobGg.Infrastructure.OpenAi;

public class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ICacheService _cacheService;
    private readonly OpenAiSettings _settings;
    private readonly ILogger<OpenAiEmbeddingService> _logger;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ApiKey);

    public OpenAiEmbeddingService(
        HttpClient httpClient,
        ICacheService cacheService,
        IOptions<OpenAiSettings> settings,
        ILogger<OpenAiEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _cacheService = cacheService;
        _settings = settings.Value;
        _logger = logger;

        if (IsConfigured)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }
    }

    public async Task<float[]?> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("OpenAI API key is not configured");
            return null;
        }

        try
        {
            var request = new EmbeddingRequest
            {
                Input = text,
                Model = _settings.EmbeddingModel
            };

            var response = await _httpClient.PostAsJsonAsync(
                "https://api.openai.com/v1/embeddings",
                request,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("OpenAI embedding request failed: {StatusCode} - {Content}",
                    response.StatusCode, errorContent);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(ct);
            return result?.Data?.FirstOrDefault()?.Embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get embedding from OpenAI");
            return null;
        }
    }

    public async Task<float[]?> GetCachedEmbeddingAsync(string cacheKey, string text, CancellationToken ct = default)
    {
        var cached = await _cacheService.GetAsync<float[]>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var embedding = await GetEmbeddingAsync(text, ct);
        if (embedding is not null)
        {
            await _cacheService.SetAsync(
                cacheKey,
                embedding,
                TimeSpan.FromMinutes(_settings.EmbeddingCacheTtlMinutes),
                ct);
        }

        return embedding;
    }

    private class EmbeddingRequest
    {
        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
    }

    private class EmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingData>? Data { get; set; }
    }

    private class EmbeddingData
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }
}
