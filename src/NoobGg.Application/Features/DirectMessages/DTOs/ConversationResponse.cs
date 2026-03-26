namespace NoobGg.Application.Features.DirectMessages.DTOs;

public record ConversationResponse
{
    public string Id { get; init; } = string.Empty;
    public string PartnerId { get; init; } = string.Empty;
    public string PartnerUsername { get; init; } = string.Empty;
    public string? PartnerAvatarUrl { get; init; }
    public string? LastMessageContent { get; init; }
    public string? LastMessageSenderId { get; init; }
    public DateTime? LastMessageAt { get; init; }
    public int UnreadCount { get; init; }
}
