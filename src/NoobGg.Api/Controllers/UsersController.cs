using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Features.Users.Queries.DiscoverPlayers;
using NoobGg.Domain.Enums;

namespace NoobGg.Api.Controllers;

[Route("api/users")]
public class UsersController : ApiControllerBase
{
    [HttpGet("discover")]
    public async Task<IActionResult> Discover(
        [FromQuery] string? search = null,
        [FromQuery] Region? region = null,
        [FromQuery] ExperienceLevel? experienceLevel = null,
        [FromQuery] bool? lookingForTeam = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var query = new DiscoverPlayersQuery
        {
            Search = search,
            Region = region,
            ExperienceLevel = experienceLevel,
            LookingForTeam = lookingForTeam,
            Page = page,
            PageSize = pageSize
        };

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("{userId}/presence")]
    public IActionResult GetPresence(string userId, [FromServices] IPresenceTracker presenceTracker)
    {
        return Ok(new { isOnline = presenceTracker.IsOnline(userId) });
    }

    [HttpPost("presence/batch")]
    public IActionResult GetPresenceBatch(
        [FromBody] string[] userIds,
        [FromServices] IPresenceTracker presenceTracker)
    {
        var statuses = presenceTracker.GetOnlineStatuses(userIds);
        return Ok(statuses);
    }
}
