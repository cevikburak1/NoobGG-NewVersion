using NoobGg.Application.Features.Users.DTOs;

namespace NoobGg.Application.Features.Recommendations.DTOs;

public record RecommendedPlayerResponse
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public string? Country { get; init; }
    public List<PlayerGameInfo> Games { get; init; } = [];
    public bool LookingForTeam { get; init; }
    public string? Region { get; init; }
    public string? ExperienceLevel { get; init; }
    public string? CommunicationPreference { get; init; }
    public double Score { get; init; }
    public List<string> MatchReasons { get; init; } = [];
}
