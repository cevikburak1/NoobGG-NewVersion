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
    public async Task<IActionResult> GetRecommendedPlayers(
        [FromQuery] string? gameId = null,
        [FromQuery] int limit = 10)
    {
        var query = new GetRecommendedPlayersQuery
        {
            GameId = gameId,
            Limit = limit
        };

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRecommendedRooms(
        [FromQuery] string? gameId = null,
        [FromQuery] int limit = 10)
    {
        var query = new GetRecommendedRoomsQuery
        {
            GameId = gameId,
            Limit = limit
        };

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }
}
