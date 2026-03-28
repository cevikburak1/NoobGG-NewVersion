using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Chat.DTOs;

public record ChatMessageResponse
{
    public string Id { get; init; } = string.Empty;
    public string RoomId { get; init; } = string.Empty;
    public string SenderId { get; init; } = string.Empty;
    public string SenderUsername { get; init; } = string.Empty;
    public string? SenderAvatarUrl { get; init; }
    public string Content { get; init; } = string.Empty;
    public MessageType Type { get; init; }
    public bool IsEdited { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? EditedAt { get; init; }
}
