using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Settings.DTOs;

public record UserSettingsResponse
{
    public ProfileVisibility ProfileVisibility { get; init; }
    public DmPermission DmPermission { get; init; }
    public bool ShowOnlineStatus { get; init; }
    public bool DefaultLookingForTeam { get; init; }

    public bool NotifyFriendRequests { get; init; }
    public bool NotifyDirectMessages { get; init; }
    public bool NotifyRoomActivity { get; init; }
    public bool NotifySystemMessages { get; init; }

    public bool IsDeactivated { get; init; }
    public DateTime? DeactivatedAt { get; init; }
    public DateTime? DeletionRequestedAt { get; init; }
}
