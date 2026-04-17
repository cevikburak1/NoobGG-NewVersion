using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Community.Queries.GetComments;

public class GetPostCommentsQueryHandler
    : IRequestHandler<GetPostCommentsQuery, Result<CommunityCommentsResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetPostCommentsQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<CommunityCommentsResponse>> Handle(
        GetPostCommentsQuery request, CancellationToken ct)
    {
        var comments = _mongoContext.GetCollection<CommunityComment>(CollectionNames.CommunityComments);
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var votes = _mongoContext.GetCollection<ContentVote>(CollectionNames.ContentVotes);

        var totalCount = (int)await comments.CountDocumentsAsync(c => c.PostId == request.PostId, cancellationToken: ct);
        var skip = (request.Page - 1) * request.PageSize;
        var commentList = await comments.Find(c => c.PostId == request.PostId)
            .SortBy(c => c.CreatedAt)
            .Skip(skip)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        if (commentList.Count == 0)
            return Result<CommunityCommentsResponse>.Success(
                new CommunityCommentsResponse([], totalCount, false, request.Page, request.PageSize));

        var authorIds = commentList.Select(c => c.AuthorId).Distinct().ToList();
        var userMap = (await users.Find(u => authorIds.Contains(u.Id)).ToListAsync(ct))
            .ToDictionary(u => u.Id);
        var profileMap = (await profiles.Find(p => authorIds.Contains(p.UserId)).ToListAsync(ct))
            .ToDictionary(p => p.UserId);

        var currentUserId = _currentUser.IsAuthenticated ? _currentUser.UserId : null;
        var votedCommentIds = new HashSet<string>();

        if (currentUserId is not null)
        {
            var commentIds = commentList.Select(c => c.Id).ToList();
            var userVotes = await votes.Find(v =>
                    v.UserId == currentUserId &&
                    v.TargetType == ContentVoteTargetType.CommunityComment &&
                    commentIds.Contains(v.TargetId))
                .ToListAsync(ct);
            votedCommentIds = userVotes.Select(v => v.TargetId).ToHashSet();
        }

        var responseList = commentList.Select(c =>
        {
            userMap.TryGetValue(c.AuthorId, out var author);
            profileMap.TryGetValue(c.AuthorId, out var profile);

            return new CommunityCommentResponse(
                c.Id,
                c.AuthorId,
                author?.Username ?? "Unknown",
                profile?.AvatarUrl,
                c.Content,
                c.UpvoteCount,
                votedCommentIds.Contains(c.Id),
                c.CreatedAt);
        }).ToList();

        var hasMore = skip + request.PageSize < totalCount;
        return Result<CommunityCommentsResponse>.Success(
            new CommunityCommentsResponse(responseList, totalCount, hasMore, request.Page, request.PageSize));
    }
}
