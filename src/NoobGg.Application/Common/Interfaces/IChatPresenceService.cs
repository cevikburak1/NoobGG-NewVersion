namespace NoobGg.Application.Common.Interfaces;

/// <summary>
/// Redis-backed presence tracking for room chat.
/// Tracks per-connection state to handle multi-tab scenarios correctly.
/// Typing indicators are handled purely through SignalR broadcast (no storage).
/// </summary>
public interface IChatPresenceService
{
    Task TrackUserJoinedRoomAsync(string roomId, string userId, string username, string connectionId);

    /// <returns>True if the user has no remaining connections in this room (fully left).</returns>
    Task<bool> TrackUserLeftRoomAsync(string roomId, string userId, string connectionId);

    Task<List<(string UserId, string Username)>> GetOnlineUsersInRoomAsync(string roomId);

    Task RemoveUserFromAllRoomsAsync(string userId);

    Task<List<string>> GetUserRoomsAsync(string userId);
}
