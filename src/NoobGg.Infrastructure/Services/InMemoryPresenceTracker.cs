using System.Collections.Concurrent;
using NoobGg.Application.Common.Interfaces;

namespace NoobGg.Infrastructure.Services;

public class InMemoryPresenceTracker : IPresenceTracker
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();
    private readonly object _lock = new();

    public void UserConnected(string userId, string connectionId)
    {
        lock (_lock)
        {
            var connections = _userConnections.GetOrAdd(userId, _ => new HashSet<string>());
            connections.Add(connectionId);
        }
    }

    public void UserDisconnected(string userId, string connectionId)
    {
        lock (_lock)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(connectionId);
                if (connections.Count == 0)
                    _userConnections.TryRemove(userId, out _);
            }
        }
    }

    public bool IsOnline(string userId)
    {
        return _userConnections.TryGetValue(userId, out var conns) && conns.Count > 0;
    }

    public Dictionary<string, bool> GetOnlineStatuses(IEnumerable<string> userIds)
    {
        var result = new Dictionary<string, bool>();
        foreach (var id in userIds)
            result[id] = IsOnline(id);
        return result;
    }

    public int GetConnectionCount(string userId)
    {
        return _userConnections.TryGetValue(userId, out var conns) ? conns.Count : 0;
    }
}
