using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Guides.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guides.Queries.GetGuides;

public class GetGuidesQueryHandler : IRequestHandler<GetGuidesQuery, Result<GuideListResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetGuidesQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<GuideListResponse>> Handle(GetGuidesQuery request, CancellationToken ct)
    {
        var guides = _mongoContext.GetCollection<Guide>(CollectionNames.Guides);

        var filter = Builders<Guide>.Filter.Eq(g => g.Status, GuideStatus.Published);

        if (!string.IsNullOrWhiteSpace(request.GameId))
            filter = Builders<Guide>.Filter.And(filter, Builders<Guide>.Filter.Eq(g => g.GameId, request.GameId));

        var totalCount = await guides.CountDocumentsAsync(filter, cancellationToken: ct);

        var sort = request.SortBy?.ToLowerInvariant() == "popular"
            ? Builders<Guide>.Sort.Descending(g => g.UpvoteCount)
            : Builders<Guide>.Sort.Descending(g => g.CreatedAt);

        var skip = (request.Page - 1) * request.PageSize;

        var guideList = await guides
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(request.PageSize)
            .ToListAsync(ct);

        if (guideList.Count == 0)
        {
            return Result<GuideListResponse>.Success(
                new GuideListResponse([], (int)totalCount, false));
        }

        var authorIds = guideList.Select(g => g.AuthorId).Distinct().ToList();

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);

        var authorUsers = await users
            .Find(Builders<User>.Filter.In(u => u.Id, authorIds))
            .ToListAsync(ct);

        var authorProfiles = await profiles
            .Find(Builders<UserProfile>.Filter.In(p => p.UserId, authorIds))
            .ToListAsync(ct);

        var userMap = authorUsers.ToDictionary(u => u.Id, u => u.Username);
        var avatarMap = authorProfiles.ToDictionary(p => p.UserId, p => p.AvatarUrl);

        var userVotedGuideIds = new HashSet<string>();

        if (_currentUser.IsAuthenticated && _currentUser.UserId is not null)
        {
            var votes = _mongoContext.GetCollection<ContentVote>(CollectionNames.ContentVotes);
            var guideIds = guideList.Select(g => g.Id).ToList();

            var userVotes = await votes
                .Find(Builders<ContentVote>.Filter.And(
                    Builders<ContentVote>.Filter.Eq(v => v.UserId, _currentUser.UserId),
                    Builders<ContentVote>.Filter.Eq(v => v.TargetType, ContentVoteTargetType.Guide),
                    Builders<ContentVote>.Filter.In(v => v.TargetId, guideIds)))
                .ToListAsync(ct);

            foreach (var v in userVotes)
                userVotedGuideIds.Add(v.TargetId);
        }

        var items = guideList.Select(g => new GuideListItemResponse(
            Id: g.Id,
            Title: g.Title,
            AuthorId: g.AuthorId,
            AuthorUsername: userMap.GetValueOrDefault(g.AuthorId, "Unknown"),
            AuthorAvatarUrl: avatarMap.GetValueOrDefault(g.AuthorId),
            GameId: g.GameId,
            CoverImageUrl: g.CoverImageUrl,
            Tags: g.Tags,
            UpvoteCount: g.UpvoteCount,
            ViewCount: g.ViewCount,
            HasUpvoted: userVotedGuideIds.Contains(g.Id),
            CreatedAt: g.CreatedAt)).ToList();

        var hasMore = skip + request.PageSize < totalCount;

        return Result<GuideListResponse>.Success(
            new GuideListResponse(items, (int)totalCount, hasMore));
    }
}
