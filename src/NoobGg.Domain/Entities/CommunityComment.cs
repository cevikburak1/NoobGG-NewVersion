namespace NoobGg.Domain.Entities;

public class CommunityComment : BaseEntity
{
    public string PostId { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int UpvoteCount { get; set; }
}
