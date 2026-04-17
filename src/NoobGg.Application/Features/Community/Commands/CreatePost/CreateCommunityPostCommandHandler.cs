using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Community.Commands.CreatePost;

public class CreateCommunityPostCommandHandler
    : IRequestHandler<CreateCommunityPostCommand, Result<CommunityPostResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public CreateCommunityPostCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<CommunityPostResponse>> Handle(
        CreateCommunityPostCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<CommunityPostResponse>.Unauthorized();

        var userId = _currentUser.UserId;

        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        Game? game = null;

        if (request.BoardType == CommunityBoardType.Game)
        {
            game = await games.Find(g => g.Id == request.GameId && g.IsActive).FirstOrDefaultAsync(ct);
            if (game is null)
                return Result<CommunityPostResponse>.NotFound("Game not found or inactive");
        }

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var user = await users.Find(u => u.Id == userId).FirstOrDefaultAsync(ct);
        if (user is null)
            return Result<CommunityPostResponse>.NotFound("User not found");

        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var profile = await profiles.Find(p => p.UserId == userId).FirstOrDefaultAsync(ct);

        var topicTitle = BuildTitle(request.Title, request.Content);
        var post = new CommunityPost
        {
            AuthorId = userId,
            BoardType = request.BoardType,
            Category = NormalizeCategory(request.Category),
            Title = topicTitle,
            Slug = BuildSlug(topicTitle),
            GameId = game?.Id,
            Content = request.Content,
            ImageUrl = request.ImageUrl,
            UpvoteCount = 0,
            CommentCount = 0,
            LastActivityAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var posts = _mongoContext.GetCollection<CommunityPost>(CollectionNames.CommunityPosts);
        await posts.InsertOneAsync(post, cancellationToken: ct);

        var response = new CommunityPostResponse(
            post.Id,
            post.Slug,
            post.Title,
            post.AuthorId,
            user.Username,
            profile?.AvatarUrl,
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
            false,
            post.IsPinned,
            post.IsLocked,
            post.LastActivityAt,
            post.CreatedAt);

        return Result<CommunityPostResponse>.Created(response);
    }

    private static string BuildTitle(string? title, string content)
    {
        var trimmedTitle = title?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedTitle))
            return trimmedTitle;

        var cleaned = content.Trim();
        if (cleaned.Length <= 64)
            return cleaned;

        return $"{cleaned[..61].TrimEnd()}...";
    }

    private static string NormalizeCategory(string category)
    {
        var trimmed = category.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? "Discussion" : trimmed;
    }

    private static string BuildSlug(string title)
    {
        var chars = title
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        var slug = string.Join(string.Empty, chars)
            .Split('-', StringSplitOptions.RemoveEmptyEntries);

        return slug.Length == 0
            ? Guid.NewGuid().ToString("N")[..10]
            : string.Join("-", slug).Trim('-');
    }
}
