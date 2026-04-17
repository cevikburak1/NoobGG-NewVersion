namespace NoobGg.Application.Features.Recent.DTOs;

public class RecentRoomResponse
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? GameName { get; set; }
    public string? GameImageUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CurrentMemberCount { get; set; }
    public int MaxMembers { get; set; }
    public DateTime SeenAt { get; set; }
}
