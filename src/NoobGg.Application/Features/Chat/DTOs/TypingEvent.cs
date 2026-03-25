namespace NoobGg.Application.Features.Chat.DTOs;

public record TypingEvent
{
    public string UserId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string RoomId { get; init; } = string.Empty;
}
