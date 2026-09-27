using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;

namespace NoobGg.Api.Controllers;

[Authorize]
[Route("api/guilds/{guildId}/chat")]
public class GuildChatController(IMongoContext mongo, ICurrentUser currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string guildId, [FromQuery] string? beforeId, CancellationToken ct)
    {
        var members = mongo.GetCollection<GuildMember>(CollectionNames.GuildMembers);
        if (!await members.Find(m => m.GuildId == guildId && m.UserId == currentUser.UserId).AnyAsync(ct))
            return Forbid();
        var messages = mongo.GetCollection<GuildChatMessage>(CollectionNames.GuildChatMessages);
        var filter = Builders<GuildChatMessage>.Filter.Eq(m => m.GuildId, guildId);
        if (!string.IsNullOrEmpty(beforeId))
        {
            var cursor = await messages.Find(m => m.Id == beforeId && m.GuildId == guildId).FirstOrDefaultAsync(ct);
            if (cursor is null) return BadRequest("Invalid chat cursor.");
            filter &= Builders<GuildChatMessage>.Filter.Or(
                Builders<GuildChatMessage>.Filter.Lt(m => m.CreatedAt, cursor.CreatedAt),
                Builders<GuildChatMessage>.Filter.And(
                    Builders<GuildChatMessage>.Filter.Eq(m => m.CreatedAt, cursor.CreatedAt),
                    Builders<GuildChatMessage>.Filter.Lt(m => m.Id, cursor.Id)));
        }
        const int pageSize = 30;
        var latest = await messages.Find(filter)
            .SortByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
            .Limit(pageSize + 1).ToListAsync(ct);
        var hasMore = latest.Count > pageSize;
        if (hasMore) latest.RemoveAt(pageSize);
        var nextCursor = hasMore ? latest[^1].Id : null;
        latest.Reverse();
        return Ok(new { items = latest, nextCursor });
    }
}
