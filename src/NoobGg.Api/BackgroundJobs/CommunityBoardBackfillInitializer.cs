using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Api.BackgroundJobs;

public class CommunityBoardBackfillInitializer : IHostedService
{
    private readonly IMongoContext _mongo;
    private readonly ILogger<CommunityBoardBackfillInitializer> _logger;

    public CommunityBoardBackfillInitializer(
        IMongoContext mongo,
        ILogger<CommunityBoardBackfillInitializer> logger)
    {
        _mongo = mongo;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            var boards = _mongo.GetCollection<CommunityBoard>(CollectionNames.CommunityBoards);
            var posts = _mongo.GetCollection<CommunityPost>(CollectionNames.CommunityPosts);
            var games = _mongo.GetCollection<Game>(CollectionNames.Games);

            var generalBoard = await EnsureGeneralBoardAsync(boards, ct);
            var updatedGeneral = await posts.UpdateManyAsync(
                Builders<CommunityPost>.Filter.And(
                    Builders<CommunityPost>.Filter.Eq(p => p.BoardType, CommunityBoardType.General),
                    Builders<CommunityPost>.Filter.Or(
                        Builders<CommunityPost>.Filter.Eq(p => p.BoardId, null),
                        Builders<CommunityPost>.Filter.Eq(p => p.BoardId, ""))),
                Builders<CommunityPost>.Update.Set(p => p.BoardId, generalBoard.Id),
                cancellationToken: ct);

            var legacyGameIds = await posts.Aggregate()
                .Match(p => p.BoardType == CommunityBoardType.Game &&
                            (p.BoardId == null || p.BoardId == "") &&
                            p.GameId != null)
                .Group(p => p.GameId!, g => g.Key)
                .ToListAsync(ct);

            var gameDocs = await games.Find(g => legacyGameIds.Contains(g.Id)).ToListAsync(ct);
            var gameMap = gameDocs.ToDictionary(g => g.Id);

            var totalGameUpdates = 0L;
            foreach (var gameId in legacyGameIds)
            {
                if (!gameMap.TryGetValue(gameId, out var game))
                    continue;

                var board = await EnsureGameBoardAsync(boards, game, ct);
                var updated = await posts.UpdateManyAsync(
                    Builders<CommunityPost>.Filter.And(
                        Builders<CommunityPost>.Filter.Eq(p => p.BoardType, CommunityBoardType.Game),
                        Builders<CommunityPost>.Filter.Eq(p => p.GameId, gameId),
                        Builders<CommunityPost>.Filter.Or(
                            Builders<CommunityPost>.Filter.Eq(p => p.BoardId, null),
                            Builders<CommunityPost>.Filter.Eq(p => p.BoardId, ""))),
                    Builders<CommunityPost>.Update.Set(p => p.BoardId, board.Id),
                    cancellationToken: ct);
                totalGameUpdates += updated.ModifiedCount;
            }

            _logger.LogInformation(
                "Community board backfill completed (generalUpdated={GeneralUpdated}, gameUpdated={GameUpdated}, gameBoards={BoardCount})",
                updatedGeneral.ModifiedCount,
                totalGameUpdates,
                legacyGameIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Community board backfill failed");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private static async Task<CommunityBoard> EnsureGeneralBoardAsync(IMongoCollection<CommunityBoard> boards, CancellationToken ct)
    {
        var existing = await boards.Find(b => b.Slug == "general").FirstOrDefaultAsync(ct);
        if (existing is not null)
            return existing;

        var board = new CommunityBoard
        {
            Name = "General Players Forum",
            Slug = "general",
            Description = "Matchups, squad building, hot takes, and everything players want to debate.",
            Category = "General",
            CreatedByUserId = "system",
            Accent = "from-primary/35 via-primary/10 to-transparent",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await boards.InsertOneAsync(board, cancellationToken: ct);
        return board;
    }

    private static async Task<CommunityBoard> EnsureGameBoardAsync(
        IMongoCollection<CommunityBoard> boards,
        Game game,
        CancellationToken ct)
    {
        var existing = await boards.Find(b => b.GameId == game.Id && !b.IsArchived).FirstOrDefaultAsync(ct);
        if (existing is not null)
            return existing;

        var slug = string.IsNullOrWhiteSpace(game.Slug) ? $"game-{game.Id}" : game.Slug;
        var board = new CommunityBoard
        {
            Name = game.Name,
            Slug = slug,
            Description = BuildGameBoardDescription(game),
            Category = "Game",
            GameId = game.Id,
            CreatedByUserId = "system",
            Accent = "from-accent/30 via-info/10 to-transparent",
            CoverImageUrl = game.BackgroundImageUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await boards.InsertOneAsync(board, cancellationToken: ct);
        return board;
    }

    private static string BuildGameBoardDescription(Game game)
    {
        var genre = game.Genres.FirstOrDefault();
        return genre is null
            ? "Strategy, meta shifts, squad requests, and patch reactions for this game."
            : $"{genre} tactics, player requests, patch reactions, and community intel for this game.";
    }
}
