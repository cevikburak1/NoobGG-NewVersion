using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class GuildMember : BaseEntity
{
    public string GuildId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public GuildMemberRole Role { get; set; } = GuildMemberRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
