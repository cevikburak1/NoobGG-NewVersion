namespace NoobGg.Domain.Entities;

public class Game : BaseEntity
{
    public int RawgId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameNormalized { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public string? ReleasedAt { get; set; }
    public double? Rating { get; set; }
    public int? Metacritic { get; set; }
    public List<string> Genres { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public List<string> Platforms { get; set; } = [];
    public bool IsMultiplayer { get; set; }
    public bool IsCoop { get; set; }
    public bool IsPvp { get; set; }
    public bool IsFreeToPlay { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastEnrichedAt { get; set; }
}
