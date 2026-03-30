using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Features.Elo.Helpers;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Rooms.Helpers;

public static class RoomEloHelper
{
    public static async Task RecalculateAsync(
        IMongoContext mongo, string roomId, CancellationToken ct = default)
    {
        var rooms = mongo.GetCollection<Room>(CollectionNames.Rooms);
        var room = await rooms.Find(r => r.Id == roomId).FirstOrDefaultAsync(ct);
        if (room is null) return;

        var rmCol = mongo.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        var memberUserIds = await rmCol
            .Find(m => m.RoomId == roomId)
            .Project(m => m.UserId)
            .ToListAsync(ct);

        int? averageElo = null;
        string? averageRankTier = null;

        if (memberUserIds.Count > 0)
        {
            var gpCol = mongo.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
            var profiles = await gpCol
                .Find(gp => memberUserIds.Contains(gp.UserId) && gp.GameId == room.GameId)
                .Project(gp => gp.EloPoints)
                .ToListAsync(ct);

            if (profiles.Count > 0)
            {
                averageElo = (int)Math.Round(profiles.Average());
                averageRankTier = EloCalculator.GetTier(averageElo.Value).ToString();
            }
        }

        await rooms.UpdateOneAsync(
            Builders<Room>.Filter.Eq(r => r.Id, roomId),
            Builders<Room>.Update
                .Set(r => r.AverageElo, averageElo)
                .Set(r => r.AverageRankTier, averageRankTier)
                .Set(r => r.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);
    }
}
