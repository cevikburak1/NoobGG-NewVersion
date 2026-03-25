namespace NoobGg.Application.Features.Chat.DTOs;

public record RoomPresenceResponse
{
    public string RoomId { get; init; } = string.Empty;
    public List<OnlineUserInfo> OnlineUsers { get; init; } = [];
    public int OnlineCount { get; init; }
}

public record OnlineUserInfo
{
    public string UserId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
}
