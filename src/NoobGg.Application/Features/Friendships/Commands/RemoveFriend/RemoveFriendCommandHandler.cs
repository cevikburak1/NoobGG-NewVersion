using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Friendships.Commands.RemoveFriend;

public class RemoveFriendCommandHandler : IRequestHandler<RemoveFriendCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notificationService;

    public RemoveFriendCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        INotificationService notificationService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result> Handle(RemoveFriendCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var myId = _currentUser.UserId;
        var friendships = _mongoContext.GetCollection<Friendship>(CollectionNames.Friendships);

        var filter = Builders<Friendship>.Filter.And(
            Builders<Friendship>.Filter.Eq(f => f.Status, FriendshipStatus.Accepted),
            Builders<Friendship>.Filter.Or(
                Builders<Friendship>.Filter.And(
                    Builders<Friendship>.Filter.Eq(f => f.RequesterId, myId),
                    Builders<Friendship>.Filter.Eq(f => f.AddresseeId, request.UserId)),
                Builders<Friendship>.Filter.And(
                    Builders<Friendship>.Filter.Eq(f => f.RequesterId, request.UserId),
                    Builders<Friendship>.Filter.Eq(f => f.AddresseeId, myId))
            ));

        var result = await friendships.DeleteOneAsync(filter, ct);
        if (result.DeletedCount == 0)
            return Result.Fail("Friendship not found", 404);

        await _notificationService.SendFriendListChangedAsync(myId, request.UserId, ct);

        return Result.Success();
    }
}
