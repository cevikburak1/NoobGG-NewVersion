using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class RoomInvite : BaseEntity
{
    public string RoomId { get; set; } = string.Empty;
    public string InviterId { get; set; } = string.Empty;
    public string InvitedUserId { get; set; } = string.Empty;
    public RoomInviteStatus Status { get; set; } = RoomInviteStatus.Pending;
    public DateTime? RespondedAt { get; set; }
}
