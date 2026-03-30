using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class Guild : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CreatorId { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
    public Region Region { get; set; }
    public Language Language { get; set; }
    public List<string> GameIds { get; set; } = [];
    public int MaxMembers { get; set; } = 50;
    public int CurrentMemberCount { get; set; }
}
