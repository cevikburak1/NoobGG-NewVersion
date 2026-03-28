using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Friendships.Commands.SendFriendRequest;

public class SendFriendRequestCommandHandler : IRequestHandler<SendFriendRequestCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notificationService;

    public SendFriendRequestCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        INotificationService notificationService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result> Handle(SendFriendRequestCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var myId = _currentUser.UserId;

        if (myId == request.AddresseeId)
            return Result.Fail("You cannot send a friend request to yourself");

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var targetExists = await users.Find(u => u.Id == request.AddresseeId).AnyAsync(ct);
        if (!targetExists)
            return Result.Fail("User not found", 404);

        var blocks = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);
        var blockExists = await blocks.Find(b =>
            (b.BlockerId == myId && b.BlockedUserId == request.AddresseeId) ||
            (b.BlockerId == request.AddresseeId && b.BlockedUserId == myId)
        ).AnyAsync(ct);

        if (blockExists)
            return Result.Fail("Cannot send friend request to this user");

        var friendships = _mongoContext.GetCollection<Friendship>(CollectionNames.Friendships);

        var existing = await friendships.Find(f =>
            (f.RequesterId == myId && f.AddresseeId == request.AddresseeId) ||
            (f.RequesterId == request.AddresseeId && f.AddresseeId == myId)
        ).FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            if (existing.Status == FriendshipStatus.Accepted)
                return Result.Fail("You are already friends with this user");

            if (existing.Status == FriendshipStatus.Pending)
                return Result.Fail("A friend request already exists between you and this user");

            if (existing.Status == FriendshipStatus.Rejected)
            {
                await friendships.DeleteOneAsync(f => f.Id == existing.Id, ct);
            }
        }

        var friendship = new Friendship
        {
            RequesterId = myId,
            AddresseeId = request.AddresseeId,
            Status = FriendshipStatus.Pending
        };

        try
        {
            await friendships.InsertOneAsync(friendship, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return Result.Fail("A friend request already exists");
        }

        await _notificationService.CreateAsync(
            request.AddresseeId,
            NotificationType.FriendRequest,
            "Friend Request",
            $"{_currentUser.Username} sent you a friend request",
            new Dictionary<string, string> { ["fromUserId"] = myId },
            ct);

        await _notificationService.SendFriendListChangedAsync(myId, request.AddresseeId, ct);

        return Result.Success();
    }
}
