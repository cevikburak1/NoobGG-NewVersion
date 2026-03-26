namespace NoobGg.Application.Common.Interfaces;

public interface IPresenceTracker
{
    void UserConnected(string userId, string connectionId);
    void UserDisconnected(string userId, string connectionId);
    bool IsOnline(string userId);
    Dictionary<string, bool> GetOnlineStatuses(IEnumerable<string> userIds);
    int GetConnectionCount(string userId);
}
