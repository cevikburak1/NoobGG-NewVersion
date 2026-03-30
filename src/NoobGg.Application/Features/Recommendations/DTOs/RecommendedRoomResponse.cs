namespace NoobGg.Application.Features.Recommendations.DTOs;

public record RecommendedRoomResponse
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string GameId { get; init; } = string.Empty;
    public string? GameName { get; init; }
    public string? GameImageUrl { get; init; }
    public string CreatorId { get; init; } = string.Empty;
    public int MaxMembers { get; init; }
    public int CurrentMemberCount { get; init; }
    public string Region { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = [];
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public int Score { get; init; }
    public List<string> MatchReasons { get; init; } = [];
}
