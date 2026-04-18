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

        var boards = _mongoContext.GetCollection<CommunityBoard>(CollectionNames.CommunityBoards);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var board = await boards.Find(b => b.Id == request.BoardId && !b.IsArchived).FirstOrDefaultAsync(ct);
        if (board is null)
            return Result<CommunityPostResponse>.NotFound("Board not found");
        var boardType = string.IsNullOrWhiteSpace(board.GameId) ? CommunityBoardType.General : CommunityBoardType.Game;
        Game? game = null;
        if (!string.IsNullOrWhiteSpace(board.GameId))
            game = await games.Find(g => g.Id == board.GameId && g.IsActive).FirstOrDefaultAsync(ct);

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
            BoardId = board.Id,
            BoardType = boardType,
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

        var boardMap = new Dictionary<string, CommunityBoard> { [board.Id] = board };
        var userMap = new Dictionary<string, User> { [user.Id] = user };
        var profileMap = profile is null ? new Dictionary<string, UserProfile>() : new Dictionary<string, UserProfile> { [user.Id] = profile };
        var gameMap = game is null ? new Dictionary<string, Game>() : new Dictionary<string, Game> { [game.Id] = game };
        var response = CommunityDtoMapper.ToPostResponse(post, boardMap, userMap, profileMap, gameMap, []);

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
