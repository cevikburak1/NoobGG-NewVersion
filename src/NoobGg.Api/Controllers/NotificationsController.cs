using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Notifications.Commands.MarkAllRead;
using NoobGg.Application.Features.Notifications.Commands.MarkNotificationRead;
using NoobGg.Application.Features.Notifications.Queries.GetNotifications;
using NoobGg.Application.Features.Notifications.Queries.GetUnreadCount;

namespace NoobGg.Api.Controllers;

[Route("api/notifications")]
[Authorize]
public class NotificationsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool? unreadOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetNotificationsQuery
        {
            UnreadOnly = unreadOnly,
            Page = page,
            PageSize = pageSize
        };

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var result = await Mediator.Send(new GetUnreadCountQuery());
        return HandleResult(result);
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(string id)
    {
        var result = await Mediator.Send(new MarkNotificationReadCommand { NotificationId = id });
        return HandleResult(result);
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var result = await Mediator.Send(new MarkAllReadCommand());
        return HandleResult(result);
    }
}
