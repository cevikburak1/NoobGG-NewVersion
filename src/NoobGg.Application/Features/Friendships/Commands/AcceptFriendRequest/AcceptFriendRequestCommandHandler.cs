using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Friendships.Commands.AcceptFriendRequest;

public class AcceptFriendRequestCommandHandler : IRequestHandler<AcceptFriendRequestCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notificationService;

    public AcceptFriendRequestCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        INotificationService notificationService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result> Handle(AcceptFriendRequestCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var myId = _currentUser.UserId;
        var friendships = _mongoContext.GetCollection<Friendship>(CollectionNames.Friendships);

        var friendship = await friendships.Find(f => f.Id == request.FriendshipId).FirstOrDefaultAsync(ct);
        if (friendship is null)
            return Result.Fail("Friend request not found", 404);

        if (friendship.AddresseeId != myId)
            return Result.Fail("You can only accept requests sent to you", 403);

        if (friendship.Status != FriendshipStatus.Pending)
            return Result.Fail("This request is no longer pending");

        var update = Builders<Friendship>.Update
            .Set(f => f.Status, FriendshipStatus.Accepted)
            .Set(f => f.RespondedAt, DateTime.UtcNow)
            .Set(f => f.UpdatedAt, DateTime.UtcNow);

        await friendships.UpdateOneAsync(f => f.Id == request.FriendshipId, update, cancellationToken: ct);

        await _notificationService.CreateAsync(
            friendship.RequesterId,
            NotificationType.FriendAccepted,
            "Friend Request Accepted",
            $"{_currentUser.Username} accepted your friend request",
            new Dictionary<string, string> { ["fromUserId"] = myId },
            ct);

        await _notificationService.SendFriendListChangedAsync(myId, friendship.RequesterId, ct);

        return Result.Success();
    }
}
