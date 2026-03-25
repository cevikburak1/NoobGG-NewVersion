using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NoobGg.Api.Hubs;

[Authorize]
public class RoomHub : Hub
{
    private readonly ILogger<RoomHub> _logger;

    public RoomHub(ILogger<RoomHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("User {UserId} connected to RoomHub", Context.UserIdentifier);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("User {UserId} disconnected from RoomHub", Context.UserIdentifier);
        await base.OnDisconnectedAsync(exception);
    }
}
