using MongoDB.Bson;
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
/// Idempotent demo pack: guilds, leaderboard-ready game profiles per game, and at least five open rooms per game.
/// Prefers existing (non-test) users in rooms; tops up the pool with verified test accounts when needed.
/// Enable with configuration DemoSeed:Enabled=true or environment NOOBGG_DEMO_SEED=1.
/// </summary>
public class GuildLeaderboardRoomDemoSeeder : IHostedService
{
    private const string MarkerId = "guildLeaderboardRoomDemoV3";
    private const int MinUserPool = 250;
    private const int RoomsPerGameTarget = 12;
    private const int LeaderboardProfilesPerGame = 200;
    private const int GuildTarget = 100;
    private const string DemoPassword = "Test1234!";

    private readonly IMongoContext _mongo;
    private readonly IPasswordHasher _hasher;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GuildLeaderboardRoomDemoSeeder> _logger;
    private static readonly Random Rng = new(20260416);

    public GuildLeaderboardRoomDemoSeeder(
        IMongoContext mongo,
        IPasswordHasher hasher,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<GuildLeaderboardRoomDemoSeeder> logger)
    {
        _mongo = mongo;
        _hasher = hasher;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (!IsDemoSeedEnabled())
        {
            _logger.LogInformation(
                "GuildLeaderboardRoomDemoSeeder: skipped (set DemoSeed:Enabled=true or NOOBGG_DEMO_SEED=1 to run)");
            return;
        }

        try
        {
            var markers = _mongo.Database.GetCollection<BsonDocument>(CollectionNames.DemoSeedMarkers);
            var forceMode = IsForceSeedEnabled();
            if (!forceMode && await markers.Find(Builders<BsonDocument>.Filter.Eq("_id", MarkerId)).AnyAsync(ct))
            {
                _logger.LogInformation("GuildLeaderboardRoomDemoSeeder: skipped — marker {Marker} present", MarkerId);
                return;
            }

            if (forceMode)
                _logger.LogWarning("GuildLeaderboardRoomDemoSeeder: FORCE mode enabled — bypassing marker check");

            _logger.LogInformation("GuildLeaderboardRoomDemoSeeder: starting");
            await SeedAsync(ct);
            await markers.InsertOneAsync(
                new BsonDocument
                {
                    { "_id", MarkerId },
                    { "completedAtUtc", DateTime.UtcNow },
                    { "environment", _environment.EnvironmentName }
                },
                cancellationToken: ct);
            _logger.LogInformation("GuildLeaderboardRoomDemoSeeder: completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GuildLeaderboardRoomDemoSeeder: failed");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private bool IsDemoSeedEnabled()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("NOOBGG_DEMO_SEED"), "1", StringComparison.OrdinalIgnoreCase))
            return true;
        return _configuration.GetValue("DemoSeed:Enabled", false);
    }

    private bool IsForceSeedEnabled()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("NOOBGG_DEMO_SEED_FORCE"), "1", StringComparison.OrdinalIgnoreCase))
            return true;
        return _configuration.GetValue("DemoSeed:Force", false);
    }

    private async Task SeedAsync(CancellationToken ct)
    {
        var gamesCol = _mongo.GetCollection<Game>(CollectionNames.Games);
        var allGames = await gamesCol.Find(_ => true).Limit(250).ToListAsync(ct);
        var games = allGames.Where(g => g.IsActive).ToList();
        if (games.Count == 0 && allGames.Count > 0)
        {
            games = allGames;
            _logger.LogWarning(
                "GuildLeaderboardRoomDemoSeeder: no games flagged IsActive — using all {Count} games from catalog",
                games.Count);
        }

        if (games.Count == 0)
        {
            _logger.LogWarning("GuildLeaderboardRoomDemoSeeder: games collection empty — nothing to seed");
            return;
        }

        var usersCol = _mongo.GetCollection<User>(CollectionNames.Users);
        var userDocs = await usersCol.Find(_ => true).ToListAsync(ct);
        var passwordHash = _hasher.Hash(DemoPassword);
        var createdUsers = await EnsureUserPoolAsync(userDocs, passwordHash, ct);
        if (createdUsers > 0)
            _logger.LogInformation("GuildLeaderboardRoomDemoSeeder: added {Count} filler users", createdUsers);

        userDocs = await usersCol.Find(_ => true).ToListAsync(ct);
        await EnsureUserSettingsForAllAsync(userDocs, ct);

        var userIds = userDocs.Select(u => u.Id).ToList();

        var orderedForRooms = userDocs
            .OrderBy(u => u.Email.Contains("noobgg.test", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ThenBy(_ => Rng.Next())
            .Select(u => u.Id)
            .ToList();

        var existingTags = await LoadExistingGuildTagsAsync(ct);
        var guilds = new List<Guild>();
        var guildMembers = new List<GuildMember>();
        for (var g = 0; g < GuildTarget; g++)
        {
            var ownerId = userIds[Rng.Next(userIds.Count)];
            var tag = NextUniqueGuildTag(existingTags);
            var gamePick = games.OrderBy(_ => Rng.Next()).Take(Math.Min(3, games.Count)).Select(x => x.Id).ToList();
            var region = PickEnum<Region>();
            var language = PickEnum<Language>();
            var createdAt = DateTime.UtcNow.AddDays(-Rng.Next(2, 120));
            var maxMembers = Rng.Next(24, 51);
            var memberTarget = Math.Min(maxMembers, Rng.Next(8, 19));
            var guildId = Guid.NewGuid().ToString();

            guilds.Add(new Guild
            {
                Id = guildId,
                Name = $"Demo Guild {g + 1:D2}",
                Tag = tag,
                TagNormalized = tag.ToLowerInvariant(),
                Description = "Auto-seeded demo guild for UI previews.",
                CreatorId = ownerId,
                IsPublic = true,
                Region = region,
                Language = language,
                GameIds = gamePick,
                MaxMembers = maxMembers,
                CurrentMemberCount = 0,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });

            var memberIds = new HashSet<string> { ownerId };
            guildMembers.Add(new GuildMember
            {
                Id = Guid.NewGuid().ToString(),
                GuildId = guildId,
                UserId = ownerId,
                Role = GuildMemberRole.Owner,
                JoinedAt = createdAt,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });

            foreach (var uid in userIds.OrderBy(_ => Rng.Next()))
            {
                if (memberIds.Count >= memberTarget) break;
                if (!memberIds.Add(uid)) continue;
                guildMembers.Add(new GuildMember
                {
                    Id = Guid.NewGuid().ToString(),
                    GuildId = guildId,
                    UserId = uid,
                    Role = GuildMemberRole.Member,
                    JoinedAt = createdAt.AddMinutes(Rng.Next(1, 2000)),
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                });
            }

            guilds[^1].CurrentMemberCount = memberIds.Count;
        }

        var guildCol = _mongo.GetCollection<Guild>(CollectionNames.Guilds);
        var gmCol = _mongo.GetCollection<GuildMember>(CollectionNames.GuildMembers);
        if (guilds.Count > 0)
        {
            await guildCol.InsertManyAsync(guilds, new InsertManyOptions { IsOrdered = false }, ct);
            await gmCol.InsertManyAsync(guildMembers, new InsertManyOptions { IsOrdered = false }, ct);
            _logger.LogInformation("GuildLeaderboardRoomDemoSeeder: seeded {Guilds} guilds, {Members} memberships",
                guilds.Count, guildMembers.Count);
        }

        var gpCol = _mongo.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var newProfiles = new List<UserGameProfile>();
        var shuffledUsers = userIds.OrderBy(_ => Rng.Next()).ToList();

        foreach (var game in games)
        {
            var picks = shuffledUsers.Take(Math.Min(LeaderboardProfilesPerGame, shuffledUsers.Count)).ToList();
            var existing = await gpCol
                .Find(gp => gp.GameId == game.Id && picks.Contains(gp.UserId))
                .Project(gp => gp.UserId)
                .ToListAsync(ct);
            var have = new HashSet<string>(existing);
            foreach (var uid in picks)
            {
                if (have.Contains(uid)) continue;
                var elo = GenerateSpreadElo();
                var tier = EloCalculator.GetTier(elo);
                newProfiles.Add(new UserGameProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = uid,
                    GameId = game.Id,
                    Rank = tier.ToString(),
                    Role = Rng.NextDouble() < 0.5 ? PickEnumString(new[] { "DPS", "Tank", "Support", "Flex" }) : null,
                    Region = PickEnum<Region>(),
                    Languages = BuildLanguages(),
                    ExperienceLevel = PickEnum<ExperienceLevel>(),
                    CommunicationPreference = PickEnum<CommunicationPreference>(),
                    HoursPlayed = Rng.Next(40, 6000),
                    LookingForTeam = Rng.NextDouble() < 0.35,
                    EloPoints = elo,
                    RankTier = tier,
                    EloHistory = BuildShortEloHistory(elo),
                    CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 90)),
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        if (newProfiles.Count > 0)
        {
            await gpCol.InsertManyAsync(newProfiles, new InsertManyOptions { IsOrdered = false }, ct);
            _logger.LogInformation("GuildLeaderboardRoomDemoSeeder: inserted {Count} game profiles for leaderboards",
                newProfiles.Count);
        }

        var roomsCol = _mongo.GetCollection<Room>(CollectionNames.Rooms);
        var rmCol = _mongo.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        var openRoomGameIds = await roomsCol
            .Find(r => r.Status != RoomStatus.Closed)
            .Project(r => r.GameId)
            .ToListAsync(ct);
        var counts = openRoomGameIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

        var newRooms = new List<Room>();
        var newRoomMembers = new List<RoomMember>();
        var roomTags = new[] { "ranked", "casual", "duo", "5stack", "scrims", "chill", "mic", "tryhard" };

        foreach (var game in games)
        {
            counts.TryGetValue(game.Id, out var n);
            var toAdd = Math.Max(0, RoomsPerGameTarget - n);
            for (var r = 0; r < toAdd; r++)
            {
                var creatorId = orderedForRooms[Rng.Next(orderedForRooms.Count)];
                var maxMembers = Rng.Next(3, 9);
                var extra = Math.Min(maxMembers - 1, Rng.Next(1, maxMembers));
                var memberIds = new List<string> { creatorId };
                foreach (var uid in orderedForRooms)
                {
                    if (memberIds.Count >= 1 + extra) break;
                    if (uid == creatorId) continue;
                    memberIds.Add(uid);
                }

                var roomId = Guid.NewGuid().ToString();
                var createdAt = DateTime.UtcNow.AddHours(-Rng.Next(1, 400));
                var region = PickEnum<Region>();
                var language = PickEnum<Language>();
                var tagSlice = Enumerable.Range(0, Rng.Next(1, 4))
                    .Select(_ => roomTags[Rng.Next(roomTags.Length)])
                    .Distinct()
                    .ToList();

                newRooms.Add(new Room
                {
                    Id = roomId,
                    Title = $"{game.Name} — demo LFG #{r + 1}",
                    Description = $"Open demo room for {game.Name}. Region {region}, voice optional.",
                    GameId = game.Id,
                    CreatorId = creatorId,
                    IsPublic = true,
                    Region = region,
                    Language = language,
                    RankRange = Rng.NextDouble() < 0.45 ? new RankRange { Min = "Silver", Max = "Diamond" } : null,
                    Tags = tagSlice,
                    MaxMembers = maxMembers,
                    CurrentMemberCount = memberIds.Count,
                    Status = RoomStatus.Open,
                    ClosedAt = null,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                });

                for (var mi = 0; mi < memberIds.Count; mi++)
                {
                    var uid = memberIds[mi];
                    var joined = createdAt.AddMinutes(mi == 0 ? 0 : Rng.Next(1, 120));
                    newRoomMembers.Add(new RoomMember
                    {
                        Id = Guid.NewGuid().ToString(),
                        RoomId = roomId,
                        UserId = uid,
                        Role = mi == 0 ? RoomMemberRole.Owner : RoomMemberRole.Member,
                        JoinedAt = joined,
                        CreatedAt = joined,
                        UpdatedAt = joined
                    });
                }
            }
        }

        if (newRooms.Count > 0)
        {
            await roomsCol.InsertManyAsync(newRooms, new InsertManyOptions { IsOrdered = false }, ct);
            await rmCol.InsertManyAsync(newRoomMembers, new InsertManyOptions { IsOrdered = false }, ct);
            _logger.LogInformation("GuildLeaderboardRoomDemoSeeder: created {Rooms} rooms, {Rm} memberships",
                newRooms.Count, newRoomMembers.Count);

            foreach (var room in newRooms)
                await RoomEloHelper.RecalculateAsync(_mongo, room.Id, ct);
        }
        else
        {
            _logger.LogInformation("GuildLeaderboardRoomDemoSeeder: no new rooms required (targets already met)");
        }
    }

    private async Task<int> EnsureUserPoolAsync(List<User> existing, string passwordHash, CancellationToken ct)
    {
        var need = Math.Max(0, MinUserPool - existing.Count);
        if (need == 0) return 0;

        var usersCol = _mongo.GetCollection<User>(CollectionNames.Users);
        var profilesCol = _mongo.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var settingsCol = _mongo.GetCollection<UserSettings>(CollectionNames.UserSettings);

        var users = new List<User>(need);
        var profiles = new List<UserProfile>(need);
        var settings = new List<UserSettings>(need);
        var now = DateTime.UtcNow;

        for (var i = 0; i < need; i++)
        {
            var id = Guid.NewGuid().ToString();
            var stamp = Guid.NewGuid().ToString("N")[..10];
            users.Add(new User
            {
                Id = id,
                Email = $"demofill_{stamp}_{i}@noobgg.test",
                Username = $"demofill_{stamp}_{i}",
                PasswordHash = passwordHash,
                Role = UserRole.User,
                IsEmailVerified = true,
                IsBanned = false,
                LastLoginAt = now.AddHours(-Rng.Next(0, 48)),
                IsProfileComplete = true,
                CreatedAt = now.AddDays(-Rng.Next(1, 60)),
                UpdatedAt = now
            });

            profiles.Add(new UserProfile
            {
                Id = Guid.NewGuid().ToString(),
                UserId = id,
                DisplayName = $"Demo Fill {i + 1}",
                Bio = "Seeded demo account",
                Country = "TR",
                Timezone = "Europe/Istanbul",
                Availability = new Availability
                {
                    Weekdays = new TimeSlot { From = "19:00", To = "23:00" },
                    Weekends = new TimeSlot { From = "12:00", To = "01:00" }
                },
                CreatedAt = now,
                UpdatedAt = now
            });

            settings.Add(new UserSettings
            {
                Id = Guid.NewGuid().ToString(),
                UserId = id,
                ProfileVisibility = ProfileVisibility.Public,
                DmPermission = DmPermission.Everyone,
                ShowOnlineStatus = true,
                DefaultLookingForTeam = true,
                NotifyFriendRequests = true,
                NotifyDirectMessages = true,
                NotifyRoomActivity = true,
                NotifySystemMessages = true,
                IsDeactivated = false,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await usersCol.InsertManyAsync(users, new InsertManyOptions { IsOrdered = false }, ct);
        await profilesCol.InsertManyAsync(profiles, new InsertManyOptions { IsOrdered = false }, ct);
        await settingsCol.InsertManyAsync(settings, new InsertManyOptions { IsOrdered = false }, ct);
        return need;
    }

    private async Task EnsureUserSettingsForAllAsync(IReadOnlyList<User> users, CancellationToken ct)
    {
        var settingsCol = _mongo.GetCollection<UserSettings>(CollectionNames.UserSettings);
        var existing = await settingsCol
            .Find(Builders<UserSettings>.Filter.In(s => s.UserId, users.Select(u => u.Id)))
            .Project(s => s.UserId)
            .ToListAsync(ct);
        var have = new HashSet<string>(existing);
        var now = DateTime.UtcNow;
        var toInsert = new List<UserSettings>();
        foreach (var u in users)
        {
            if (have.Contains(u.Id)) continue;
            toInsert.Add(new UserSettings
            {
                Id = Guid.NewGuid().ToString(),
                UserId = u.Id,
                ProfileVisibility = ProfileVisibility.Public,
                DmPermission = DmPermission.Everyone,
                ShowOnlineStatus = true,
                DefaultLookingForTeam = false,
                NotifyFriendRequests = true,
                NotifyDirectMessages = true,
                NotifyRoomActivity = true,
                NotifySystemMessages = true,
                IsDeactivated = false,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (toInsert.Count > 0)
        {
            await settingsCol.InsertManyAsync(toInsert, new InsertManyOptions { IsOrdered = false }, ct);
            _logger.LogInformation("GuildLeaderboardRoomDemoSeeder: backfilled {Count} user settings rows", toInsert.Count);
        }
    }

    private async Task<HashSet<string>> LoadExistingGuildTagsAsync(CancellationToken ct)
    {
        var guildCol = _mongo.GetCollection<Guild>(CollectionNames.Guilds);
        var tags = await guildCol.Find(_ => true).Project(g => g.Tag).ToListAsync(ct);
        return new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
    }

    private static string NextUniqueGuildTag(HashSet<string> tags)
    {
        for (var attempt = 0; attempt < 5000; attempt++)
        {
            var hex = Guid.NewGuid().ToString("N");
            var tag = ("Z" + hex[..5]).ToUpperInvariant();
            if (tag.Length > 6)
                tag = tag[..6];
            if (tags.Add(tag))
                return tag;
        }

        return ("Z" + Guid.NewGuid().ToString("N")[..5]).ToUpperInvariant()[..6];
    }

    private static int GenerateSpreadElo()
    {
        var bucket = Rng.Next(0, 10);
        var center = bucket switch
        {
            0 or 1 => 950,
            2 or 3 => 1250,
            4 or 5 => 1650,
            6 or 7 => 2100,
            8 => 2700,
            _ => 3200
        };
        return Math.Clamp(center + Rng.Next(-120, 121), 200, 4000);
    }

    private static List<EloSnapshot> BuildShortEloHistory(int currentElo)
    {
        var list = new List<EloSnapshot>();
        var p = currentElo + Rng.Next(-80, 80);
        p = Math.Clamp(p, 200, 4000);
        for (var day = 10; day >= 0; day--)
        {
            p = Math.Clamp(p + Rng.Next(-35, 36), 200, 4000);
            list.Add(new EloSnapshot { Points = p, RecordedAt = DateTime.UtcNow.AddDays(-day) });
        }

        list[^1] = new EloSnapshot { Points = currentElo, RecordedAt = DateTime.UtcNow };
        return list;
    }

    private static List<Language> BuildLanguages()
    {
        var a = PickEnum<Language>();
        var list = new List<Language> { a };
        if (Rng.NextDouble() < 0.35)
        {
            var b = PickEnum<Language>();
            if (b != a) list.Add(b);
        }

        return list;
    }

    private static T PickEnum<T>() where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        return values[Rng.Next(values.Length)];
    }

    private static string? PickEnumString(string[] items) => items[Rng.Next(items.Length)];
}
