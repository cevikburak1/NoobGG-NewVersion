namespace NoobGg.Infrastructure.Storage;

public class FileStorageSettings
{
    public const string SectionName = "FileStorage";
    public string BasePath { get; set; } = "wwwroot";
}
