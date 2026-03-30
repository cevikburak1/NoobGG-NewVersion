using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Features.Elo.Helpers;
using NoobGg.Application.Features.Rooms.Helpers;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;
using NoobGg.Domain.ValueObjects;

namespace NoobGg.Api.BackgroundJobs;

/// <summary>
/// Ensures every room member has a UserGameProfile for their room's game.
/// Creates missing profiles with random Elo data so rank badges display properly.
/// </summary>
public class RoomMemberProfileSeeder : IHostedService
{
    private readonly IMongoContext _mongo;
    private readonly ILogger<RoomMemberProfileSeeder> _logger;
    private static readonly Random Rng = new(999);

    public RoomMemberProfileSeeder(IMongoContext mongo, ILogger<RoomMemberProfileSeeder> logger)
    {
        _mongo = mongo;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            await SeedMissingProfiles(ct);
            await RecalculateAllRoomAverages(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RoomMemberProfileSeeder: failed");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private async Task SeedMissingProfiles(CancellationToken ct)
    {
        var roomsCol = _mongo.GetCollection<Room>(CollectionNames.Rooms);
        var rmCol = _mongo.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        var gpCol = _mongo.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);

        var rooms = await roomsCol.Find(r => r.Status != RoomStatus.Closed).ToListAsync(ct);
        if (rooms.Count == 0) return;

        var created = 0;

        foreach (var room in rooms)
        {
            var members = await rmCol.Find(m => m.RoomId == room.Id)
                .Project(m => m.UserId)
                .ToListAsync(ct);

            if (members.Count == 0) continue;

            var existingProfiles = await gpCol
                .Find(gp => members.Contains(gp.UserId) && gp.GameId == room.GameId)
                .Project(gp => gp.UserId)
                .ToListAsync(ct);

            var existingSet = new HashSet<string>(existingProfiles);
            var missing = members.Where(uid => !existingSet.Contains(uid)).ToList();

            if (missing.Count == 0) continue;

            var newProfiles = missing.Select(userId =>
            {
                var elo = GenerateNormalElo();
                var tier = EloCalculator.GetTier(elo);
                return new UserGameProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    GameId = room.GameId,
                    Rank = tier.ToString(),
                    Role = null,
                    Region = room.Region,
                    Languages = [room.Language],
                    ExperienceLevel = Pick<ExperienceLevel>(),
                    CommunicationPreference = Pick<CommunicationPreference>(),
                    HoursPlayed = Rng.Next(10, 3000),
                    LookingForTeam = Rng.NextDouble() < 0.4,
                    EloPoints = elo,
                    RankTier = tier,
                    EloHistory = GenerateEloHistory(elo),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }).ToList();

            await gpCol.InsertManyAsync(newProfiles, new InsertManyOptions { IsOrdered = false }, ct);
            created += newProfiles.Count;
        }

        if (created > 0)
            _logger.LogInformation("RoomMemberProfileSeeder: created {Count} missing game profiles for room members", created);
        else
            _logger.LogInformation("RoomMemberProfileSeeder: no missing profiles found");
    }

    private async Task RecalculateAllRoomAverages(CancellationToken ct)
    {
        var roomsCol = _mongo.GetCollection<Room>(CollectionNames.Rooms);
        var rooms = await roomsCol.Find(r => r.Status != RoomStatus.Closed).ToListAsync(ct);
        var updated = 0;

        foreach (var room in rooms)
        {
            await RoomEloHelper.RecalculateAsync(_mongo, room.Id, ct);
            updated++;
        }

        _logger.LogInformation("RoomMemberProfileSeeder: recalculated average elo for {Count} rooms", updated);
    }

    private static int GenerateNormalElo()
    {
        var u1 = 1.0 - Rng.NextDouble();
        var u2 = Rng.NextDouble();
        var normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        var elo = (int)(1800 + normal * 700);
        return Math.Clamp(elo, 200, 4000);
    }

    private static List<EloSnapshot> GenerateEloHistory(int currentElo)
    {
        var history = new List<EloSnapshot>();
        var points = currentElo - Rng.Next(-200, 200);
        points = Math.Clamp(points, 200, 4000);

        for (var day = 29; day >= 0; day--)
        {
            var drift = Rng.Next(-40, 41);
            points = Math.Clamp(points + drift, 200, 4000);
            history.Add(new EloSnapshot
            {
                Points = points,
                RecordedAt = DateTime.UtcNow.AddDays(-day)
            });
        }

        history[^1] = new EloSnapshot { Points = currentElo, RecordedAt = DateTime.UtcNow };
        return history;
    }

    private static T Pick<T>() where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        return values[Rng.Next(values.Length)];
    }
}
