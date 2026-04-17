using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoobGg.Application.Features.GuildEvents.Commands.CreateEvent;
using NoobGg.Application.Features.GuildEvents.Commands.DeleteEvent;
using NoobGg.Application.Features.GuildEvents.Queries.GetEvents;

namespace NoobGg.Api.Controllers;

[Authorize]
[Route("api/guild-events")]
public class GuildEventsController : ApiControllerBase
{
    [HttpGet("{guildId}")]
    public async Task<IActionResult> GetEvents(string guildId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = await Mediator.Send(new GetGuildEventsQuery { GuildId = guildId, From = from, To = to });
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] CreateGuildEventCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpDelete("{eventId}")]
    public async Task<IActionResult> DeleteEvent(string eventId)
    {
        var result = await Mediator.Send(new DeleteGuildEventCommand { EventId = eventId });
        return HandleResult(result);
    }
}
