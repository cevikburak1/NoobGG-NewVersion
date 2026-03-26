using NoobGg.Domain.Enums;

namespace NoobGg.Application.Common.Interfaces;

public interface INotificationService
{
    Task CreateAsync(
        string userId,
        NotificationType type,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default);
}
