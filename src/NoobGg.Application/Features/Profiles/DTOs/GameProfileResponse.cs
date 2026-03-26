namespace NoobGg.Application.Features.Profiles.DTOs;

public record GameProfileResponse
{
    public string Id { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string GameId { get; init; } = string.Empty;
    public string GameName { get; init; } = string.Empty;
    public string? GameImageUrl { get; init; }
    public string Rank { get; init; } = string.Empty;
    public string? Role { get; init; }
    public string Region { get; init; } = string.Empty;
    public string ExperienceLevel { get; init; } = string.Empty;
    public string CommunicationPreference { get; init; } = string.Empty;
    public int? HoursPlayed { get; init; }
    public bool LookingForTeam { get; init; }
    public string? Note { get; init; }
    public string? InGameName { get; init; }
}
