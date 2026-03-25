using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class Report : BaseEntity
{
    public string ReporterId { get; set; } = string.Empty;
    public ReportTargetType TargetType { get; set; } = ReportTargetType.User;
    public string ReportedUserId { get; set; } = string.Empty;
    public string? RoomId { get; set; }
    public ReportReason Reason { get; set; }
    public string? Description { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public string? ReviewedBy { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
