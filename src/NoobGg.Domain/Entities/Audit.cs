using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class Audit : BaseEntity
{
    public string ActorId { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}
