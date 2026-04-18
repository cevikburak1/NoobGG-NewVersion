using MongoDB.Bson;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Api.BackgroundJobs;

/// <summary>
/// Seeds community posts, guides, votes, guild events, and tournaments using only IDs that already exist
/// in Users, Games, and Guilds/GuildMembers. Does not invent user accounts.
/// Idempotent via DemoSeedMarkers. Enable with CommunityFeaturesSeed:Enabled, DemoSeed:Enabled, or NOOBGG_COMMUNITY_SEED=1.
/// </summary>
public class CommunityFeaturesDemoSeeder : IHostedService
{
    private const string MarkerId = "communityFeaturesDemoV1";

    private readonly IMongoContext _mongo;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<CommunityFeaturesDemoSeeder> _logger;
    private static readonly Random Rng = new(20260417);

    public CommunityFeaturesDemoSeeder(
        IMongoContext mongo,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<CommunityFeaturesDemoSeeder> logger)
    {
        _mongo = mongo;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (!IsSeedEnabled())
        {
            _logger.LogInformation(
                "CommunityFeaturesDemoSeeder: skipped (set CommunityFeaturesSeed:Enabled=true, DemoSeed:Enabled=true, or NOOBGG_COMMUNITY_SEED=1)");
            return;
        }

        try
        {
            var markers = _mongo.Database.GetCollection<BsonDocument>(CollectionNames.DemoSeedMarkers);
            var boardsCol = _mongo.GetCollection<CommunityBoard>(CollectionNames.CommunityBoards);
            var postsCol = _mongo.GetCollection<CommunityPost>(CollectionNames.CommunityPosts);
            var force = IsForceEnabled();
            if (force)
                _logger.LogWarning("CommunityFeaturesDemoSeeder: FORCE — clear community/guides/votes/events/tournaments demo data first to avoid duplicates");

            if (!force)
            {
                if (await markers.Find(Builders<BsonDocument>.Filter.Eq("_id", MarkerId)).AnyAsync(ct))
                {
                    _logger.LogInformation("CommunityFeaturesDemoSeeder: skipped — marker {Marker} present", MarkerId);
                    return;
                }

                if (await postsCol.CountDocumentsAsync(FilterDefinition<CommunityPost>.Empty, cancellationToken: ct) > 0)
                {
                    _logger.LogInformation(
                        "CommunityFeaturesDemoSeeder: skipped — {Collection} already has documents (clear collection or use force + marker removal to re-seed)",
                        CollectionNames.CommunityPosts);
                    return;
                }
            }

            var usersCol = _mongo.GetCollection<User>(CollectionNames.Users);
            var userIds = await usersCol.Find(_ => true).Project(u => u.Id).ToListAsync(ct);
            if (userIds.Count < 3)
            {
                _logger.LogWarning(
                    "CommunityFeaturesDemoSeeder: skipped — need at least 3 users in DB (found {Count})", userIds.Count);
                return;
            }

            var gamesCol = _mongo.GetCollection<Game>(CollectionNames.Games);
            var games = await gamesCol.Find(g => g.IsActive).Limit(40).ToListAsync(ct);
            if (games.Count == 0)
                games = await gamesCol.Find(_ => true).Limit(40).ToListAsync(ct);
            if (games.Count == 0)
            {
                _logger.LogWarning("CommunityFeaturesDemoSeeder: skipped — no games in catalog");
                return;
            }

            _logger.LogInformation("CommunityFeaturesDemoSeeder: starting (users={Users}, games={Games})", userIds.Count, games.Count);

            var commentsCol = _mongo.GetCollection<CommunityComment>(CollectionNames.CommunityComments);
            var guidesCol = _mongo.GetCollection<Guide>(CollectionNames.Guides);
            var votesCol = _mongo.GetCollection<ContentVote>(CollectionNames.ContentVotes);
            var eventsCol = _mongo.GetCollection<GuildEvent>(CollectionNames.GuildEvents);
            var tournamentsCol = _mongo.GetCollection<Tournament>(CollectionNames.Tournaments);
            var entriesCol = _mongo.GetCollection<TournamentEntry>(CollectionNames.TournamentEntries);

            var shuffledUsers = userIds.OrderBy(_ => Rng.Next()).ToList();
            var gameIds = games.Select(g => g.Id).ToList();
            var defaultBoardOwnerId = shuffledUsers[0];

            var generalBoard = await EnsureBoardAsync(
                boardsCol,
                "general",
                "General Players Forum",
                "Matchups, squad building, hot takes, roster calls, and everything players want to debate outside a single game.",
                "General",
                defaultBoardOwnerId,
                null,
                "from-primary/35 via-primary/10 to-transparent",
                null,
                ct);

            var gameBoardMap = new Dictionary<string, CommunityBoard>();
            foreach (var game in games)
            {
                var gameBoard = await EnsureBoardAsync(
                    boardsCol,
                    game.Slug,
                    game.Name,
                    BuildGameBoardDescription(game),
                    "Game",
                    defaultBoardOwnerId,
                    game.Id,
                    "from-accent/30 via-info/10 to-transparent",
                    game.BackgroundImageUrl,
                    ct);
                gameBoardMap[game.Id] = gameBoard;
            }

            var generalTopics = new (string title, string body, string category)[]
            {
                ("Just hit a new PR in ranked — LFG for duos tonight!", "Just hit a new PR in ranked — LFG for duos tonight! Looking for someone who communicates well and doesn't tilt.", "Looking for Team"),
                ("Anyone else seeing weird queue times this week?", "Anyone else seeing weird queue times this week? My average wait went from 30s to 3 minutes overnight.", "Debate"),
                ("Tip: warm up in unranked before jumping into placements.", "Tip: warm up in unranked before jumping into placements. Seriously — two or three unranked games make a huge difference in reaction time.", "Strategy"),
                ("Shoutout to everyone in last night's community scrim.", "Shoutout to everyone in last night's community scrim. Great energy and surprisingly close games.", "Highlights"),
                ("Roadmap looks solid. What feature do you want next?", "Roadmap looks solid. What feature do you want next? Personally I'd love in-app tournament brackets.", "Debate"),
                ("Looking for a chill stack, voice optional.", "Looking for a chill stack, voice optional. I play evenings EU, mostly unranked or low-stakes comp.", "Looking for Team"),
            };

            var gameTopics = new (string title, string body, string category)[]
            {
                ("Patch dropped — what are you running first?", "Patch dropped — what are you running first? The balance changes look like they'll shake up the meta hard.", "Patch Talk"),
                ("This meta is wild. What's your counter pick?", "This meta is wild. What's your counter pick? I keep running into the same comp and can't find an answer.", "Meta"),
                ("Best settings for competitive? Still tweaking sens.", "Best settings for competitive? Still tweaking sens. Dropped my DPI but something feels off with the new patch.", "Strategy"),
                ("Finally unlocked the skin I've wanted forever.", "Finally unlocked the skin I've wanted forever. Took way too long but it was worth the grind.", "Highlights"),
                ("LFM for weekly guild event — DM me.", "LFM for weekly guild event — DM me. We run it every Saturday at 9 PM CET, all ranks welcome.", "Looking for Team"),
                ("Who's grinding the new season ladder?", "Who's grinding the new season ladder? Season reset always feels like a fresh start.", "Meta"),
                ("GGs to everyone in the tournament test bracket.", "GGs to everyone in the tournament test bracket. Some surprisingly close matches in round 2.", "Highlights"),
                ("Streaming later — drop by if you want co-op strats.", "Streaming later — drop by if you want co-op strats. I'll be testing the new duo comp.", "Looking for Team"),
                ("Close game yesterday — one more round next time!", "Close game yesterday — one more round next time! That final fight was insane.", "Highlights"),
            };

            var posts = new List<CommunityPost>();

            for (var i = 0; i < generalTopics.Length; i++)
            {
                var (title, body, cat) = generalTopics[i];
                var author = shuffledUsers[i % shuffledUsers.Count];
                var hoursAgo = Rng.Next(1, 160);
                var created = DateTime.UtcNow.AddHours(-hoursAgo);
                var lastActivity = created.AddMinutes(Rng.Next(10, hoursAgo * 30));
                if (lastActivity > DateTime.UtcNow) lastActivity = DateTime.UtcNow.AddMinutes(-Rng.Next(1, 20));

                posts.Add(new CommunityPost
                {
                    Id = Guid.NewGuid().ToString(),
                    AuthorId = author,
                    BoardId = generalBoard.Id,
                    BoardType = CommunityBoardType.General,
                    Category = cat,
                    Title = title,
                    Slug = BuildSlug(title),
                    GameId = null,
                    Content = body,
                    ImageUrl = null,
                    UpvoteCount = 0,
                    CommentCount = 0,
                    LastActivityAt = lastActivity,
                    CreatedAt = created,
                    UpdatedAt = created
                });
            }

            for (var i = 0; i < gameTopics.Length; i++)
            {
                var (title, body, cat) = gameTopics[i];
                var author = shuffledUsers[(i + generalTopics.Length) % shuffledUsers.Count];
                var gid = gameIds[i % gameIds.Count];
                var hoursAgo = Rng.Next(1, 200);
                var created = DateTime.UtcNow.AddHours(-hoursAgo);
                var lastActivity = created.AddMinutes(Rng.Next(10, hoursAgo * 25));
                if (lastActivity > DateTime.UtcNow) lastActivity = DateTime.UtcNow.AddMinutes(-Rng.Next(1, 30));
                if (!gameBoardMap.TryGetValue(gid, out var gameBoard))
                    continue;

                posts.Add(new CommunityPost
                {
                    Id = Guid.NewGuid().ToString(),
                    AuthorId = author,
                    BoardId = gameBoard.Id,
                    BoardType = CommunityBoardType.Game,
                    Category = cat,
                    Title = title,
                    Slug = BuildSlug(title),
                    GameId = gid,
                    Content = body,
                    ImageUrl = null,
                    UpvoteCount = 0,
                    CommentCount = 0,
                    LastActivityAt = lastActivity,
                    CreatedAt = created,
                    UpdatedAt = created
                });
            }

            await postsCol.InsertManyAsync(posts, new InsertManyOptions { IsOrdered = false }, ct);

            var comments = new List<CommunityComment>();
            var commentTexts = new[]
            {
                "Same here!",
                "Add me — I'm online after 8.",
                "Facts.",
                "Try lowering your DPI slightly.",
                "I'm down tomorrow.",
                "Thanks for the tip.",
            };

            foreach (var post in posts)
            {
                var n = Rng.Next(1, 4);
                for (var c = 0; c < n; c++)
                {
                    var author = shuffledUsers[(Rng.Next(shuffledUsers.Count) + c) % shuffledUsers.Count];
                    if (author == post.AuthorId && shuffledUsers.Count > 1)
                        author = shuffledUsers.First(u => u != post.AuthorId);

                    var cc = new CommunityComment
                    {
                        Id = Guid.NewGuid().ToString(),
                        PostId = post.Id,
                        AuthorId = author,
                        Content = commentTexts[Rng.Next(commentTexts.Length)],
                        UpvoteCount = 0,
                        CreatedAt = post.CreatedAt.AddMinutes(Rng.Next(5, 600)),
                        UpdatedAt = post.CreatedAt.AddMinutes(Rng.Next(5, 600))
                    };
                    comments.Add(cc);
                    post.CommentCount++;
                }
            }

            if (comments.Count > 0)
                await commentsCol.InsertManyAsync(comments, new InsertManyOptions { IsOrdered = false }, ct);

            foreach (var post in posts)
            {
                var lastComment = comments.Where(c => c.PostId == post.Id).MaxBy(c => c.CreatedAt);
                var lastAct = lastComment is not null && lastComment.CreatedAt > post.LastActivityAt
                    ? lastComment.CreatedAt
                    : post.LastActivityAt;

                await postsCol.UpdateOneAsync(
                    p => p.Id == post.Id,
                    Builders<CommunityPost>.Update
                        .Set(p => p.CommentCount, post.CommentCount)
                        .Set(p => p.LastActivityAt, lastAct),
                    cancellationToken: ct);
            }

            var votes = new List<ContentVote>();
            foreach (var post in posts)
            {
                var voters = shuffledUsers.Where(u => u != post.AuthorId).Take(Rng.Next(2, Math.Min(6, shuffledUsers.Count))).ToList();
                var up = 0;
                foreach (var vid in voters)
                {
                    votes.Add(new ContentVote
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = vid,
                        TargetId = post.Id,
                        TargetType = ContentVoteTargetType.CommunityPost,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-Rng.Next(10, 5000)),
                        UpdatedAt = DateTime.UtcNow
                    });
                    up++;
                }

                await postsCol.UpdateOneAsync(
                    p => p.Id == post.Id,
                    Builders<CommunityPost>.Update.Set(p => p.UpvoteCount, up),
                    cancellationToken: ct);
            }

