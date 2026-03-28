using Microsoft.Extensions.Options;
using NoobGg.Application.Common.Interfaces;

namespace NoobGg.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IOptions<FileStorageSettings> settings)
    {
        _rootPath = settings.Value.BasePath;
    }

    public async Task<string> SaveFileAsync(Stream stream, string fileName, string subfolder, CancellationToken ct = default)
    {
        var directory = Path.Combine(_rootPath, "uploads", subfolder);
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, fileName);

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream, ct);

        return $"/uploads/{subfolder}/{fileName}";
    }

    public void DeleteFile(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        var fullPath = Path.Combine(_rootPath, relativePath.TrimStart('/'));

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
