using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Api.BackgroundJobs;

public sealed class MatchmakingRecoveryJob(IServiceScopeFactory scopes, ILogger<MatchmakingRecoveryJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var mongo = scope.ServiceProvider.GetRequiredService<IMongoContext>();
                var queue = mongo.GetCollection<MatchQueueEntry>(CollectionNames.MatchQueueEntries);
                var rooms = mongo.GetCollection<Room>(CollectionNames.Rooms);
                var members = mongo.GetCollection<RoomMember>(CollectionNames.RoomMembers);
                var cutoff = DateTime.UtcNow.AddMinutes(-2);

                await queue.UpdateManyAsync(e => e.Status == MatchQueueEntryStatus.Matching && e.UpdatedAt < cutoff,
                    Builders<MatchQueueEntry>.Update.Set(e => e.Status, MatchQueueEntryStatus.Expired)
                        .Set(e => e.MatchClaimId, (string?)null), cancellationToken: stoppingToken);
                await queue.UpdateManyAsync(e => (e.Status == MatchQueueEntryStatus.Searching ||
                    e.Status == MatchQueueEntryStatus.FallbackSuggested) && e.ExpiresAt < DateTime.UtcNow,
                    Builders<MatchQueueEntry>.Update.Set(e => e.Status, MatchQueueEntryStatus.Expired),
                    cancellationToken: stoppingToken);

                var unfinished = await rooms.Find(r => !r.IsPublic && r.Tags.Contains("quick-match") && r.CreatedAt < cutoff)
                    .Limit(100).ToListAsync(stoppingToken);
                foreach (var room in unfinished)
                {
                    if (await queue.Find(e => e.MatchedRoomId == room.Id && e.Status == MatchQueueEntryStatus.Matched)
                        .AnyAsync(stoppingToken))
                    {
                        await rooms.UpdateOneAsync(r => r.Id == room.Id,
                            Builders<Room>.Update.Set(r => r.IsPublic, true), cancellationToken: stoppingToken);
                    }
                    else
                    {
                        await members.DeleteManyAsync(m => m.RoomId == room.Id, stoppingToken);
                        await rooms.DeleteOneAsync(r => r.Id == room.Id, cancellationToken: stoppingToken);
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Matchmaking recovery failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
