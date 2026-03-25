using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Chat.Queries.GetChatHistory;

namespace NoobGg.Api.Controllers;

[Route("api/chat")]
[Authorize]
public class ChatController : ApiControllerBase
{
    [HttpGet("{roomId}/messages")]
    public async Task<IActionResult> GetMessages(
        string roomId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTime? before = null)
    {
        var query = new GetChatHistoryQuery
        {
            RoomId = roomId,
            Page = page,
            PageSize = pageSize,
            Before = before
        };

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }
}
