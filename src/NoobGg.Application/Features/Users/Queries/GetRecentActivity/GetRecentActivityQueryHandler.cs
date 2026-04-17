using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Users.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Users.Queries.GetRecentActivity;

public class GetRecentActivityQueryHandler
    : IRequestHandler<GetRecentActivityQuery, Result<RecentActivityResponse>>
{
    private const int ConversationLimit = 8;
    private const int RoomLimit = 8;
    private const int PlayerLimit = 12;
    private const int MembershipScanLimit = 28;
    private const int PeerRoomScanLimit = 10;

    private const int SourcePriorityDm = 3;
    private const int SourcePriorityFriend = 2;
    private const int SourcePriorityRoom = 1;

    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;

    public GetRecentActivityQueryHandler(IMongoContext mongoContext, ICurrentUser currentUser)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
    }

    public async Task<Result<RecentActivityResponse>> Handle(
        GetRecentActivityQuery request,
        CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<RecentActivityResponse>.Unauthorized();

        var userId = _currentUser.UserId;

        var blocksCol = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);
        var blockList = await blocksCol
            .Find(b => b.BlockerId == userId || b.BlockedUserId == userId)
            .ToListAsync(ct);
        var blockedIds = new HashSet<string>();
        foreach (var b in blockList)
            blockedIds.Add(b.BlockerId == userId ? b.BlockedUserId : b.BlockerId);

        var conversationsCol = _mongoContext.GetCollection<Conversation>(CollectionNames.Conversations);
        var usersCol = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var profilesCol = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var roomMembersCol = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        var roomsCol = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var gamesCol = _mongoContext.GetCollection<Game>(CollectionNames.Games);
        var friendshipsCol = _mongoContext.GetCollection<Friendship>(CollectionNames.Friendships);

        var convFilter = Builders<Conversation>.Filter.Or(
            Builders<Conversation>.Filter.Eq(c => c.Participant1Id, userId),
            Builders<Conversation>.Filter.Eq(c => c.Participant2Id, userId));

        var convList = await conversationsCol
            .Find(convFilter)
            .SortByDescending(c => c.LastMessageAt)
            .ToListAsync(ct);

        convList = convList
            .Where(c =>
            {
                var partnerId = c.Participant1Id == userId ? c.Participant2Id : c.Participant1Id;
                return !blockedIds.Contains(partnerId);
            })
            .Take(ConversationLimit)
            .ToList();

        var partnerIdsForConv = convList
            .Select(c => c.Participant1Id == userId ? c.Participant2Id : c.Participant1Id)
            .Distinct()
            .ToList();

        var partnerUsersConv = partnerIdsForConv.Count > 0
            ? await usersCol.Find(Builders<User>.Filter.In(u => u.Id, partnerIdsForConv)).ToListAsync(ct)
            : [];
        var partnerProfilesConv = partnerIdsForConv.Count > 0
            ? await profilesCol.Find(Builders<UserProfile>.Filter.In(p => p.UserId, partnerIdsForConv)).ToListAsync(ct)
            : [];
        var userMapConv = partnerUsersConv.ToDictionary(u => u.Id);
        var profileMapConv = partnerProfilesConv.ToDictionary(p => p.UserId);

        var recentConversations = convList.Select(c =>
        {
            var partnerId = c.Participant1Id == userId ? c.Participant2Id : c.Participant1Id;
            var unread = c.Participant1Id == userId ? c.Participant1UnreadCount : c.Participant2UnreadCount;
            userMapConv.TryGetValue(partnerId, out var partner);
            profileMapConv.TryGetValue(partnerId, out var partnerProfile);

            return new RecentConversationItem
            {
                Id = c.Id,
                PartnerId = partnerId,
                PartnerUsername = partner?.Username ?? "Unknown",
                PartnerAvatarUrl = partnerProfile?.AvatarUrl,
                LastMessageContent = c.LastMessageContent,
                LastMessageSenderId = c.LastMessageSenderId,
                LastMessageAt = c.LastMessageAt,
                UnreadCount = unread
            };
        }).ToList();

        var myMemberships = await roomMembersCol
            .Find(m => m.UserId == userId)
            .SortByDescending(m => m.JoinedAt)
            .Limit(MembershipScanLimit)
            .ToListAsync(ct);

        var roomIdsInOrder = new List<string>();
        foreach (var m in myMemberships)
        {
            if (roomIdsInOrder.Contains(m.RoomId))
                continue;
            roomIdsInOrder.Add(m.RoomId);
            if (roomIdsInOrder.Count >= 15)
                break;
        }

        var roomDocs = roomIdsInOrder.Count > 0
            ? await roomsCol.Find(Builders<Room>.Filter.In(r => r.Id, roomIdsInOrder)).ToListAsync(ct)
            : [];
        var roomMap = roomDocs.ToDictionary(r => r.Id);

        var gameIds = roomDocs.Select(r => r.GameId).Distinct().ToList();
        var gameDocs = gameIds.Count > 0
            ? await gamesCol.Find(Builders<Game>.Filter.In(g => g.Id, gameIds)).ToListAsync(ct)
            : [];
        var gameMap = gameDocs.ToDictionary(g => g.Id, g => (g.Name, g.BackgroundImageUrl));

        var recentRooms = new List<RecentRoomItem>();
        foreach (var m in myMemberships)
        {
            if (recentRooms.Count >= RoomLimit)
                break;
            if (!roomMap.TryGetValue(m.RoomId, out var room))
                continue;
            if (recentRooms.Any(r => r.RoomId == m.RoomId))
                continue;

            gameMap.TryGetValue(room.GameId, out var game);
            recentRooms.Add(new RecentRoomItem
            {
                RoomId = room.Id,
                Title = room.Title,
                GameId = room.GameId,
                GameName = game.Name,
                GameImageUrl = game.BackgroundImageUrl,
                JoinedAt = m.JoinedAt,
                CurrentMemberCount = room.CurrentMemberCount,
                Region = room.Region.ToString(),
                Status = room.Status.ToString()
            });
        }

        var peerRoomIds = roomIdsInOrder.Take(PeerRoomScanLimit).ToList();
        var peerMembers = peerRoomIds.Count > 0
            ? await roomMembersCol
                .Find(Builders<RoomMember>.Filter.And(
                    Builders<RoomMember>.Filter.In(m => m.RoomId, peerRoomIds),
                    Builders<RoomMember>.Filter.Ne(m => m.UserId, userId)))
                .ToListAsync(ct)
            : [];

        var bestByUser = new Dictionary<string, (DateTime AtUtc, int Priority, string Source)>();

        void Consider(string otherUserId, DateTime atUtc, int priority, string source)
        {
            if (otherUserId == userId || blockedIds.Contains(otherUserId))
                return;

            if (!bestByUser.TryGetValue(otherUserId, out var cur))
            {
                bestByUser[otherUserId] = (atUtc, priority, source);
                return;
            }

            if (atUtc > cur.AtUtc || (atUtc == cur.AtUtc && priority > cur.Priority))
                bestByUser[otherUserId] = (atUtc, priority, source);
        }

        foreach (var c in convList)
        {
            var partnerId = c.Participant1Id == userId ? c.Participant2Id : c.Participant1Id;
            var at = c.LastMessageAt ?? c.CreatedAt;
            Consider(partnerId, at, SourcePriorityDm, "directMessage");
        }

        var friendshipsAccepted = await friendshipsCol
            .Find(f =>
                f.Status == FriendshipStatus.Accepted &&
                (f.RequesterId == userId || f.AddresseeId == userId))
            .ToListAsync(ct);

        foreach (var f in friendshipsAccepted)
        {
            var friendId = f.RequesterId == userId ? f.AddresseeId : f.RequesterId;
            var at = f.RespondedAt ?? f.CreatedAt;
            Consider(friendId, at, SourcePriorityFriend, "friendship");
        }

        foreach (var pm in peerMembers)
        {
            Consider(pm.UserId, pm.JoinedAt, SourcePriorityRoom, "room");
        }

        var ranked = bestByUser
            .OrderByDescending(kv => kv.Value.AtUtc)
            .ThenByDescending(kv => kv.Value.Priority)
            .Take(PlayerLimit)
            .Select(kv => kv.Key)
            .ToList();

        var playerUsers = ranked.Count > 0
            ? await usersCol.Find(Builders<User>.Filter.In(u => u.Id, ranked)).ToListAsync(ct)
            : [];
        var playerProfiles = ranked.Count > 0
            ? await profilesCol.Find(Builders<UserProfile>.Filter.In(p => p.UserId, ranked)).ToListAsync(ct)
            : [];
        var userMapPlayers = playerUsers.ToDictionary(u => u.Id);
        var profileMapPlayers = playerProfiles.ToDictionary(p => p.UserId);

        var recentPlayers = ranked
            .Select(uid =>
            {
                var meta = bestByUser[uid];
                userMapPlayers.TryGetValue(uid, out var u);
                profileMapPlayers.TryGetValue(uid, out var p);
                return new RecentPlayerItem
                {
                    UserId = uid,
                    Username = u?.Username ?? "Unknown",
                    AvatarUrl = p?.AvatarUrl,
                    LastInteractionAt = meta.AtUtc,
                    Source = meta.Source
                };
            })
            .ToList();

        return Result<RecentActivityResponse>.Success(new RecentActivityResponse
        {
            RecentPlayers = recentPlayers,
            RecentConversations = recentConversations,
            RecentRooms = recentRooms
        });
    }
}
