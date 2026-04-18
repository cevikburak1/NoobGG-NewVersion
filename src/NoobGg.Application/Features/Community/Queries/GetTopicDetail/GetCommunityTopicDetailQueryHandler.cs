using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Community.Queries.GetTopicDetail;

public class GetCommunityTopicDetailQueryHandler
    : IRequestHandler<GetCommunityTopicDetailQuery, Result<CommunityTopicDetailResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetCommunityTopicDetailQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<CommunityTopicDetailResponse>> Handle(GetCommunityTopicDetailQuery request, CancellationToken ct)
    {
        var posts = _mongoContext.GetCollection<CommunityPost>(CollectionNames.CommunityPosts);
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var votes = _mongoContext.GetCollection<ContentVote>(CollectionNames.ContentVotes);
        var boards = _mongoContext.GetCollection<CommunityBoard>(CollectionNames.CommunityBoards);

        var topic = await posts.Find(p => p.Id == request.TopicId).FirstOrDefaultAsync(ct);
        if (topic is null)
            return Result<CommunityTopicDetailResponse>.NotFound("Topic not found");

        var relatedFilter = !string.IsNullOrWhiteSpace(topic.BoardId)
            ? Builders<CommunityPost>.Filter.And(
                Builders<CommunityPost>.Filter.Eq(p => p.BoardId, topic.BoardId),
                Builders<CommunityPost>.Filter.Ne(p => p.Id, topic.Id))
            : topic.BoardType == CommunityBoardType.General
                ? Builders<CommunityPost>.Filter.And(
                    Builders<CommunityPost>.Filter.Eq(p => p.BoardType, CommunityBoardType.General),
                    Builders<CommunityPost>.Filter.Ne(p => p.Id, topic.Id))
                : Builders<CommunityPost>.Filter.And(
                    Builders<CommunityPost>.Filter.Eq(p => p.BoardType, CommunityBoardType.Game),
                    Builders<CommunityPost>.Filter.Eq(p => p.GameId, topic.GameId),
                    Builders<CommunityPost>.Filter.Ne(p => p.Id, topic.Id));

        var relatedTopics = await posts.Find(relatedFilter)
            .SortByDescending(p => p.LastActivityAt)
            .Limit(4)
            .ToListAsync(ct);

        var allTopics = new List<CommunityPost> { topic };
        allTopics.AddRange(relatedTopics);

        var authorIds = allTopics.Select(p => p.AuthorId).Distinct().ToList();
        var gameIds = allTopics
            .Select(p => p.GameId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList()!;

        var userMap = (await users.Find(u => authorIds.Contains(u.Id)).ToListAsync(ct)).ToDictionary(u => u.Id);
        var profileMap = (await profiles.Find(p => authorIds.Contains(p.UserId)).ToListAsync(ct)).ToDictionary(p => p.UserId);
        var gameMap = (await games.Find(g => gameIds.Contains(g.Id)).ToListAsync(ct)).ToDictionary(g => g.Id);
        var boardIds = allTopics
            .Select(p => p.BoardId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList()!;
        var boardMap = (await boards.Find(b => boardIds.Contains(b.Id)).ToListAsync(ct)).ToDictionary(b => b.Id);

        var votedTopicIds = new HashSet<string>();
        if (_currentUser.IsAuthenticated && !string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            var topicIds = allTopics.Select(p => p.Id).ToList();
            var userVotes = await votes.Find(v =>
                    v.UserId == _currentUser.UserId &&
                    v.TargetType == ContentVoteTargetType.CommunityPost &&
                    topicIds.Contains(v.TargetId))
                .ToListAsync(ct);
            votedTopicIds = userVotes.Select(v => v.TargetId).ToHashSet();
        }

        var topicResponse = CommunityDtoMapper.ToPostResponse(topic, boardMap, userMap, profileMap, gameMap, votedTopicIds);
        var relatedResponses = relatedTopics
            .Select(item => CommunityDtoMapper.ToPostResponse(item, boardMap, userMap, profileMap, gameMap, votedTopicIds))
            .ToList();

        return Result<CommunityTopicDetailResponse>.Success(
            new CommunityTopicDetailResponse(topicResponse, relatedResponses));
    }
}
