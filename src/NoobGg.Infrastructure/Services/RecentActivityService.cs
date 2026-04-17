using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Infrastructure.Services;

public class RecentActivityService : IRecentActivityService
{
    private readonly IMongoContext _mongoContext;

    public RecentActivityService(IMongoContext mongoContext)
    {
        _mongoContext = mongoContext;
    }

    public async Task UpsertAsync(string userId, string targetId, RecentActivityTargetType targetType, CancellationToken ct = default)
    {
        var collection = _mongoContext.GetCollection<RecentActivity>(CollectionNames.RecentActivities);

        var filter = Builders<RecentActivity>.Filter.And(
            Builders<RecentActivity>.Filter.Eq(r => r.UserId, userId),
            Builders<RecentActivity>.Filter.Eq(r => r.TargetId, targetId),
            Builders<RecentActivity>.Filter.Eq(r => r.TargetType, targetType));

        var now = DateTime.UtcNow;
        var update = Builders<RecentActivity>.Update
            .SetOnInsert(r => r.Id, Guid.NewGuid().ToString())
            .SetOnInsert(r => r.UserId, userId)
            .SetOnInsert(r => r.TargetId, targetId)
            .SetOnInsert(r => r.TargetType, targetType)
            .SetOnInsert(r => r.CreatedAt, now)
            .Set(r => r.SeenAt, now)
            .Set(r => r.UpdatedAt, now);

        await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct);
    }
}
