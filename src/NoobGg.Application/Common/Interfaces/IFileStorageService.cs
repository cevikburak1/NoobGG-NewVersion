namespace NoobGg.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream stream, string fileName, string subfolder, CancellationToken ct = default);
    void DeleteFile(string relativePath);
}
