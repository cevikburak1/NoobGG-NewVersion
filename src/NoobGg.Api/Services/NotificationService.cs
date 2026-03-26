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
}
