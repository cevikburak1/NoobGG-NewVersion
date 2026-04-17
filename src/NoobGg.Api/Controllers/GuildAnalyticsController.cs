using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.GuildAnalytics.Queries.GetGuildStats;

namespace NoobGg.Api.Controllers;

[Authorize]
[Route("api/guild-analytics")]
public class GuildAnalyticsController : ApiControllerBase
{
    [HttpGet("{guildId}")]
    public async Task<IActionResult> GetStats(string guildId, [FromQuery] string? gameId, [FromQuery] int days = 30)
    {
        var result = await Mediator.Send(new GetGuildStatsQuery
        {
            GuildId = guildId,
            GameId = gameId,
            Days = days
        });
        return HandleResult(result);
    }
}
