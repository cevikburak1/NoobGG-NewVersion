namespace NoobGg.Application.Features.Recommendations.DTOs;

public record RecommendedPlayerResponse
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public string? Country { get; init; }
    public List<RecommendedPlayerGameInfo> Games { get; init; } = [];
    public bool LookingForTeam { get; init; }
    public string? Region { get; init; }
    public string? ExperienceLevel { get; init; }
    public string? CommunicationPreference { get; init; }
    public string? FriendshipStatus { get; init; }
    public int Score { get; init; }
    public List<string> MatchReasons { get; init; } = [];
}

public record RecommendedPlayerGameInfo
{
    public string GameId { get; init; } = string.Empty;
    public string GameName { get; init; } = string.Empty;
    public string? GameImageUrl { get; init; }
    public string Rank { get; init; } = string.Empty;
    public string ExperienceLevel { get; init; } = string.Empty;
    public bool LookingForTeam { get; init; }
}
