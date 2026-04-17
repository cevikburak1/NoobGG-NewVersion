namespace NoobGg.Application.Features.Guides.DTOs;

public record GuideListItemResponse(
    string Id,
    string Title,
    string AuthorId,
    string AuthorUsername,
    string? AuthorAvatarUrl,
    string GameId,
    string? CoverImageUrl,
    List<string> Tags,
    int UpvoteCount,
    int ViewCount,
    bool HasUpvoted,
    DateTime CreatedAt);

public record GuideDetailResponse(
    string Id,
    string Title,
    string Content,
    string AuthorId,
    string AuthorUsername,
    string? AuthorAvatarUrl,
    string GameId,
    string? CoverImageUrl,
    List<string> Tags,
    string Status,
    int UpvoteCount,
    int ViewCount,
    bool HasUpvoted,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record GuideListResponse(
    List<GuideListItemResponse> Guides,
    int TotalCount,
    bool HasMore);
