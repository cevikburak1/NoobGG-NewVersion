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
        var boardsCollection = _mongoContext.GetCollection<CommunityBoard>(CollectionNames.CommunityBoards);
        var posts = _mongoContext.GetCollection<CommunityPost>(CollectionNames.CommunityPosts);
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var votes = _mongoContext.GetCollection<ContentVote>(CollectionNames.ContentVotes);

        var allBoards = await boardsCollection.Find(b => !b.IsArchived).ToListAsync(ct);
        var boardCategories = allBoards
            .Select(b => b.Category.Trim())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();

        IEnumerable<CommunityBoard> filteredBoards = allBoards;
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            filteredBoards = filteredBoards.Where(board =>
                string.Equals(board.Category, request.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            filteredBoards = filteredBoards.Where(board =>
                board.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                board.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var boardStats = await posts.Aggregate()
            .Group(
                p => p.BoardId ?? "general",
                g => new BoardStatsProjection(
                    g.Key,
                    g.Count(),
                    g.Max(item => item.LastActivityAt)))
            .ToListAsync(ct);
        var statMap = boardStats.ToDictionary(item => item.BoardId, item => item);

        filteredBoards = (request.Sort ?? "activity").ToLowerInvariant() switch
        {
            "name" => filteredBoards.OrderBy(board => board.Name),
            "popular" => filteredBoards.OrderByDescending(board => statMap.GetValueOrDefault(board.Id)?.TopicCount ?? 0),
            _ => filteredBoards.OrderByDescending(board => statMap.GetValueOrDefault(board.Id)?.LastActivityAt ?? DateTime.MinValue)
        };

        var safePage = Math.Clamp(request.Page, 1, 2000);
        var safePageSize = Math.Clamp(request.PageSize, 1, 100);
        var boardsPage = filteredBoards
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        var boardIds = boardsPage.Select(board => board.Id).ToHashSet();
        var topicFilter = boardIds.Count == 0
            ? FilterDefinition<CommunityPost>.Empty
            : Builders<CommunityPost>.Filter.In(p => p.BoardId, boardIds);

        var latestTopics = await posts.Find(topicFilter)
            .SortByDescending(p => p.LastActivityAt)
            .Limit(6)
            .ToListAsync(ct);

        var trendingTopics = await posts.Find(topicFilter)
            .SortByDescending(p => p.CommentCount)
            .ThenByDescending(p => p.UpvoteCount)
            .ThenByDescending(p => p.LastActivityAt)
            .Limit(6)
            .ToListAsync(ct);

        var mostLikedTopics = await posts.Find(topicFilter)
            .SortByDescending(p => p.UpvoteCount)
            .ThenByDescending(p => p.CommentCount)
            .ThenByDescending(p => p.LastActivityAt)
            .Limit(6)
            .ToListAsync(ct);

        var topDiscussedTopics = await posts.Find(topicFilter)
            .SortByDescending(p => p.CommentCount)
            .ThenByDescending(p => p.LastActivityAt)
            .Limit(6)
            .ToListAsync(ct);

        var spotlightTopics = latestTopics
            .Concat(trendingTopics)
            .Concat(topDiscussedTopics)
            .Concat(mostLikedTopics)
            .DistinctBy(topic => topic.Id)
            .ToList();
        var authorIds = spotlightTopics.Select(p => p.AuthorId).Distinct().ToList();
        var gameIds = spotlightTopics
            .Select(p => p.GameId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList()!;
        var boardIdLookup = boardsPage.Select(board => board.Id).Concat(spotlightTopics
            .Select(post => post.BoardId)
            .Where(boardId => !string.IsNullOrWhiteSpace(boardId))!)
            .Distinct()
            .ToList();

        var userMap = (await users.Find(u => authorIds.Contains(u.Id)).ToListAsync(ct)).ToDictionary(u => u.Id);
        var profileMap = (await profiles.Find(p => authorIds.Contains(p.UserId)).ToListAsync(ct)).ToDictionary(p => p.UserId);
        var gameMap = (await games.Find(g => gameIds.Contains(g.Id)).ToListAsync(ct)).ToDictionary(g => g.Id);
        var boardMap = (await boardsCollection.Find(b => boardIdLookup.Contains(b.Id)).ToListAsync(ct)).ToDictionary(b => b.Id);

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
                boardsPage.Select(board =>
                {
                    Game? game = null;
                    if (!string.IsNullOrWhiteSpace(board.GameId))
                        gameMap.TryGetValue(board.GameId, out game);
                    statMap.TryGetValue(board.Id, out var stats);
                    return new CommunityBoardResponse(
                        board.Id,
                        board.Slug,
                        board.Name,
                        board.Description,
                        board.Category,
                        game is null ? CommunityBoardType.General : CommunityBoardType.Game,
                        game?.Id,
                        game?.Name,
                        game?.Slug,
                        board.CoverImageUrl ?? game?.BackgroundImageUrl,
                        stats?.TopicCount ?? 0,
                        stats?.LastActivityAt,
                        board.Accent);
                }).ToList(),
                boardCategories,
                trendingTopics.Select(topic => CommunityDtoMapper.ToPostResponse(topic, boardMap, userMap, profileMap, gameMap, votedTopicIds)).ToList(),
                latestTopics.Select(topic => CommunityDtoMapper.ToPostResponse(topic, boardMap, userMap, profileMap, gameMap, votedTopicIds)).ToList(),
                topDiscussedTopics.Select(topic => CommunityDtoMapper.ToPostResponse(topic, boardMap, userMap, profileMap, gameMap, votedTopicIds)).ToList(),
                mostLikedTopics.Select(topic => CommunityDtoMapper.ToPostResponse(topic, boardMap, userMap, profileMap, gameMap, votedTopicIds)).ToList()));
    }

    private sealed record BoardStatsProjection(string BoardId, int TopicCount, DateTime? LastActivityAt);
}
