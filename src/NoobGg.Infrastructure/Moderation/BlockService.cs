using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;

namespace NoobGg.Infrastructure.Moderation;

public class BlockService : IBlockService
{
    private readonly IMongoContext _mongoContext;

    public BlockService(IMongoContext mongoContext)
    {
        _mongoContext = mongoContext;
    }

    public async Task<bool> IsBlockedAsync(string blockerId, string blockedUserId, CancellationToken ct = default)
    {
        var blocks = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);
        return await blocks
            .Find(b => b.BlockerId == blockerId && b.BlockedUserId == blockedUserId)
            .AnyAsync(ct);
    }

    public async Task<bool> HasBlockBetweenAsync(string userA, string userB, CancellationToken ct = default)
    {
        var blocks = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);

        var filter = Builders<Block>.Filter.Or(
            Builders<Block>.Filter.And(
                Builders<Block>.Filter.Eq(b => b.BlockerId, userA),
                Builders<Block>.Filter.Eq(b => b.BlockedUserId, userB)),
            Builders<Block>.Filter.And(
                Builders<Block>.Filter.Eq(b => b.BlockerId, userB),
                Builders<Block>.Filter.Eq(b => b.BlockedUserId, userA)));

        return await blocks.Find(filter).AnyAsync(ct);
    }

    public async Task<HashSet<string>> GetBlockedUserIdsAsync(string userId, CancellationToken ct = default)
    {
        var blocks = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);
        var docs = await blocks
            .Find(b => b.BlockerId == userId)
            .Project(b => b.BlockedUserId)
            .ToListAsync(ct);

        return docs.ToHashSet();
    }
}
