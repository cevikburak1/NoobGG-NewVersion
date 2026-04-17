using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Community.Commands.ToggleVote;

public class ToggleContentVoteCommandHandler
    : IRequestHandler<ToggleContentVoteCommand, Result<bool>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public ToggleContentVoteCommandHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(ToggleContentVoteCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<bool>.Unauthorized();

        var userId = _currentUser.UserId;
        var votes = _mongoContext.GetCollection<ContentVote>(CollectionNames.ContentVotes);

        var existingVote = await votes.Find(v =>
                v.UserId == userId &&
                v.TargetId == request.TargetId &&
                v.TargetType == request.TargetType)
            .FirstOrDefaultAsync(ct);

        if (existingVote is not null)
        {
            await votes.DeleteOneAsync(v => v.Id == existingVote.Id, ct);
            await UpdateUpvoteCountAsync(request.TargetId, request.TargetType, -1, ct);
            return Result<bool>.Success(false);
        }

        var vote = new ContentVote
        {
            UserId = userId,
            TargetId = request.TargetId,
            TargetType = request.TargetType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await votes.InsertOneAsync(vote, cancellationToken: ct);
        await UpdateUpvoteCountAsync(request.TargetId, request.TargetType, 1, ct);
        return Result<bool>.Success(true);
    }

    private async Task UpdateUpvoteCountAsync(
        string targetId, ContentVoteTargetType targetType, int increment, CancellationToken ct)
    {
        switch (targetType)
        {
            case ContentVoteTargetType.CommunityPost:
                var posts = _mongoContext.GetCollection<CommunityPost>(CollectionNames.CommunityPosts);
                await posts.UpdateOneAsync(
                    p => p.Id == targetId,
                    Builders<CommunityPost>.Update
                        .Inc(p => p.UpvoteCount, increment)
                        .Set(p => p.UpdatedAt, DateTime.UtcNow),
                    cancellationToken: ct);
                break;

            case ContentVoteTargetType.CommunityComment:
                var comments = _mongoContext.GetCollection<CommunityComment>(CollectionNames.CommunityComments);
                await comments.UpdateOneAsync(
                    c => c.Id == targetId,
                    Builders<CommunityComment>.Update
                        .Inc(c => c.UpvoteCount, increment)
                        .Set(c => c.UpdatedAt, DateTime.UtcNow),
                    cancellationToken: ct);
                break;
        }
    }
}
