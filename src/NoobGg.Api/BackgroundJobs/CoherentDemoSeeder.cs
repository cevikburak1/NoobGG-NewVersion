using System.Security;
using MongoDB.Bson;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Demo;
using NoobGg.Domain.Entities;

namespace NoobGg.Api.BackgroundJobs;

/// <summary>Opt-in development data only. Never clears a database or writes into an existing unrelated community.</summary>
public sealed class CoherentDemoSeeder(IMongoContext mongo, IPasswordHasher hasher, IConfiguration config,
    IWebHostEnvironment environment, ILogger<CoherentDemoSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        if (!environment.IsDevelopment() || !config.GetValue<bool>("DemoSeed:Enabled") || config["DemoSeed:Scenario"] != "coherent-200")
            throw new InvalidOperationException("The coherent-200 seed is development-only and must be explicitly enabled.");
        if (!mongo.Database.DatabaseNamespace.DatabaseName.StartsWith("noobgg_demo_200", StringComparison.Ordinal))
            throw new InvalidOperationException("Use a separate database starting with noobgg_demo_200; existing databases are never reset.");
        var markers = mongo.Database.GetCollection<BsonDocument>(CollectionNames.DemoSeedMarkers);
        var markerFilter = Builders<BsonDocument>.Filter.Eq("_id", CoherentDemoWorld.Version);
        var marker = await markers.Find(markerFilter).FirstOrDefaultAsync(ct);
        var users = mongo.GetCollection<User>(CollectionNames.Users);
        if (marker == null && await users.Find(_ => true).AnyAsync(ct))
            throw new InvalidOperationException("Refusing to mix demo users with existing data. Select an empty demo database.");
        await markers.UpdateOneAsync(markerFilter, Builders<BsonDocument>.Update
            .SetOnInsert("SeededAt", DateTime.UtcNow).SetOnInsert("State", "Building"), new UpdateOptions { IsUpsert = true }, ct);
        marker = await markers.Find(markerFilter).SingleAsync(ct);
        var plans = await mongo.GetCollection<SubscriptionPlan>(CollectionNames.SubscriptionPlans).Find(p => p.IsActive).ToListAsync(ct);
        var world = CoherentDemoWorld.Build(marker["SeededAt"].ToUniversalTime(), hasher.Hash("Test1234!"), plans);
        var errors = world.Validate();
        if (errors.Count > 0) throw new InvalidOperationException("Invalid demo world: " + string.Join("; ", errors.Take(20)));
        await WriteArtwork(world, ct);
        if (marker.GetValue("State", "").AsString == "Complete")
        {
            logger.LogInformation("Coherent demo seed already complete; user actions preserved and local artwork ensured.");
            return;
        }
        foreach (var collection in world.Collections)
        {
            var target = mongo.Database.GetCollection<BsonDocument>(collection.Key);
            var writes = collection.Value.Select(entity => (WriteModel<BsonDocument>)new ReplaceOneModel<BsonDocument>(
                Builders<BsonDocument>.Filter.Eq("_id", entity.Id), entity.ToBsonDocument(entity.GetType())) { IsUpsert = true }).ToList();
            if (writes.Count > 0) await target.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = true }, ct);
        }
        var actual = await users.CountDocumentsAsync(_ => true, cancellationToken: ct);
        if (actual != CoherentDemoWorld.UserCount) throw new InvalidOperationException($"Expected 200 demo users, found {actual}");
        var counts = new BsonDocument(world.Collections.Select(c => new BsonElement(c.Key, c.Value.Count)));
        await markers.UpdateOneAsync(markerFilter, Builders<BsonDocument>.Update.Set("State", "Complete").Set("CompletedAt", DateTime.UtcNow)
            .Set("ValidationErrors", 0).Set("Counts", counts), cancellationToken: ct);
        logger.LogInformation("Coherent demo ready: {Counts}. All references and business prerequisites validated.", counts.ToJson());
    }
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private async Task WriteArtwork(CoherentDemoWorld world, CancellationToken ct)
    {
        var root = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", "demo200");
        Directory.CreateDirectory(Path.Combine(root, "avatars")); Directory.CreateDirectory(Path.Combine(root, "games"));
        var colors = new[] { "#c5f277", "#bba4fb", "#8db4ff", "#70dbb4", "#edc77b" };
        var profiles = world.Rows<UserProfile>(CollectionNames.UserProfiles);
        for (var i = 0; i < profiles.Count; i++)
        {
            var initials = string.Concat(profiles[i].DisplayName.Split(' ').Select(s => s[0]));
            var file = Path.Combine(root, "avatars", $"{i}.svg");
            if (!File.Exists(file)) await File.WriteAllTextAsync(file, $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 120 120\"><rect width=\"120\" height=\"120\" rx=\"60\" fill=\"#202635\"/><circle cx=\"98\" cy=\"10\" r=\"70\" fill=\"{colors[i % colors.Length]}\" opacity=\".18\"/><text x=\"60\" y=\"74\" text-anchor=\"middle\" font-family=\"sans-serif\" font-size=\"36\" font-weight=\"700\" fill=\"{colors[i % colors.Length]}\">{SecurityElement.Escape(initials)}</text></svg>", ct);
        }
        var games = world.Rows<Game>(CollectionNames.Games);
        for (var i = 0; i < games.Count; i++)
        {
            var file = Path.Combine(root, "games", $"{i}.svg");
            if (!File.Exists(file)) await File.WriteAllTextAsync(file, $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 800 400\"><defs><linearGradient id=\"g\" x2=\"1\" y2=\"1\"><stop stop-color=\"#17202b\"/><stop offset=\"1\" stop-color=\"#0c0e14\"/></linearGradient></defs><rect width=\"800\" height=\"400\" fill=\"url(#g)\"/><g stroke=\"{colors[i % colors.Length]}\" fill=\"none\" opacity=\".3\"><circle cx=\"600\" cy=\"150\" r=\"135\"/><circle cx=\"600\" cy=\"150\" r=\"110\"/><path d=\"M0 400 270 0l260 400M300 400 570 0l260 400\"/></g><text x=\"40\" y=\"285\" fill=\"#f3f4f8\" font-family=\"sans-serif\" font-weight=\"700\" font-size=\"40\">{SecurityElement.Escape(games[i].Name)}</text><text x=\"42\" y=\"328\" fill=\"{colors[i % colors.Length]}\" font-family=\"sans-serif\" font-size=\"13\" letter-spacing=\"4\">NOOBGG · DEMO COMMUNITY</text></svg>", ct);
        }
    }
}
