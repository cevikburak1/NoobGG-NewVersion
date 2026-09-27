using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;

namespace NoobGg.Api.Hubs;

public sealed class RestrictedUserHubFilter(IMongoContext mongo) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var userId = invocationContext.Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            throw new HubException("Unauthorized");
        var users = mongo.GetCollection<User>(CollectionNames.Users);
        var user = await users.Find(u => u.Id == userId)
            .FirstOrDefaultAsync(invocationContext.Context.ConnectionAborted);
        if (user is null || user.IsBanned || user.SuspendedUntil > DateTime.UtcNow)
        {
            invocationContext.Context.Abort();
            throw new HubException("Account restricted");
        }
        return await next(invocationContext);
    }
}
