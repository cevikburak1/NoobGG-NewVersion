using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Community.DTOs;

public record CommunityPostResponse(
    string Id,
    string Slug,
    string Title,
    string AuthorId,
    string AuthorUsername,
    string? AuthorAvatarUrl,
    CommunityBoardType BoardType,
    string Category,
    string? GameId,
    string? GameName,
    string? GameSlug,
    string? GameBackgroundImageUrl,
    string Content,
    string? ImageUrl,
    int UpvoteCount,
    int CommentCount,
    bool HasUpvoted,
    bool IsPinned,
    bool IsLocked,
    DateTime LastActivityAt,
    DateTime CreatedAt);

public record CommunityCommentResponse(
    string Id,
    string AuthorId,
    string AuthorUsername,
    string? AuthorAvatarUrl,
    string Content,
    int UpvoteCount,
    bool HasUpvoted,
    DateTime CreatedAt);

public record CommunityCommentsResponse(
    List<CommunityCommentResponse> Comments,
    int TotalCount,
    bool HasMore,
    int Page,
    int PageSize);

public record CommunityFeedResponse(
    List<CommunityPostResponse> Posts,
    int TotalCount,
    bool HasMore);

public record CommunityTopicListResponse(
    List<CommunityPostResponse> Topics,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasNextPage,
    bool HasPreviousPage);

public record CommunityTopicDetailResponse(
    CommunityPostResponse Topic,
    List<CommunityPostResponse> RelatedTopics);

public record CommunityBoardResponse(
    string Id,
    string Slug,
    string Title,
    string Description,
    CommunityBoardType BoardType,
    string? GameId,
    string? GameName,
    string? GameSlug,
    string? CoverImageUrl,
    int TopicCount,
    DateTime? LastActivityAt,
    string Accent);

public record CommunityBoardsOverviewResponse(
    List<CommunityBoardResponse> Boards,
    List<CommunityPostResponse> TrendingTopics,
    List<CommunityPostResponse> LatestTopics);
