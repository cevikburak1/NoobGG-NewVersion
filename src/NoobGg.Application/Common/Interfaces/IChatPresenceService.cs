namespace NoobGg.Application.Common.Interfaces;

/// <summary>
/// Redis-backed presence tracking for room chat.
/// Typing indicators are handled purely through SignalR broadcast (no storage).
/// </summary>
public interface IChatPresenceService
{
    Task TrackUserJoinedRoomAsync(string roomId, string userId, string username);
    Task TrackUserLeftRoomAsync(string roomId, string userId);
    Task<List<(string UserId, string Username)>> GetOnlineUsersInRoomAsync(string roomId);
    Task RemoveUserFromAllRoomsAsync(string userId);
    Task<List<string>> GetUserRoomsAsync(string userId);
}
