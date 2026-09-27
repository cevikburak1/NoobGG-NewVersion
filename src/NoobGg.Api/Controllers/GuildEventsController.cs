using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;
using NoobGg.Application.Features.GuildEvents.Commands.CreateEvent;
using NoobGg.Application.Features.GuildEvents.Commands.DeleteEvent;
using NoobGg.Application.Features.GuildEvents.Queries.GetEvents;

namespace NoobGg.Api.Controllers;

[Authorize]
[Route("api/guild-events")]
public class GuildEventsController(IMongoContext mongo, ICurrentUser currentUser) : ApiControllerBase
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

    [HttpPost("{eventId}/rsvp")]
    public async Task<IActionResult> Rsvp(string eventId, [FromBody] GuildEventRsvpRequest request, CancellationToken ct)
    {
        var events = mongo.GetCollection<GuildEvent>(CollectionNames.GuildEvents);
        var guildEvent = await events.Find(e => e.Id == eventId).FirstOrDefaultAsync(ct);
        if (guildEvent is null) return NotFound();
        if (guildEvent.StartsAt <= DateTime.UtcNow) return Conflict("Event has started");
        var members = mongo.GetCollection<GuildMember>(CollectionNames.GuildMembers);
        if (!await members.Find(m => m.GuildId == guildEvent.GuildId && m.UserId == currentUser.UserId).AnyAsync(ct))
            return Forbid();
        var update = request.Going
            ? Builders<GuildEvent>.Update.AddToSet(e => e.GoingUserIds, currentUser.UserId!)
            : Builders<GuildEvent>.Update.Pull(e => e.GoingUserIds, currentUser.UserId!);
        await events.UpdateOneAsync(e => e.Id == eventId, update, cancellationToken: ct);
        return NoContent();
    }
}

public record GuildEventRsvpRequest(bool Going);
