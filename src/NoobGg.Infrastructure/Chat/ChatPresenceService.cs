using Microsoft.Extensions.Logging;
using NoobGg.Application.Common.Interfaces;
using StackExchange.Redis;

namespace NoobGg.Infrastructure.Chat;

/// <summary>
/// Redis-backed presence tracking for room chat.
/// Uses Redis HASH for room→users mapping and SET for user→rooms mapping.
/// </summary>
public class ChatPresenceService : IChatPresenceService
{
    private readonly IDatabase _db;
    private readonly ILogger<ChatPresenceService> _logger;

    private const string RoomOnlinePrefix = "chat:room:online:";
    private const string UserRoomsPrefix = "chat:user:rooms:";

    public ChatPresenceService(IConnectionMultiplexer redis, ILogger<ChatPresenceService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task TrackUserJoinedRoomAsync(string roomId, string userId, string username)
    {
        var roomKey = RoomOnlinePrefix + roomId;
        var userKey = UserRoomsPrefix + userId;

        var batch = _db.CreateBatch();
        var t1 = batch.HashSetAsync(roomKey, userId, username);
        var t2 = batch.SetAddAsync(userKey, roomId);
        batch.Execute();

        await Task.WhenAll(t1, t2);

        _logger.LogDebug("Presence tracked: user {UserId} joined room {RoomId}", userId, roomId);
    }

    public async Task TrackUserLeftRoomAsync(string roomId, string userId)
    {
        var roomKey = RoomOnlinePrefix + roomId;
        var userKey = UserRoomsPrefix + userId;

        var batch = _db.CreateBatch();
        var t1 = batch.HashDeleteAsync(roomKey, userId);
        var t2 = batch.SetRemoveAsync(userKey, roomId);
        batch.Execute();

        await Task.WhenAll(t1, t2);

        _logger.LogDebug("Presence tracked: user {UserId} left room {RoomId}", userId, roomId);
    }

    public async Task<List<(string UserId, string Username)>> GetOnlineUsersInRoomAsync(string roomId)
    {
        var roomKey = RoomOnlinePrefix + roomId;
        var entries = await _db.HashGetAllAsync(roomKey);

        return entries
            .Select(e => (UserId: e.Name.ToString(), Username: e.Value.ToString()))
            .ToList();
    }

    public async Task RemoveUserFromAllRoomsAsync(string userId)
    {
        var userKey = UserRoomsPrefix + userId;
        var roomIds = await _db.SetMembersAsync(userKey);

        if (roomIds.Length == 0)
            return;

        var batch = _db.CreateBatch();
        var tasks = new List<Task>(roomIds.Length + 1);

        foreach (var roomId in roomIds)
        {
            var roomKey = RoomOnlinePrefix + roomId;
            tasks.Add(batch.HashDeleteAsync(roomKey, userId));
        }

        tasks.Add(batch.KeyDeleteAsync(userKey));
        batch.Execute();

        await Task.WhenAll(tasks);

        _logger.LogDebug("Presence cleanup: user {UserId} removed from {Count} rooms", userId, roomIds.Length);
    }

    public async Task<List<string>> GetUserRoomsAsync(string userId)
    {
        var userKey = UserRoomsPrefix + userId;
        var roomIds = await _db.SetMembersAsync(userKey);

        return roomIds.Select(r => r.ToString()).ToList();
    }
}
