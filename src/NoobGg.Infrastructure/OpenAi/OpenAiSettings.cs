namespace NoobGg.Infrastructure.OpenAi;

public class OpenAiSettings
{
    public const string SectionName = "OpenAi";

    public string ApiKey { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public int EmbeddingCacheTtlMinutes { get; set; } = 60;
}
