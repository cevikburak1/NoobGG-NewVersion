namespace NoobGg.Application.Features.Games.DTOs;

public record GameResponse
{
    public string Id { get; init; } = string.Empty;
    public int RawgId { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? BackgroundImageUrl { get; init; }
    public string? ReleasedAt { get; init; }
    public double? Rating { get; init; }
    public int? Metacritic { get; init; }
    public List<string> Genres { get; init; } = [];
    public List<string> Tags { get; init; } = [];
    public List<string> Platforms { get; init; } = [];
    public bool IsMultiplayer { get; init; }
    public bool IsCoop { get; init; }
    public bool IsPvp { get; init; }
    public bool IsFreeToPlay { get; init; }
}
