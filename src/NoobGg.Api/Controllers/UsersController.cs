using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Features.Users.Queries.DiscoverPlayers;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Api.Controllers;

[Route("api/users")]
public class UsersController : ApiControllerBase
{
    [HttpGet("discover")]
    public async Task<IActionResult> Discover(
        [FromQuery] string? search = null,
        [FromQuery] string? gameId = null,
        [FromQuery] Region? region = null,
        [FromQuery] ExperienceLevel? experienceLevel = null,
        [FromQuery] bool? lookingForTeam = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var query = new DiscoverPlayersQuery
        {
            Search = search,
            GameId = gameId,
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
    public async Task<IActionResult> GetPresence(
        string userId,
        [FromServices] IPresenceTracker presenceTracker,
        [FromServices] IMongoContext mongoContext)
    {
        var settingsCol = mongoContext.GetCollection<UserSettings>(CollectionNames.UserSettings);
        var settings = await settingsCol.Find(s => s.UserId == userId).FirstOrDefaultAsync();

        if (settings is { ShowOnlineStatus: false })
            return Ok(new { isOnline = false });

        return Ok(new { isOnline = presenceTracker.IsOnline(userId) });
    }

    [HttpPost("presence/batch")]
    public async Task<IActionResult> GetPresenceBatch(
        [FromBody] string[] userIds,
        [FromServices] IPresenceTracker presenceTracker,
        [FromServices] IMongoContext mongoContext)
    {
        var statuses = presenceTracker.GetOnlineStatuses(userIds);

        var settingsCol = mongoContext.GetCollection<UserSettings>(CollectionNames.UserSettings);
        var hiddenSettings = await settingsCol
            .Find(s => s.ShowOnlineStatus == false)
            .Project(s => s.UserId)
            .ToListAsync();
        var hiddenIds = new HashSet<string>(hiddenSettings);

        var result = new Dictionary<string, bool>();
        foreach (var kvp in statuses)
            result[kvp.Key] = hiddenIds.Contains(kvp.Key) ? false : kvp.Value;

        return Ok(result);
    }
}
