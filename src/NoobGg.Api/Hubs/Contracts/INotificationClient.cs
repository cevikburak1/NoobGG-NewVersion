using NoobGg.Application.Features.Notifications.DTOs;

namespace NoobGg.Api.Hubs.Contracts;

public interface INotificationClient
{
    Task ReceiveNotification(NotificationResponse notification);
    Task UnreadCountChanged(int count);
    Task BlockListChanged(string userId, bool isBlocked);
    Task FriendListChanged();
    Task ForceDisconnect(string reason);
}
