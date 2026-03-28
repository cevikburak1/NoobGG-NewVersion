using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Friendships.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Friendships.Queries.GetPendingRequests;

public class GetPendingRequestsQueryHandler : IRequestHandler<GetPendingRequestsQuery, Result<PendingRequestsResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetPendingRequestsQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PendingRequestsResponse>> Handle(GetPendingRequestsQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<PendingRequestsResponse>.Unauthorized();

        var myId = _currentUser.UserId;
        var friendships = _mongoContext.GetCollection<Friendship>(CollectionNames.Friendships);
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);

        var pending = await friendships.Find(f =>
            f.Status == FriendshipStatus.Pending &&
            (f.RequesterId == myId || f.AddresseeId == myId)
        ).ToListAsync(ct);

        if (pending.Count == 0)
            return Result<PendingRequestsResponse>.Success(new PendingRequestsResponse());

        var allUserIds = pending
            .SelectMany(f => new[] { f.RequesterId, f.AddresseeId })
            .Where(id => id != myId)
            .Distinct()
            .ToList();

        var userList = await users.Find(Builders<User>.Filter.In(u => u.Id, allUserIds)).ToListAsync(ct);
        var userMap = userList.ToDictionary(u => u.Id);

        var profileList = await profiles.Find(Builders<UserProfile>.Filter.In(p => p.UserId, allUserIds)).ToListAsync(ct);
        var profileMap = profileList.ToDictionary(p => p.UserId);

        var incoming = new List<FriendRequestResponse>();
        var outgoing = new List<FriendRequestResponse>();

        foreach (var f in pending)
        {
            var otherId = f.RequesterId == myId ? f.AddresseeId : f.RequesterId;
            userMap.TryGetValue(otherId, out var user);
            profileMap.TryGetValue(otherId, out var profile);

            var dto = new FriendRequestResponse
            {
                FriendshipId = f.Id,
                UserId = otherId,
                Username = user?.Username ?? "Unknown",
                AvatarUrl = profile?.AvatarUrl,
                CreatedAt = f.CreatedAt
            };

            if (f.AddresseeId == myId)
                incoming.Add(dto);
            else
                outgoing.Add(dto);
        }

        return Result<PendingRequestsResponse>.Success(new PendingRequestsResponse
        {
            Incoming = incoming.OrderByDescending(r => r.CreatedAt).ToList(),
            Outgoing = outgoing.OrderByDescending(r => r.CreatedAt).ToList()
        });
    }
}
