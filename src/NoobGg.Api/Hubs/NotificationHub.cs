using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using NoobGg.Api.Hubs.Contracts;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;

namespace NoobGg.Api.Hubs;

[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    private readonly ILogger<NotificationHub> _logger;
    private readonly IMongoContext _mongoContext;

    public NotificationHub(ILogger<NotificationHub> logger, IMongoContext mongoContext)
    {
        _logger = logger;
        _mongoContext = mongoContext;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, $"notifications:{userId}");

        var collection = _mongoContext.GetCollection<Notification>(CollectionNames.Notifications);
        var unreadCount = await collection.CountDocumentsAsync(
            Builders<Notification>.Filter.And(
                Builders<Notification>.Filter.Eq(n => n.UserId, userId),
                Builders<Notification>.Filter.Eq(n => n.IsRead, false)));

        await Clients.Caller.UnreadCountChanged((int)unreadCount);

        _logger.LogInformation("User {UserId} connected to NotificationHub", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"notifications:{userId}");

        _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
        await base.OnDisconnectedAsync(exception);
    }

    private string GetUserId()
    {
        return Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? throw new HubException("User identity not found");
    }
}
