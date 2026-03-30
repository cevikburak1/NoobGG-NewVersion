using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.Recommendations.Queries.GetRecommendedPlayers;
using NoobGg.Application.Features.Recommendations.Queries.GetRecommendedRooms;

namespace NoobGg.Api.Controllers;

[Route("api/recommendations")]
[Authorize]
public class RecommendationsController : ApiControllerBase
{
    [HttpGet("players")]
    public async Task<IActionResult> GetRecommendedPlayers([FromQuery] int limit = 6)
    {
        var query = new GetRecommendedPlayersQuery { Limit = Math.Clamp(limit, 1, 20) };
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRecommendedRooms([FromQuery] int limit = 6)
    {
        var query = new GetRecommendedRoomsQuery { Limit = Math.Clamp(limit, 1, 20) };
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }
}
