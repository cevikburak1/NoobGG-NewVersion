using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class ContentVote : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public ContentVoteTargetType TargetType { get; set; }
}
