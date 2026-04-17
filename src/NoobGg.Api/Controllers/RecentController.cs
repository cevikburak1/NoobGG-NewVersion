using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Recent.Queries.GetRecentPlayers;
using NoobGg.Application.Features.Recent.Queries.GetRecentRooms;

namespace NoobGg.Api.Controllers;

[Route("api/recent")]
[Authorize]
public class RecentController : ApiControllerBase
{
    [HttpGet("players")]
    public async Task<IActionResult> GetRecentPlayers([FromQuery] int limit = 5)
    {
        var query = new GetRecentPlayersQuery { Limit = Math.Clamp(limit, 1, 10) };
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRecentRooms([FromQuery] int limit = 5)
    {
        var query = new GetRecentRoomsQuery { Limit = Math.Clamp(limit, 1, 10) };
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }
}
