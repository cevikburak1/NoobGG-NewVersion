using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Guides.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guides.Commands.CreateGuide;

public class CreateGuideCommandHandler : IRequestHandler<CreateGuideCommand, Result<GuideDetailResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public CreateGuideCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<GuideDetailResponse>> Handle(CreateGuideCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<GuideDetailResponse>.Unauthorized();

        var userId = _currentUser.UserId;
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);

        var game = await games.Find(g => g.Id == request.GameId && g.IsActive).FirstOrDefaultAsync(ct);
        if (game is null)
            return Result<GuideDetailResponse>.NotFound("Game not found or inactive");

        var guide = new Guide
        {
            AuthorId = userId,
            GameId = request.GameId,
            Title = request.Title,
            Content = request.Content,
            CoverImageUrl = request.CoverImageUrl,
            Tags = request.Tags,
            Status = GuideStatus.Published,
            UpvoteCount = 0,
            ViewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var guides = _mongoContext.GetCollection<Guide>(CollectionNames.Guides);
        await guides.InsertOneAsync(guide, cancellationToken: ct);

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);

        var author = await users.Find(u => u.Id == userId).FirstOrDefaultAsync(ct);
        var authorProfile = await profiles.Find(p => p.UserId == userId).FirstOrDefaultAsync(ct);

        var response = new GuideDetailResponse(
            Id: guide.Id,
            Title: guide.Title,
            Content: guide.Content,
            AuthorId: guide.AuthorId,
            AuthorUsername: author?.Username ?? _currentUser.Username ?? "Unknown",
            AuthorAvatarUrl: authorProfile?.AvatarUrl,
            GameId: guide.GameId,
            CoverImageUrl: guide.CoverImageUrl,
            Tags: guide.Tags,
            Status: guide.Status.ToString(),
            UpvoteCount: guide.UpvoteCount,
            ViewCount: guide.ViewCount,
            HasUpvoted: false,
            CreatedAt: guide.CreatedAt,
            UpdatedAt: guide.UpdatedAt);

        return Result<GuideDetailResponse>.Created(response);
    }
}
