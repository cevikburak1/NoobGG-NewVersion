using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class MatchQueueEntry : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public Region Region { get; set; }
    public Language Language { get; set; }
    public int EloPoints { get; set; }
    public string? Role { get; set; }
    public MatchQueueEntryStatus Status { get; set; } = MatchQueueEntryStatus.Searching;
    public string? MatchedRoomId { get; set; }
    public string? MatchedWithUserId { get; set; }
    public DateTime ExpiresAt { get; set; }
}
