using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class GuildInvite : BaseEntity
{
    public string GuildId { get; set; } = string.Empty;
    public string InviterId { get; set; } = string.Empty;
    public string InvitedUserId { get; set; } = string.Empty;
    public GuildInviteStatus Status { get; set; } = GuildInviteStatus.Pending;
    public DateTime? RespondedAt { get; set; }
}
