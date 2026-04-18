namespace NoobGg.Domain.Entities;

public class CommunityBoard : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string? GameId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public string Accent { get; set; } = "from-accent/30 via-info/10 to-transparent";
    public string? CoverImageUrl { get; set; }
}
