using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Friendships.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Friendships.Queries.GetMyFriends;

public class GetMyFriendsQueryHandler : IRequestHandler<GetMyFriendsQuery, Result<List<FriendshipResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetMyFriendsQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<List<FriendshipResponse>>> Handle(GetMyFriendsQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<List<FriendshipResponse>>.Unauthorized();

        var myId = _currentUser.UserId;
        var friendships = _mongoContext.GetCollection<Friendship>(CollectionNames.Friendships);
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);

        var accepted = await friendships.Find(f =>
            f.Status == FriendshipStatus.Accepted &&
            (f.RequesterId == myId || f.AddresseeId == myId)
        ).ToListAsync(ct);

        if (accepted.Count == 0)
            return Result<List<FriendshipResponse>>.Success([]);

        var friendUserIds = accepted
            .Select(f => f.RequesterId == myId ? f.AddresseeId : f.RequesterId)
            .ToList();

        var userList = await users.Find(Builders<User>.Filter.In(u => u.Id, friendUserIds)).ToListAsync(ct);
        var userMap = userList.ToDictionary(u => u.Id);

        var profileList = await profiles.Find(Builders<UserProfile>.Filter.In(p => p.UserId, friendUserIds)).ToListAsync(ct);
        var profileMap = profileList.ToDictionary(p => p.UserId);

        var result = accepted.Select(f =>
        {
            var friendId = f.RequesterId == myId ? f.AddresseeId : f.RequesterId;
            userMap.TryGetValue(friendId, out var user);
            profileMap.TryGetValue(friendId, out var profile);

            return new FriendshipResponse
            {
                Id = f.Id,
                UserId = friendId,
                Username = user?.Username ?? "Unknown",
                AvatarUrl = profile?.AvatarUrl,
                Status = f.Status.ToString(),
                IsRequester = f.RequesterId == myId,
                CreatedAt = f.CreatedAt,
                RespondedAt = f.RespondedAt
            };
        }).OrderBy(f => f.Username).ToList();

        return Result<List<FriendshipResponse>>.Success(result);
    }
}
