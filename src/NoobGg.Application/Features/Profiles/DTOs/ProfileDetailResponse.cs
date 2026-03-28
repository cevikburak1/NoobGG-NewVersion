namespace NoobGg.Application.Features.Profiles.DTOs;

public record ProfileDetailResponse
{
    public string UserId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? BannerUrl { get; init; }
    public string? Bio { get; init; }
    public string? Country { get; init; }
    public string? Timezone { get; init; }
    public string? Region { get; init; }
    public string? Language { get; init; }
    public string? ExperienceLevel { get; init; }
    public string? CommunicationPreference { get; init; }
    public string? PlaySchedule { get; init; }
    public bool IsProfileComplete { get; init; }
    public DateTime CreatedAt { get; init; }

    public List<GameProfileResponse> Games { get; init; } = [];
    public ProfileStats Stats { get; init; } = new();

    public bool IsOwnProfile { get; init; }
    public bool IsOnline { get; init; }
    public bool IsBlocked { get; init; }
    public bool IsBlockedByThem { get; init; }
    public bool IsRestricted { get; init; }
    public string? RestrictedReason { get; init; }
    public string? FriendshipStatus { get; init; }
    public string? FriendshipId { get; init; }
    public bool IsFriendRequestSentByMe { get; init; }
}

public record ProfileStats
{
    public int RoomsJoined { get; init; }
    public int RoomsCreated { get; init; }
    public int GamesPlayed { get; init; }
}
