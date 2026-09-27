using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;

namespace NoobGg.Api.Hubs;

[Authorize]
public class GuildChatHub(IMongoContext mongo) : Hub
{
    public async Task SendMessage(string guildId, string body)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(guildId))
            throw new HubException("Unauthorized");
        body = body.Trim();
        if (body.Length is < 1 or > 1000)
            throw new HubException("Message must be between 1 and 1000 characters");

        var users = mongo.GetCollection<User>(CollectionNames.Users);
        var user = await users.Find(u => u.Id == userId).FirstOrDefaultAsync(Context.ConnectionAborted);
        if (user is null || user.IsBanned || user.SuspendedUntil > DateTime.UtcNow)
            throw new HubException("Account restricted");

        var members = mongo.GetCollection<GuildMember>(CollectionNames.GuildMembers);
        var memberIds = await members.Find(m => m.GuildId == guildId)
            .Project(m => m.UserId).ToListAsync(Context.ConnectionAborted);
        if (!memberIds.Contains(userId))
            throw new HubException("Guild membership required");

        var message = new GuildChatMessage
        {
            GuildId = guildId,
            SenderId = userId,
            SenderUsername = user.Username,
            Body = body
        };
        await mongo.GetCollection<GuildChatMessage>(CollectionNames.GuildChatMessages)
            .InsertOneAsync(message, cancellationToken: Context.ConnectionAborted);
        await Clients.Users(memberIds).SendAsync("GuildMessage", message, Context.ConnectionAborted);
    }
}