            var guideTitles = new[]
            {
                "Beginner loadouts that still work in ranked",
                "Map control fundamentals — rotation timing",
                "Economy guide: when to save vs force",
                "Warm-up routine before competitive sessions",
                "Communication callouts — short and clear",
                "Sensitivity and aim: finding your baseline",
                "Teamfight positioning for carry players",
            };

            var guides = new List<Guide>();
            for (var g = 0; g < Math.Min(guideTitles.Length, 7); g++)
            {
                var author = shuffledUsers[(g + 3) % shuffledUsers.Count];
                var gameId = gameIds[g % gameIds.Count];
                var created = DateTime.UtcNow.AddDays(-Rng.Next(2, 40));
                guides.Add(new Guide
                {
                    Id = Guid.NewGuid().ToString(),
                    AuthorId = author,
                    GameId = gameId,
                    Title = guideTitles[g],
                    Content = "## Overview\n\nThis guide was seeded for demo purposes using **existing** accounts only.\n\n## Tips\n\n- Stay consistent with your routine.\n- Review replays weekly.\n- Communicate intent before commits.\n\nGood luck on the ladder!",
                    CoverImageUrl = null,
                    Tags = new List<string> { "guide", "competitive", "basics" },
                    Status = GuideStatus.Published,
                    UpvoteCount = 0,
                    ViewCount = Rng.Next(12, 400),
                    CreatedAt = created,
                    UpdatedAt = created
                });
            }

