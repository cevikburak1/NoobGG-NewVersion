namespace NoobGg.Application.Features.Users.DTOs;

public record RecentActivityResponse
{
    public List<RecentPlayerItem> RecentPlayers { get; init; } = [];
    public List<RecentConversationItem> RecentConversations { get; init; } = [];
    public List<RecentRoomItem> RecentRooms { get; init; } = [];
}

public record RecentPlayerItem
{
    public string UserId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public DateTime LastInteractionAt { get; init; }
    public string Source { get; init; } = string.Empty;
}

public record RecentConversationItem
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

public record RecentRoomItem
{
    public string RoomId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string GameId { get; init; } = string.Empty;
    public string? GameName { get; init; }
    public string? GameImageUrl { get; init; }
    public DateTime JoinedAt { get; init; }
    public int CurrentMemberCount { get; init; }
    public string Region { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
