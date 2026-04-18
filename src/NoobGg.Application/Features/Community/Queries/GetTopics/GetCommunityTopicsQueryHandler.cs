using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Community.Queries.GetTopics;

public class GetCommunityTopicsQueryHandler
    : IRequestHandler<GetCommunityTopicsQuery, Result<CommunityTopicListResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetCommunityTopicsQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<CommunityTopicListResponse>> Handle(GetCommunityTopicsQuery request, CancellationToken ct)
    {
        var boardsCollection = _mongoContext.GetCollection<CommunityBoard>(CollectionNames.CommunityBoards);
        var posts = _mongoContext.GetCollection<CommunityPost>(CollectionNames.CommunityPosts);
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var votes = _mongoContext.GetCollection<ContentVote>(CollectionNames.ContentVotes);

        var board = await ResolveBoardAsync(boardsCollection, request, ct);
        if (board is null)
            return Result<CommunityTopicListResponse>.NotFound("Board not found");
        var filter = BuildBoardFilter(board);

        var totalCount = (int)await posts.CountDocumentsAsync(filter, cancellationToken: ct);
        var skip = (request.Page - 1) * request.PageSize;

        var query = posts.Find(filter);
        query = request.Sort.ToLowerInvariant() switch
        {
            "top" or "mostliked" => query.SortByDescending(p => p.UpvoteCount).ThenByDescending(p => p.LastActivityAt),
            "hot" or "mostcommented" => query.SortByDescending(p => p.CommentCount).ThenByDescending(p => p.LastActivityAt),
            _ => query.SortByDescending(p => p.IsPinned).ThenByDescending(p => p.LastActivityAt),
        };

        var topicList = await query
            .Skip(skip)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        if (topicList.Count == 0)
        {
            return Result<CommunityTopicListResponse>.Success(
                new CommunityTopicListResponse([], totalCount, request.Page, request.PageSize, false, request.Page > 1));
        }

        var authorIds = topicList.Select(p => p.AuthorId).Distinct().ToList();
        var gameIds = topicList
            .Select(p => p.GameId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList()!;

        var userMap = (await users.Find(u => authorIds.Contains(u.Id)).ToListAsync(ct)).ToDictionary(u => u.Id);
        var profileMap = (await profiles.Find(p => authorIds.Contains(p.UserId)).ToListAsync(ct)).ToDictionary(p => p.UserId);
        var gameMap = (await games.Find(g => gameIds.Contains(g.Id)).ToListAsync(ct)).ToDictionary(g => g.Id);
        var boardMap = new Dictionary<string, CommunityBoard> { [board.Id] = board };

        var votedTopicIds = new HashSet<string>();
        if (_currentUser.IsAuthenticated && !string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            var topicIds = topicList.Select(p => p.Id).ToList();
            var userVotes = await votes.Find(v =>
                    v.UserId == _currentUser.UserId &&
                    v.TargetType == ContentVoteTargetType.CommunityPost &&
                    topicIds.Contains(v.TargetId))
                .ToListAsync(ct);
            votedTopicIds = userVotes.Select(v => v.TargetId).ToHashSet();
        }

        var response = topicList
            .Select(topic => CommunityDtoMapper.ToPostResponse(topic, boardMap, userMap, profileMap, gameMap, votedTopicIds))
            .ToList();

        return Result<CommunityTopicListResponse>.Success(
            new CommunityTopicListResponse(
                response,
                totalCount,
                request.Page,
                request.PageSize,
                skip + request.PageSize < totalCount,
                request.Page > 1));
    }

    private static async Task<CommunityBoard?> ResolveBoardAsync(
        IMongoCollection<CommunityBoard> boards,
        GetCommunityTopicsQuery request,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.BoardId))
            return await boards.Find(b => b.Id == request.BoardId && !b.IsArchived).FirstOrDefaultAsync(ct);
        return await boards.Find(b => b.Slug == request.BoardSlug && !b.IsArchived).FirstOrDefaultAsync(ct);
    }

    private static FilterDefinition<CommunityPost> BuildBoardFilter(CommunityBoard board)
    {
        var boardIdFilter = Builders<CommunityPost>.Filter.Eq(p => p.BoardId, board.Id);
        if (!string.IsNullOrWhiteSpace(board.GameId))
        {
            var legacyGameFilter = Builders<CommunityPost>.Filter.And(
                Builders<CommunityPost>.Filter.Eq(p => p.BoardType, CommunityBoardType.Game),
                Builders<CommunityPost>.Filter.Eq(p => p.GameId, board.GameId));
            return Builders<CommunityPost>.Filter.Or(boardIdFilter, legacyGameFilter);
        }

        var legacyGeneralFilter = Builders<CommunityPost>.Filter.Eq(p => p.BoardType, CommunityBoardType.General);
        return Builders<CommunityPost>.Filter.Or(boardIdFilter, legacyGeneralFilter);
    }
}