            await guidesCol.InsertManyAsync(guides, new InsertManyOptions { IsOrdered = false }, ct);

            foreach (var guide in guides)
            {
                var gv = shuffledUsers.Where(u => u != guide.AuthorId).Take(Rng.Next(1, 5)).ToList();
                var uc = 0;
                foreach (var vid in gv)
                {
                    votes.Add(new ContentVote
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = vid,
                        TargetId = guide.Id,
                        TargetType = ContentVoteTargetType.Guide,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-Rng.Next(20, 8000)),
                        UpdatedAt = DateTime.UtcNow
                    });
                    uc++;
                }

                await guidesCol.UpdateOneAsync(
                    x => x.Id == guide.Id,
                    Builders<Guide>.Update.Set(x => x.UpvoteCount, uc),
                    cancellationToken: ct);
            }

            if (votes.Count > 0)
                await votesCol.InsertManyAsync(votes, new InsertManyOptions { IsOrdered = false }, ct);

            var guildMembersCol = _mongo.GetCollection<GuildMember>(CollectionNames.GuildMembers);
            var guildsCol = _mongo.GetCollection<Guild>(CollectionNames.Guilds);
            var guildDocs = await guildsCol.Find(_ => true).Limit(30).ToListAsync(ct);
            var events = new List<GuildEvent>();
            foreach (var guild in guildDocs.Take(4))
            {
                var members = await guildMembersCol.Find(m => m.GuildId == guild.Id).Limit(20).ToListAsync(ct);
                if (members.Count == 0) continue;

                var creator = members.FirstOrDefault(m =>
                        m.Role == GuildMemberRole.Owner || m.Role == GuildMemberRole.Admin)
                    ?.UserId
                    ?? members[0].UserId;
                var gameForEvent = guild.GameIds.FirstOrDefault(gid => gameIds.Contains(gid))
                    ?? gameIds[Rng.Next(gameIds.Count)];

                var start1 = DateTime.UtcNow.AddDays(2).AddHours(18);
                events.Add(new GuildEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    GuildId = guild.Id,
                    CreatorId = creator,
                    Title = "Guild scrim night",
                    Description = "Internal scrims — all ranks welcome. Voice in guild Discord.",
                    StartsAt = start1,
                    EndsAt = start1.AddHours(2),
                    GameId = gameForEvent,
                    TournamentId = null,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow
                });

                var start2 = DateTime.UtcNow.AddDays(9).AddHours(19);
                events.Add(new GuildEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    GuildId = guild.Id,
                    CreatorId = creator,
                    Title = "Weekly review & VOD",
                    Description = "We review last week's matches and plan comps.",
                    StartsAt = start2,
                    EndsAt = start2.AddHours(1.5),
                    GameId = gameForEvent,
                    TournamentId = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            if (events.Count > 0)
                await eventsCol.InsertManyAsync(events, new InsertManyOptions { IsOrdered = false }, ct);

            var organizerId = shuffledUsers[0];
            var mainGameId = gameIds[0];
            var deadline = DateTime.UtcNow.AddDays(7);
            var maxP = 8;
            var totalRounds = (int)Math.Ceiling(Math.Log2(maxP));

            var tournament1 = new Tournament
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Community Cup (demo)",
                Description = "Open registration — seeded with existing players only.",
                GameId = mainGameId,
                OrganizerId = organizerId,
                GuildId = null,
                Format = TournamentFormat.SingleElimination,
                Status = TournamentStatus.Registration,
                MaxParticipants = maxP,
                CurrentParticipants = 0,
                RegistrationDeadline = deadline,
                StartsAt = deadline.AddDays(1),
                CurrentRound = 0,
                TotalRounds = totalRounds,
                PrizeBadges = new List<string> { "Champion", "Finalist", "Semi-finalist" },
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow
            };

            var entrants = shuffledUsers.Take(Math.Min(5, shuffledUsers.Count)).ToList();
            tournament1.CurrentParticipants = entrants.Count;

            await tournamentsCol.InsertOneAsync(tournament1, cancellationToken: ct);

            var seed = 1;
            foreach (var uid in entrants)
            {
                await entriesCol.InsertOneAsync(new TournamentEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    TournamentId = tournament1.Id,
                    ParticipantId = uid,
                    EntryType = TournamentEntryType.Player,
                    GuildId = null,
                    Seed = seed++,
                    IsEliminated = false,
                    Placement = 0,
                    EarnedBadges = [],
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }, cancellationToken: ct);
            }

            if (guildDocs.Count > 0)
            {
                var g0 = guildDocs[0];
                var gMembers = await guildMembersCol.Find(m => m.GuildId == g0.Id).Limit(16).ToListAsync(ct);
                if (gMembers.Count >= 2)
                {
                    var org = gMembers.FirstOrDefault(m => m.Role == GuildMemberRole.Owner)?.UserId ?? gMembers[0].UserId;
                    var tg = new Tournament
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = $"[{g0.Tag}] Guild showdown",
                        Description = "Guild-only bracket — demo data tied to this guild.",
                        GameId = g0.GameIds.FirstOrDefault(gid => gameIds.Contains(gid)) ?? mainGameId,
                        OrganizerId = org,
                        GuildId = g0.Id,
                        Format = TournamentFormat.SingleElimination,
                        Status = TournamentStatus.Registration,
                        MaxParticipants = 8,
                        CurrentParticipants = 0,
                        RegistrationDeadline = DateTime.UtcNow.AddDays(14),
                        StartsAt = DateTime.UtcNow.AddDays(15),
                        CurrentRound = 0,
                        TotalRounds = 3,
                        PrizeBadges = new List<string> { "Guild Champion" },
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        UpdatedAt = DateTime.UtcNow
                    };

                    var guildEntrants = gMembers.Select(m => m.UserId).Take(4).ToList();
                    tg.CurrentParticipants = guildEntrants.Count;
                    await tournamentsCol.InsertOneAsync(tg, cancellationToken: ct);
                    var s = 1;
                    foreach (var uid in guildEntrants)
                    {
                        await entriesCol.InsertOneAsync(new TournamentEntry
                        {
                            Id = Guid.NewGuid().ToString(),
                            TournamentId = tg.Id,
                            ParticipantId = uid,
                            EntryType = TournamentEntryType.Player,
                            GuildId = g0.Id,
                            Seed = s++,
                            IsEliminated = false,
                            Placement = 0,
                            EarnedBadges = [],
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }, cancellationToken: ct);
                    }
                }
            }

            await markers.InsertOneAsync(
                new BsonDocument
                {
                    { "_id", MarkerId },
                    { "completedAtUtc", DateTime.UtcNow },
                    { "environment", _environment.EnvironmentName },
                    { "posts", posts.Count },
                    { "guides", guides.Count },
                    { "guildEvents", events.Count }
                },
                cancellationToken: ct);

            _logger.LogInformation(
                "CommunityFeaturesDemoSeeder: completed — posts={Posts}, comments={Comments}, guides={Guides}, votes={Votes}, events={Events}, tournaments seeded",
                posts.Count, comments.Count, guides.Count, votes.Count, events.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CommunityFeaturesDemoSeeder: failed");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private bool IsSeedEnabled()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("NOOBGG_COMMUNITY_SEED"), "1", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(Environment.GetEnvironmentVariable("NOOBGG_DEMO_SEED"), "1", StringComparison.OrdinalIgnoreCase))
            return true;
        if (_configuration.GetValue("CommunityFeaturesSeed:Enabled", false))
            return true;
        return _configuration.GetValue("DemoSeed:Enabled", false);
    }

    private bool IsForceEnabled() =>
        string.Equals(Environment.GetEnvironmentVariable("NOOBGG_COMMUNITY_SEED_FORCE"), "1", StringComparison.OrdinalIgnoreCase)
        || _configuration.GetValue("CommunityFeaturesSeed:Force", false);

    private static async Task<CommunityBoard> EnsureBoardAsync(
        IMongoCollection<CommunityBoard> boards,
        string slug,
        string name,
        string description,
        string category,
        string createdByUserId,
        string? gameId,
        string accent,
        string? coverImageUrl,
        CancellationToken ct)
    {
        var existing = await boards.Find(b => b.Slug == slug).FirstOrDefaultAsync(ct);
        if (existing is not null)
            return existing;

        var board = new CommunityBoard
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Slug = slug,
            Description = description,
            Category = category,
            CreatedByUserId = createdByUserId,
            GameId = gameId,
            Accent = accent,
            CoverImageUrl = coverImageUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await boards.InsertOneAsync(board, cancellationToken: ct);
        return board;
    }

    private static string BuildGameBoardDescription(Game game)
    {
        var genre = game.Genres.FirstOrDefault();
        return genre is null
            ? "Strategy, meta shifts, squad requests, and patch reactions for this game."
            : $"{genre} tactics, player requests, patch reactions, and community intel for this game.";
    }

    private static string BuildSlug(string title)
    {
        var chars = title.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        var parts = new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0
            ? Guid.NewGuid().ToString("N")[..10]
            : string.Join("-", parts).Trim('-');
    }
}
