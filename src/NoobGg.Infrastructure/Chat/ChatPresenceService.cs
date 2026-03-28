using Microsoft.Extensions.Logging;
using NoobGg.Application.Common.Interfaces;
using StackExchange.Redis;

namespace NoobGg.Infrastructure.Chat;

/// <summary>
/// Redis-backed presence tracking for room chat.
/// Uses per-user connection SETs to handle multi-tab correctly:
/// a user is only removed from a room when their last connection leaves.
/// </summary>
public class ChatPresenceService : IChatPresenceService
{
    private readonly IDatabase _db;
    private readonly ILogger<ChatPresenceService> _logger;

    private const string RoomOnlinePrefix = "chat:room:online:";
    private const string UserRoomsPrefix = "chat:user:rooms:";
    private const string RoomConnPrefix = "chat:room:conns:";

    public ChatPresenceService(IConnectionMultiplexer redis, ILogger<ChatPresenceService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task TrackUserJoinedRoomAsync(string roomId, string userId, string username, string connectionId)
    {
        var roomKey = RoomOnlinePrefix + roomId;
        var userKey = UserRoomsPrefix + userId;
        var connKey = $"{RoomConnPrefix}{roomId}:{userId}";

        var batch = _db.CreateBatch();
        var t1 = batch.HashSetAsync(roomKey, userId, username);
        var t2 = batch.SetAddAsync(userKey, roomId);
        var t3 = batch.SetAddAsync(connKey, connectionId);
        batch.Execute();

        await Task.WhenAll(t1, t2, t3);

        _logger.LogDebug("Presence tracked: connection {ConnId} for user {UserId} joined room {RoomId}",
            connectionId, userId, roomId);
    }

    public async Task<bool> TrackUserLeftRoomAsync(string roomId, string userId, string connectionId)
    {
        var connKey = $"{RoomConnPrefix}{roomId}:{userId}";

        await _db.SetRemoveAsync(connKey, connectionId);
        var remaining = await _db.SetLengthAsync(connKey);

        if (remaining > 0)
        {
            _logger.LogDebug(
                "Connection {ConnId} left room {RoomId}, user {UserId} still has {Count} connection(s)",
                connectionId, roomId, userId, remaining);
            return false;
        }

        var roomKey = RoomOnlinePrefix + roomId;
        var userKey = UserRoomsPrefix + userId;

        var batch = _db.CreateBatch();
        var t1 = batch.HashDeleteAsync(roomKey, userId);
        var t2 = batch.SetRemoveAsync(userKey, roomId);
        var t3 = batch.KeyDeleteAsync(connKey);
        batch.Execute();

        await Task.WhenAll(t1, t2, t3);

        _logger.LogDebug("Presence tracked: user {UserId} fully left room {RoomId} (last connection)", userId, roomId);
        return true;
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
        var tasks = new List<Task>(roomIds.Length * 2 + 1);

        foreach (var roomId in roomIds)
        {
            tasks.Add(batch.HashDeleteAsync(RoomOnlinePrefix + roomId, userId));
            tasks.Add(batch.KeyDeleteAsync($"{RoomConnPrefix}{roomId}:{userId}"));
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
