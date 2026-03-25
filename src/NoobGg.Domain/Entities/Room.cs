using NoobGg.Domain.Enums;
using NoobGg.Domain.ValueObjects;

namespace NoobGg.Domain.Entities;

public class Room : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string GameId { get; set; } = string.Empty;
    public string CreatorId { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
    public Region Region { get; set; }
    public Language Language { get; set; }
    public RankRange? RankRange { get; set; }
    public List<string> Tags { get; set; } = [];
    public int MaxMembers { get; set; } = 5;
    public int CurrentMemberCount { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Open;
    public DateTime? ClosedAt { get; set; }
    public string? VoiceChannelId { get; set; }
}
