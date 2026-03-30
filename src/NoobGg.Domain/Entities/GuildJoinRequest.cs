using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class GuildJoinRequest : BaseEntity
{
    public string GuildId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? Message { get; set; }
    public GuildJoinRequestStatus Status { get; set; } = GuildJoinRequestStatus.Pending;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
