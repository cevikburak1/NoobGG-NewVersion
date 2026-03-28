using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Friendships.Commands.RejectFriendRequest;

public class RejectFriendRequestCommandHandler : IRequestHandler<RejectFriendRequestCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notificationService;

    public RejectFriendRequestCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        INotificationService notificationService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result> Handle(RejectFriendRequestCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var myId = _currentUser.UserId;
        var friendships = _mongoContext.GetCollection<Friendship>(CollectionNames.Friendships);

        var friendship = await friendships.Find(f => f.Id == request.FriendshipId).FirstOrDefaultAsync(ct);
        if (friendship is null)
            return Result.Fail("Friend request not found", 404);

        if (friendship.AddresseeId != myId && friendship.RequesterId != myId)
            return Result.Fail("You are not part of this friend request", 403);

        if (friendship.Status != FriendshipStatus.Pending)
            return Result.Fail("This request is no longer pending");

        var update = Builders<Friendship>.Update
            .Set(f => f.Status, FriendshipStatus.Rejected)
            .Set(f => f.RespondedAt, DateTime.UtcNow)
            .Set(f => f.UpdatedAt, DateTime.UtcNow);

        await friendships.UpdateOneAsync(f => f.Id == request.FriendshipId, update, cancellationToken: ct);

        var otherUserId = friendship.RequesterId == myId ? friendship.AddresseeId : friendship.RequesterId;
        await _notificationService.SendFriendListChangedAsync(myId, otherUserId, ct);

        return Result.Success();
    }
}
