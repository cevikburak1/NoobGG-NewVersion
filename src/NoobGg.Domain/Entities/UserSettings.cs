using NoobGg.Domain.Enums;

namespace NoobGg.Domain.Entities;

public class UserSettings : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public ProfileVisibility ProfileVisibility { get; set; } = ProfileVisibility.Public;
    public DmPermission DmPermission { get; set; } = DmPermission.Everyone;
    public bool ShowOnlineStatus { get; set; } = true;
    public bool DefaultLookingForTeam { get; set; }

    public bool NotifyFriendRequests { get; set; } = true;
    public bool NotifyDirectMessages { get; set; } = true;
    public bool NotifyRoomActivity { get; set; } = true;
    public bool NotifySystemMessages { get; set; } = true;

    public bool IsDeactivated { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public string? DeactivationReason { get; set; }
    public DateTime? DeletionRequestedAt { get; set; }
}
