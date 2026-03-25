using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NoobGg.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("User {UserId} connected to ChatHub", Context.UserIdentifier);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("User {UserId} disconnected from ChatHub", Context.UserIdentifier);
        await base.OnDisconnectedAsync(exception);
    }
}
