namespace NoobGg.Application.Common.Interfaces;

public interface IRoomNotificationService
{
    Task NotifyMemberJoinedAsync(string roomId, string userId, string username, CancellationToken ct = default);
    Task NotifyMemberLeftAsync(string roomId, string userId, string username, CancellationToken ct = default);
    Task NotifyRoomClosedAsync(string roomId, CancellationToken ct = default);
    Task NotifyRoomListChangedAsync(CancellationToken ct = default);
}
