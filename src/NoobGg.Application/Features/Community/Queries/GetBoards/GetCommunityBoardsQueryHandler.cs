using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Community.Queries.GetBoards;

public class GetCommunityBoardsQueryHandler
    : IRequestHandler<GetCommunityBoardsQuery, Result<CommunityBoardsOverviewResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetCommunityBoardsQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<CommunityBoardsOverviewResponse>> Handle(GetCommunityBoardsQuery request, CancellationToken ct)
    {
        var posts = _mongoContext.GetCollection<CommunityPost>(CollectionNames.CommunityPosts);
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var votes = _mongoContext.GetCollection<ContentVote>(CollectionNames.ContentVotes);

        var latestTopics = await posts.Find(FilterDefinition<CommunityPost>.Empty)
            .SortByDescending(p => p.LastActivityAt)
            .Limit(6)
            .ToListAsync(ct);

        var trendingTopics = await posts.Find(FilterDefinition<CommunityPost>.Empty)
            .SortByDescending(p => p.CommentCount)
            .ThenByDescending(p => p.UpvoteCount)
            .ThenByDescending(p => p.LastActivityAt)
            .Limit(6)
            .ToListAsync(ct);

        var featuredGames = await games.Find(g => g.IsActive)
            .SortByDescending(g => g.Rating)
            .ThenByDescending(g => g.Metacritic)
            .Limit(6)
            .ToListAsync(ct);

        var generalFilter = Builders<CommunityPost>.Filter.Eq(p => p.BoardType, CommunityBoardType.General);
        var generalCount = (int)await posts.CountDocumentsAsync(generalFilter, cancellationToken: ct);
        var generalLastActivity = await posts.Find(generalFilter)
            .SortByDescending(p => p.LastActivityAt)
            .Project(p => (DateTime?)p.LastActivityAt)
            .FirstOrDefaultAsync(ct);

        var boards = new List<CommunityBoardResponse>
        {
            new(
                "general",
                "general",
                "General Players Forum",
                "Matchups, squad building, hot takes, roster calls, and everything players want to debate outside a single game.",
                CommunityBoardType.General,
                null,
                null,
                null,
                null,
                generalCount,
                generalLastActivity,
                "from-primary/35 via-primary/10 to-transparent"),
        };

        foreach (var game in featuredGames)
        {
            var gameFilter = Builders<CommunityPost>.Filter.And(
                Builders<CommunityPost>.Filter.Eq(p => p.BoardType, CommunityBoardType.Game),
                Builders<CommunityPost>.Filter.Eq(p => p.GameId, game.Id));

            var count = (int)await posts.CountDocumentsAsync(gameFilter, cancellationToken: ct);
            var lastActivity = await posts.Find(gameFilter)
                .SortByDescending(p => p.LastActivityAt)
                .Project(p => (DateTime?)p.LastActivityAt)
                .FirstOrDefaultAsync(ct);

            boards.Add(new CommunityBoardResponse(
                game.Id,
                game.Slug,
                game.Name,
                BuildGameBoardDescription(game),
                CommunityBoardType.Game,
                game.Id,
                game.Name,
                game.Slug,
                game.BackgroundImageUrl,
                count,
                lastActivity,
                "from-accent/30 via-info/10 to-transparent"));
        }

        var spotlightTopics = latestTopics.Concat(trendingTopics).ToList();
        var authorIds = spotlightTopics.Select(p => p.AuthorId).Distinct().ToList();
        var gameIds = spotlightTopics
            .Select(p => p.GameId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList()!;

        var userMap = (await users.Find(u => authorIds.Contains(u.Id)).ToListAsync(ct)).ToDictionary(u => u.Id);
        var profileMap = (await profiles.Find(p => authorIds.Contains(p.UserId)).ToListAsync(ct)).ToDictionary(p => p.UserId);
        var gameMap = (await games.Find(g => gameIds.Contains(g.Id)).ToListAsync(ct)).ToDictionary(g => g.Id);

        var votedTopicIds = new HashSet<string>();
        if (_currentUser.IsAuthenticated && !string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            var topicIds = spotlightTopics.Select(p => p.Id).Distinct().ToList();
            var userVotes = await votes.Find(v =>
                    v.UserId == _currentUser.UserId &&
                    v.TargetType == ContentVoteTargetType.CommunityPost &&
                    topicIds.Contains(v.TargetId))
                .ToListAsync(ct);
            votedTopicIds = userVotes.Select(v => v.TargetId).ToHashSet();
        }

        return Result<CommunityBoardsOverviewResponse>.Success(
            new CommunityBoardsOverviewResponse(
                boards,
                trendingTopics.Select(topic => CommunityDtoMapper.ToPostResponse(topic, userMap, profileMap, gameMap, votedTopicIds)).ToList(),
                latestTopics.Select(topic => CommunityDtoMapper.ToPostResponse(topic, userMap, profileMap, gameMap, votedTopicIds)).ToList()));
    }

    private static string BuildGameBoardDescription(Game game)
    {
        var genre = game.Genres.FirstOrDefault();
        return genre is null
            ? "Strategy, meta shifts, squad requests, and patch reactions for this game."
            : $"{genre} tactics, player requests, patch reactions, and community intel for this game.";
    }
}
