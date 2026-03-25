namespace NoobGg.Application.Features.Games.DTOs;

public record RawgPageResult(
    int TotalCount,
    List<RawgGameListItem> Results,
    bool HasNextPage);

public record RawgGameListItem(
    int Id,
    string Slug,
    string Name,
    string? Released,
    string? BackgroundImage,
    double? Rating,
    int? Metacritic,
    List<string> Genres,
    List<string> Tags,
    List<string> Platforms,
    bool IsMultiplayer,
    bool IsCoop,
    bool IsPvp,
    bool IsFreeToPlay);

public record RawgGameDetail(
    int Id,
    string Slug,
    string Name,
    string? DescriptionRaw,
    string? Released,
    string? BackgroundImage,
    double? Rating,
    int? Metacritic,
    List<string> Genres,
    List<string> Tags,
    List<string> Platforms);
