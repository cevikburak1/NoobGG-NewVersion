namespace NoobGg.Application.Common.Interfaces;

public interface IEmbeddingService
{
    Task<float[]?> GetEmbeddingAsync(string text, CancellationToken ct = default);
    Task<float[]?> GetCachedEmbeddingAsync(string cacheKey, string text, CancellationToken ct = default);
    bool IsConfigured { get; }
}
