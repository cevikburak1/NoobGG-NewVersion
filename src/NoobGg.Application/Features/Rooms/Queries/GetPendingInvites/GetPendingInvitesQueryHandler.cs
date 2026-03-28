using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Rooms.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Rooms.Queries.GetPendingInvites;

public class GetPendingInvitesQueryHandler : IRequestHandler<GetPendingInvitesQuery, Result<List<RoomInviteResponse>>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetPendingInvitesQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<List<RoomInviteResponse>>> Handle(GetPendingInvitesQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<List<RoomInviteResponse>>.Fail("Unauthorized", 401);

        var userId = _currentUser.UserId;

        var invites = _mongoContext.GetCollection<RoomInvite>(CollectionNames.RoomInvites);
        var pendingInvites = await invites
            .Find(i => i.InvitedUserId == userId && i.Status == RoomInviteStatus.Pending)
            .SortByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        if (pendingInvites.Count == 0)
            return Result<List<RoomInviteResponse>>.Success([]);

        var roomIds = pendingInvites.Select(i => i.RoomId).Distinct().ToList();
        var inviterIds = pendingInvites.Select(i => i.InviterId).Distinct().ToList();

        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var roomList = await rooms.Find(r => roomIds.Contains(r.Id)).ToListAsync(ct);
        var roomMap = roomList.ToDictionary(r => r.Id);

        var gameIds = roomList.Select(r => r.GameId).Distinct().ToList();
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var gameList = await games.Find(g => gameIds.Contains(g.Id)).ToListAsync(ct);
        var gameMap = gameList.ToDictionary(g => g.Id);

        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var inviterProfiles = await profiles.Find(p => inviterIds.Contains(p.UserId)).ToListAsync(ct);
        var profileMap = inviterProfiles.ToDictionary(p => p.UserId);

        var userCol = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var inviterUsers = await userCol.Find(u => inviterIds.Contains(u.Id)).ToListAsync(ct);
        var userMap = inviterUsers.ToDictionary(u => u.Id);

        var result = pendingInvites.Select(i =>
        {
            roomMap.TryGetValue(i.RoomId, out var room);
            Game? game = null;
            if (room is not null) gameMap.TryGetValue(room.GameId, out game);
            profileMap.TryGetValue(i.InviterId, out var profile);
            userMap.TryGetValue(i.InviterId, out var user);

            return new RoomInviteResponse(
                Id: i.Id,
                RoomId: i.RoomId,
                RoomTitle: room?.Title ?? "Unknown Room",
                GameName: game?.Name,
                GameImageUrl: game?.BackgroundImageUrl,
                InviterId: i.InviterId,
                InviterUsername: user?.Username ?? "Unknown",
                InviterAvatarUrl: profile?.AvatarUrl,
                Status: i.Status.ToString(),
                CreatedAt: i.CreatedAt);
        }).ToList();

        return Result<List<RoomInviteResponse>>.Success(result);
    }
}
