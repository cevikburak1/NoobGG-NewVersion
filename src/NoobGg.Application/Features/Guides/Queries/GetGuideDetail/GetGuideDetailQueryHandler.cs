using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Guides.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guides.Queries.GetGuideDetail;

public class GetGuideDetailQueryHandler : IRequestHandler<GetGuideDetailQuery, Result<GuideDetailResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetGuideDetailQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<GuideDetailResponse>> Handle(GetGuideDetailQuery request, CancellationToken ct)
    {
        var guides = _mongoContext.GetCollection<Guide>(CollectionNames.Guides);

        var guide = await guides.Find(g => g.Id == request.GuideId).FirstOrDefaultAsync(ct);
        if (guide is null)
            return Result<GuideDetailResponse>.NotFound("Guide not found");

        await guides.UpdateOneAsync(
            Builders<Guide>.Filter.Eq(g => g.Id, guide.Id),
            Builders<Guide>.Update.Inc(g => g.ViewCount, 1),
            cancellationToken: ct);

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);

        var author = await users.Find(u => u.Id == guide.AuthorId).FirstOrDefaultAsync(ct);
        var authorProfile = await profiles.Find(p => p.UserId == guide.AuthorId).FirstOrDefaultAsync(ct);

        var hasUpvoted = false;
        if (_currentUser.IsAuthenticated && _currentUser.UserId is not null)
        {
            var votes = _mongoContext.GetCollection<ContentVote>(CollectionNames.ContentVotes);
            hasUpvoted = await votes.Find(
                Builders<ContentVote>.Filter.And(
                    Builders<ContentVote>.Filter.Eq(v => v.UserId, _currentUser.UserId),
                    Builders<ContentVote>.Filter.Eq(v => v.TargetId, guide.Id),
                    Builders<ContentVote>.Filter.Eq(v => v.TargetType, ContentVoteTargetType.Guide)))
                .AnyAsync(ct);
        }

        var response = new GuideDetailResponse(
            Id: guide.Id,
            Title: guide.Title,
            Content: guide.Content,
            AuthorId: guide.AuthorId,
            AuthorUsername: author?.Username ?? "Unknown",
            AuthorAvatarUrl: authorProfile?.AvatarUrl,
            GameId: guide.GameId,
            CoverImageUrl: guide.CoverImageUrl,
            Tags: guide.Tags,
            Status: guide.Status.ToString(),
            UpvoteCount: guide.UpvoteCount + 1,
            ViewCount: guide.ViewCount + 1,
            HasUpvoted: hasUpvoted,
            CreatedAt: guide.CreatedAt,
            UpdatedAt: guide.UpdatedAt);

        return Result<GuideDetailResponse>.Success(response);
    }
}
