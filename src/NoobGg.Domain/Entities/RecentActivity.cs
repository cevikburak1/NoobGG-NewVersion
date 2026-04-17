using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class RecentActivity : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public RecentActivityTargetType TargetType { get; set; }
    public DateTime SeenAt { get; set; } = DateTime.UtcNow;
}
