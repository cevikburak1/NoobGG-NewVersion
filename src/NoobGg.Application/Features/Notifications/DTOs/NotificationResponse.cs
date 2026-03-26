using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Notifications.DTOs;

public record NotificationResponse
{
    public string Id { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public Dictionary<string, string>? Data { get; init; }
    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
