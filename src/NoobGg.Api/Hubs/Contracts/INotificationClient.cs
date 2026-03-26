using NoobGg.Application.Features.Notifications.DTOs;

namespace NoobGg.Api.Hubs.Contracts;

public interface INotificationClient
{
    Task ReceiveNotification(NotificationResponse notification);
    Task UnreadCountChanged(int count);
}
