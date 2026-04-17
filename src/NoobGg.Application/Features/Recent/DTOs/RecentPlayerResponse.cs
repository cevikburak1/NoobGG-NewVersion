namespace NoobGg.Application.Features.Recent.DTOs;

public class RecentPlayerResponse
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Country { get; set; }
    public bool IsOnline { get; set; }
    public DateTime SeenAt { get; set; }
}
