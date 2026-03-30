using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Features.Elo.Helpers;
using NoobGg.Domain.Entities;
using NoobGg.Domain.ValueObjects;

namespace NoobGg.Api.BackgroundJobs;

public class EloDataSeeder : IHostedService
{
    private readonly IMongoContext _mongo;
    private readonly ILogger<EloDataSeeder> _logger;
    private static readonly Random Rng = new(123);

    public EloDataSeeder(IMongoContext mongo, ILogger<EloDataSeeder> logger)
    {
        _mongo = mongo;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            var gpCol = _mongo.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
            var sample = await gpCol.Find(_ => true).Limit(1).FirstOrDefaultAsync(ct);
            if (sample is null) return;

            if (sample.EloPoints != 0 && sample.EloPoints != 1500)
            {
                _logger.LogInformation("EloDataSeeder: skipped — elo data already seeded");
                return;
            }

            var matchCol = _mongo.GetCollection<MatchResult>(CollectionNames.MatchResults);
            var matchCount = await matchCol.CountDocumentsAsync(FilterDefinition<MatchResult>.Empty, cancellationToken: ct);
            if (matchCount > 0)
            {
                _logger.LogInformation("EloDataSeeder: skipped — match results already exist");
                return;
            }

            _logger.LogInformation("EloDataSeeder: starting elo data seeding");
            await SeedEloData(ct);
            _logger.LogInformation("EloDataSeeder: completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EloDataSeeder: failed");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private async Task SeedEloData(CancellationToken ct)
    {
        var gpCol = _mongo.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var allProfiles = await gpCol.Find(_ => true).ToListAsync(ct);

        foreach (var profile in allProfiles)
        {
            var elo = GenerateNormalElo();
            var tier = EloCalculator.GetTier(elo);
            var history = GenerateEloHistory(elo);

            await gpCol.UpdateOneAsync(
                p => p.Id == profile.Id,
                Builders<UserGameProfile>.Update
                    .Set(p => p.EloPoints, elo)
                    .Set(p => p.RankTier, tier)
                    .Set(p => p.Rank, tier.ToString())
                    .Set(p => p.EloHistory, history),
                cancellationToken: ct);
        }

        _logger.LogInformation("Updated {Count} game profiles with elo data", allProfiles.Count);

        var profilesByGame = allProfiles.GroupBy(p => p.GameId).ToDictionary(g => g.Key, g => g.ToList());
        var matchResults = new List<MatchResult>();

        foreach (var (gameId, profiles) in profilesByGame)
        {
            if (profiles.Count < 2) continue;

            var matchCount = Math.Min(profiles.Count * 2, 200);
            for (var i = 0; i < matchCount; i++)
            {
                var p1 = profiles[Rng.Next(profiles.Count)];
                var p2 = profiles[Rng.Next(profiles.Count)];
                if (p1.UserId == p2.UserId) continue;

                var p1Won = Rng.NextDouble() < WinProbability(p1.EloPoints, p2.EloPoints);
                var (change1, change2) = EloCalculator.Calculate(p1.EloPoints, p2.EloPoints, p1Won);

                matchResults.Add(new MatchResult
                {
                    GameId = gameId,
                    Player1Id = p1.UserId,
                    Player2Id = p2.UserId,
                    WinnerId = p1Won ? p1.UserId : p2.UserId,
                    Player1EloBefore = p1.EloPoints,
                    Player2EloBefore = p2.EloPoints,
                    Player1EloChange = change1,
                    Player2EloChange = change2,
                    CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(0, 60)).AddHours(-Rng.Next(0, 24))
                });
            }
        }

        if (matchResults.Count > 0)
        {
            var matchCol = _mongo.GetCollection<MatchResult>(CollectionNames.MatchResults);
            await matchCol.InsertManyAsync(matchResults, new InsertManyOptions { IsOrdered = false }, ct);
            _logger.LogInformation("Seeded {Count} match results", matchResults.Count);
        }
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

    private static double WinProbability(int elo1, int elo2)
    {
        return 1.0 / (1.0 + Math.Pow(10, (elo2 - elo1) / 400.0));
    }
}
