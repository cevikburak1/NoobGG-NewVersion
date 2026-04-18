using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Community.Commands.CreateBoard;

public class CreateCommunityBoardCommandHandler
    : IRequestHandler<CreateCommunityBoardCommand, Result<CommunityBoardResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public CreateCommunityBoardCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<CommunityBoardResponse>> Handle(CreateCommunityBoardCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            return Result<CommunityBoardResponse>.Unauthorized();

        var slugSeed = string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug;
        var baseSlug = BuildSlug(slugSeed);
        if (string.IsNullOrWhiteSpace(baseSlug))
            return Result<CommunityBoardResponse>.Fail("Board slug could not be generated");

        var boards = _mongoContext.GetCollection<CommunityBoard>(CollectionNames.CommunityBoards);
        var slug = await EnsureUniqueSlugAsync(boards, baseSlug, ct);

        var gameId = string.IsNullOrWhiteSpace(request.GameId) ? null : request.GameId.Trim();
        Game? game = null;
        if (!string.IsNullOrWhiteSpace(gameId))
        {
            var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
            game = await games.Find(g => g.Id == gameId && g.IsActive).FirstOrDefaultAsync(ct);
            if (game is null)
                return Result<CommunityBoardResponse>.NotFound("Game not found or inactive");
        }

        var board = new CommunityBoard
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description.Trim(),
            Category = NormalizeCategory(request.Category),
            GameId = game?.Id,
            CreatedByUserId = _currentUser.UserId!,
            CoverImageUrl = string.IsNullOrWhiteSpace(request.CoverImageUrl) ? game?.BackgroundImageUrl : request.CoverImageUrl.Trim(),
            Accent = BuildAccent(request.Category),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await boards.InsertOneAsync(board, cancellationToken: ct);

        var boardType = game is null ? CommunityBoardType.General : CommunityBoardType.Game;
        return Result<CommunityBoardResponse>.Created(
            new CommunityBoardResponse(
                board.Id,
                board.Slug,
                board.Name,
                board.Description,
                board.Category,
                boardType,
                game?.Id,
                game?.Name,
                game?.Slug,
                board.CoverImageUrl,
                0,
                null,
                board.Accent));
    }

    private static string NormalizeCategory(string category)
    {
        var trimmed = category.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? "General" : trimmed;
    }

    private static string BuildAccent(string category)
    {
        var normalized = category.Trim().ToLowerInvariant();
        return normalized switch
        {
            "strategy" => "from-primary/35 via-primary/10 to-transparent",
            "lfg" or "looking for team" => "from-info/35 via-accent/10 to-transparent",
            "meta" => "from-accent/30 via-info/10 to-transparent",
            _ => "from-accent/30 via-primary/10 to-transparent",
        };
    }

    private static async Task<string> EnsureUniqueSlugAsync(
        IMongoCollection<CommunityBoard> boards,
        string baseSlug,
        CancellationToken ct)
    {
        var slug = baseSlug;
        var suffix = 2;
        while (await boards.Find(b => b.Slug == slug).AnyAsync(ct))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }

    private static string BuildSlug(string title)
    {
        var chars = title
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        var parts = new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0
            ? string.Empty
            : string.Join("-", parts).Trim('-');
    }
}
