using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Community.Queries.GetFeed;

public class GetCommunityFeedQueryHandler
    : IRequestHandler<GetCommunityFeedQuery, Result<CommunityFeedResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetCommunityFeedQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<CommunityFeedResponse>> Handle(
        GetCommunityFeedQuery request, CancellationToken ct)
    {
        var posts = _mongoContext.GetCollection<CommunityPost>(CollectionNames.CommunityPosts);
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var votes = _mongoContext.GetCollection<ContentVote>(CollectionNames.ContentVotes);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);

        var filter = Builders<CommunityPost>.Filter.And(
            Builders<CommunityPost>.Filter.Eq(p => p.BoardType, CommunityBoardType.Game),
            Builders<CommunityPost>.Filter.Eq(p => p.GameId, request.GameId));
        var totalCount = (int)await posts.CountDocumentsAsync(filter, cancellationToken: ct);

        var skip = (request.Page - 1) * request.PageSize;
        var postList = await posts.Find(filter)
            .SortByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        if (postList.Count == 0)
        {
            return Result<CommunityFeedResponse>.Success(
                new CommunityFeedResponse([], totalCount, false));
        }

        var authorIds = postList.Select(p => p.AuthorId).Distinct().ToList();
        var gameIds = postList
            .Select(p => p.GameId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList()!;
        var userMap = (await users.Find(u => authorIds.Contains(u.Id)).ToListAsync(ct))
            .ToDictionary(u => u.Id);
        var profileMap = (await profiles.Find(p => authorIds.Contains(p.UserId)).ToListAsync(ct))
            .ToDictionary(p => p.UserId);
        var gameMap = (await games.Find(g => gameIds.Contains(g.Id)).ToListAsync(ct))
            .ToDictionary(g => g.Id);

        var currentUserId = _currentUser.IsAuthenticated ? _currentUser.UserId : null;
        var votedPostIds = new HashSet<string>();

        if (currentUserId is not null)
        {
            var postIds = postList.Select(p => p.Id).ToList();
            var userVotes = await votes.Find(v =>
                    v.UserId == currentUserId &&
                    v.TargetType == ContentVoteTargetType.CommunityPost &&
                    postIds.Contains(v.TargetId))
                .ToListAsync(ct);
            votedPostIds = userVotes.Select(v => v.TargetId).ToHashSet();
        }

        var responseList = postList
            .Select(p => CommunityDtoMapper.ToPostResponse(p, userMap, profileMap, gameMap, votedPostIds))
            .ToList();

        var hasMore = skip + request.PageSize < totalCount;
        return Result<CommunityFeedResponse>.Success(
            new CommunityFeedResponse(responseList, totalCount, hasMore));
    }
}
