using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using NoobGg.Api.Hubs;
using NoobGg.Api.Hubs.Contracts;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Features.Notifications.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Api.Services;

public class NotificationService : INotificationService
{
    private readonly IMongoContext _mongoContext;
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IMongoContext mongoContext,
        IHubContext<NotificationHub, INotificationClient> hubContext,
        ILogger<NotificationService> logger)
    {
        _mongoContext = mongoContext;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task CreateAsync(
        string userId,
        NotificationType type,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default)
    {
        var settingsCol = _mongoContext.GetCollection<UserSettings>(CollectionNames.UserSettings);
        var settings = await settingsCol.Find(s => s.UserId == userId).FirstOrDefaultAsync(ct);

        if (settings is not null && !ShouldNotify(settings, type))
        {
            _logger.LogDebug("Notification suppressed for user {UserId}: {Type} (preference off)", userId, type);
            return;
        }

        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            Data = data
        };

        var collection = _mongoContext.GetCollection<Notification>(CollectionNames.Notifications);
        await collection.InsertOneAsync(notification, cancellationToken: ct);

        var response = new NotificationResponse
        {
            Id = notification.Id,
            Type = notification.Type,
            Title = notification.Title,
            Body = notification.Body,
            Data = notification.Data,
            IsRead = false,
            ReadAt = null,
            CreatedAt = notification.CreatedAt
        };

        var groupName = $"notifications:{userId}";

        await _hubContext.Clients.Group(groupName).ReceiveNotification(response);

        var unreadCount = await collection.CountDocumentsAsync(
            Builders<Notification>.Filter.And(
                Builders<Notification>.Filter.Eq(n => n.UserId, userId),
                Builders<Notification>.Filter.Eq(n => n.IsRead, false)),
            cancellationToken: ct);

        await _hubContext.Clients.Group(groupName).UnreadCountChanged((int)unreadCount);

        _logger.LogDebug("Notification created for user {UserId}: {Type}", userId, type);
    }

    private static bool ShouldNotify(UserSettings settings, NotificationType type) => type switch
    {
        NotificationType.DirectMessage => settings.NotifyDirectMessages,
        NotificationType.FriendRequest or NotificationType.FriendAccepted => settings.NotifyFriendRequests,
        NotificationType.RoomJoined or NotificationType.RoomLeft or NotificationType.RoomClosed or NotificationType.RoomInvite => settings.NotifyRoomActivity,
        NotificationType.ReportResolved or NotificationType.SubscriptionChanged or NotificationType.SystemMessage => settings.NotifySystemMessages,
        _ => true
    };

    public async Task SendBlockListChangedAsync(string userId1, string userId2, bool isBlocked, CancellationToken ct = default)
    {
        var group1 = $"notifications:{userId1}";
        var group2 = $"notifications:{userId2}";

        await Task.WhenAll(
            _hubContext.Clients.Group(group1).BlockListChanged(userId2, isBlocked),
            _hubContext.Clients.Group(group2).BlockListChanged(userId1, isBlocked)
        );
    }

    public async Task SendFriendListChangedAsync(string userId1, string userId2, CancellationToken ct = default)
    {
        var group1 = $"notifications:{userId1}";
        var group2 = $"notifications:{userId2}";

        await Task.WhenAll(
            _hubContext.Clients.Group(group1).FriendListChanged(),
            _hubContext.Clients.Group(group2).FriendListChanged()
        );
    }
}
