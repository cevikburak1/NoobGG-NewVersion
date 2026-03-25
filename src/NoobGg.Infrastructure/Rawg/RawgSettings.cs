namespace NoobGg.Infrastructure.Rawg;

public class RawgSettings
{
    public const string SectionName = "Rawg";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.rawg.io/api";
    public int SyncIntervalHours { get; set; } = 24;
    public int MaxSyncPages { get; set; } = 250;
    public int PageSize { get; set; } = 40;
    public int SyncDelayMs { get; set; } = 250;
    public int EnrichmentBatchSize { get; set; } = 50;
    public int EnrichmentDelayMs { get; set; } = 300;
    public bool EnableSync { get; set; } = true;
}
