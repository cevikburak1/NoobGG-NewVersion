namespace NoobGg.Application.Features.Chat.DTOs;

public record ChatPresenceEvent
{
    public string UserId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string RoomId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
