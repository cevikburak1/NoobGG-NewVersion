using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class RoomMember : BaseEntity
{
    public string RoomId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public RoomMemberRole Role { get; set; } = RoomMemberRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
