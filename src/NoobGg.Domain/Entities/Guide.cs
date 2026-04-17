using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class Guide : BaseEntity
{
    public string AuthorId { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public List<string> Tags { get; set; } = [];
    public GuideStatus Status { get; set; } = GuideStatus.Published;
    public int UpvoteCount { get; set; }
    public int ViewCount { get; set; }
}
