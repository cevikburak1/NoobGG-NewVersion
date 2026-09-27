using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Api.BackgroundJobs;

public sealed class GuildEventReminderJob(IServiceScopeFactory scopes, ILogger<GuildEventReminderJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var mongo = scope.ServiceProvider.GetRequiredService<IMongoContext>();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var events = mongo.GetCollection<GuildEvent>(CollectionNames.GuildEvents);
                var members = mongo.GetCollection<GuildMember>(CollectionNames.GuildMembers);
                var now = DateTime.UtcNow;
                var due = await events.Find(e => e.StartsAt > now &&
                    e.StartsAt <= now.AddHours(1) && e.ReminderSentAt == null)
                    .Limit(100).ToListAsync(stoppingToken);
                foreach (var guildEvent in due)
                {
                    var claim = await events.UpdateOneAsync(e => e.Id == guildEvent.Id && e.ReminderSentAt == null,
                        Builders<GuildEvent>.Update.Set(e => e.ReminderSentAt, now), cancellationToken: stoppingToken);
                    if (claim.ModifiedCount == 0) continue;
                    var activeMemberIds = await members.Find(m => m.GuildId == guildEvent.GuildId)
                        .Project(m => m.UserId).ToListAsync(stoppingToken);
                    foreach (var userId in guildEvent.GoingUserIds.Intersect(activeMemberIds))
                    {
                        await notifications.CreateAsync(userId, NotificationType.SystemMessage,
                            "Guild event starts soon", guildEvent.Title,
                            new Dictionary<string, string> { ["guildId"] = guildEvent.GuildId, ["eventId"] = guildEvent.Id },
                            stoppingToken);
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Guild event reminder failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
