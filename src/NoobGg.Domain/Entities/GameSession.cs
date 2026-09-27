using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class GameSession : BaseEntity
{
    public string RoomId { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string RoomTitle { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public Region Region { get; set; }
    public Language Language { get; set; }
    public List<string> ParticipantIds { get; set; } = [];
    public DateTime EndedAt { get; set; }
    public string? ReplayRoomId { get; set; }
}
