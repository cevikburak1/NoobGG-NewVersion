namespace NoobGg.Infrastructure.Caching;

public class RedisSettings
{
    public const string SectionName = "Redis";
    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = "NoobGg:";
}
