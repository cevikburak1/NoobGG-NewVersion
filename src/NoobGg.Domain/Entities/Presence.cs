using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

/// <summary>
/// MongoDB fallback for presence state. Primary source of truth is Redis.
/// Persisted here for reconnection recovery and analytics.
/// </summary>
public class Presence : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public PresenceStatus Status { get; set; } = PresenceStatus.Offline;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public string? CurrentRoomId { get; set; }
    public DateTime? ConnectedAt { get; set; }
}
