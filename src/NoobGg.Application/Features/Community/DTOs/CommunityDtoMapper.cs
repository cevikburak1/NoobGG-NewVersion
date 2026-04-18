using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Community.DTOs;

internal static class CommunityDtoMapper
{
    public static CommunityPostResponse ToPostResponse(
        CommunityPost post,
        Dictionary<string, CommunityBoard> boardMap,
        Dictionary<string, User> userMap,
        Dictionary<string, UserProfile> profileMap,
        Dictionary<string, Game> gameMap,
        HashSet<string> votedPostIds)
    {
        var title = string.IsNullOrWhiteSpace(post.Title)
            ? BuildFallbackTitle(post.Content)
            : post.Title;
        var slug = string.IsNullOrWhiteSpace(post.Slug)
            ? post.Id
            : post.Slug;

        userMap.TryGetValue(post.AuthorId, out var author);
        profileMap.TryGetValue(post.AuthorId, out var profile);
        var boardId = post.BoardId ?? "general";
        boardMap.TryGetValue(boardId, out var board);

        Game? game = null;
        if (!string.IsNullOrWhiteSpace(post.GameId))
            gameMap.TryGetValue(post.GameId, out game);

        return new CommunityPostResponse(
            post.Id,
            slug,
            title,
            post.AuthorId,
            author?.Username ?? "Unknown",
            profile?.AvatarUrl,
            board?.Id ?? boardId,
            board?.Slug ?? "general",
            board?.Name ?? "General Players Forum",
            post.BoardType,
            post.Category,
            post.GameId,
            game?.Name,
            game?.Slug,
            game?.BackgroundImageUrl,
            post.Content,
            post.ImageUrl,
            post.UpvoteCount,
            post.CommentCount,
            votedPostIds.Contains(post.Id),
            post.IsPinned,
            post.IsLocked,
            post.LastActivityAt,
            post.CreatedAt);
    }

    private static string BuildFallbackTitle(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.Length <= 72)
            return trimmed;

        return $"{trimmed[..69].TrimEnd()}...";
    }
}
