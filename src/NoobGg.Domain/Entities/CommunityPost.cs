using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class CommunityPost : BaseEntity
{
    public string AuthorId { get; set; } = string.Empty;
    public string? BoardId { get; set; }
    public CommunityBoardType BoardType { get; set; } = CommunityBoardType.Game;
    public string Category { get; set; } = "Discussion";
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? GameId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int UpvoteCount { get; set; }
    public int CommentCount { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public bool IsPinned { get; set; }
    public bool IsLocked { get; set; }
}
