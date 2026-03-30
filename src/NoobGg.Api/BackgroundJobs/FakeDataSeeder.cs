using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;
using NoobGg.Domain.ValueObjects;

namespace NoobGg.Api.BackgroundJobs;

/// <summary>
/// One-shot development seeder: inserts ~1000 users with profiles, game profiles,
/// rooms, room members, friendships, conversations, DMs, and notifications.
/// Skips silently when the users collection already has >=1000 documents.
/// All users share the password "Test1234!" and have verified emails.
/// </summary>
public class FakeDataSeeder : IHostedService
{
    private readonly IMongoContext _mongo;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<FakeDataSeeder> _logger;
    private static readonly Random Rng = new(42);

    public FakeDataSeeder(IMongoContext mongo, IPasswordHasher hasher, ILogger<FakeDataSeeder> logger)
    {
        _mongo = mongo;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            var usersCol = _mongo.GetCollection<User>(CollectionNames.Users);
            var existingCount = await usersCol.CountDocumentsAsync(FilterDefinition<User>.Empty, cancellationToken: ct);
            if (existingCount >= 1000)
            {
                _logger.LogInformation("FakeDataSeeder: skipped — already {Count} users in DB", existingCount);
                return;
            }

            _logger.LogInformation("FakeDataSeeder: starting — seeding fake data for development");
            await SeedAsync(ct);
            _logger.LogInformation("FakeDataSeeder: completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FakeDataSeeder: failed");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private async Task SeedAsync(CancellationToken ct)
    {
        var gamesCol = _mongo.GetCollection<Game>(CollectionNames.Games);
        var existingGames = await gamesCol.Find(_ => true).Limit(500).ToListAsync(ct);

        List<Game> games;
        if (existingGames.Count >= 10)
        {
            games = existingGames;
            _logger.LogInformation("Using {Count} existing games from DB", games.Count);
        }
        else
        {
            games = GenerateFakeGames();
            await gamesCol.InsertManyAsync(games, cancellationToken: ct);
            _logger.LogInformation("Inserted {Count} fake games", games.Count);
        }

        var passwordHash = _hasher.Hash("Test1234!");

        var users = new List<User>(1000);
        var profiles = new List<UserProfile>(1000);
        var settings = new List<UserSettings>(1000);
        var gameProfiles = new List<UserGameProfile>();

        var countries = new[] { "US", "TR", "DE", "FR", "BR", "JP", "KR", "GB", "CA", "AU", "RU", "PL", "ES", "IT", "NL", "SE", "NO", "FI", "DK", "CZ" };
        var timezones = new[] { "UTC", "Europe/Istanbul", "America/New_York", "Europe/Berlin", "Asia/Tokyo", "America/Sao_Paulo", "Europe/London", "Australia/Sydney" };
        var bios = new[]
        {
            "Competitive FPS player looking for a team",
            "Casual gamer, love RPGs and strategy games",
            "Hardcore MMORPG player since 2005",
            "Weekend warrior — mostly play co-op with friends",
            "Ranked grinder, aiming for top 500",
            "Just here to have fun and meet new people",
            "Pro-level aim, looking for scrims",
            "Support main, happy to fill any role",
            "Love tactical shooters and battle royales",
            "Old school gamer, still going strong",
            "Streaming on Twitch, looking for duo partners",
            "New to the scene, eager to learn",
            null
        };
        var ranks = new[] { "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Master", "Grandmaster", "Unranked" };
        var roles = new[] { "DPS", "Tank", "Support", "Flex", "Entry", "IGL", "Lurker", "AWPer", null };
        var tags = new[] { "competitive", "casual", "ranked", "scrims", "chill", "tryhard", "fun", "learning", "coaching", "duo", "squad", "5stack" };

        for (var i = 0; i < 1000; i++)
        {
            var userId = Guid.NewGuid().ToString();
            var createdAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 180)).AddHours(-Rng.Next(0, 24));

            users.Add(new User
            {
                Id = userId,
                Email = $"player{i:D4}@noobgg.test",
                Username = $"Player_{i:D4}",
                PasswordHash = passwordHash,
                Role = i < 2 ? UserRole.Admin : i < 5 ? UserRole.Moderator : UserRole.User,
                IsEmailVerified = true,
                IsBanned = false,
                LastLoginAt = DateTime.UtcNow.AddHours(-Rng.Next(0, 72)),
                IsProfileComplete = true,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });

            profiles.Add(new UserProfile
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                DisplayName = $"Player {i:D4}",
                AvatarUrl = null,
                Bio = bios[Rng.Next(bios.Length)],
                Country = countries[Rng.Next(countries.Length)],
                Timezone = timezones[Rng.Next(timezones.Length)],
                Availability = new Availability
                {
                    Weekdays = new TimeSlot { From = "18:00", To = "23:00" },
                    Weekends = new TimeSlot { From = "10:00", To = "02:00" }
                },
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });

            settings.Add(new UserSettings
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                ProfileVisibility = Rng.NextDouble() < 0.05 ? ProfileVisibility.Private : ProfileVisibility.Public,
                DmPermission = Pick<DmPermission>(),
                ShowOnlineStatus = Rng.NextDouble() > 0.1,
                DefaultLookingForTeam = Rng.NextDouble() < 0.4,
                NotifyFriendRequests = true,
                NotifyDirectMessages = true,
                NotifyRoomActivity = true,
                NotifySystemMessages = true,
                IsDeactivated = false,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });

            var numGames = Rng.Next(1, Math.Min(5, games.Count + 1));
            var userGames = games.OrderBy(_ => Rng.Next()).Take(numGames).ToList();
            foreach (var game in userGames)
            {
                var region = Pick<Region>();
                var langs = new List<Language> { Pick<Language>() };
                if (Rng.NextDouble() < 0.3) langs.Add(Pick<Language>());

                gameProfiles.Add(new UserGameProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    GameId = game.Id,
                    Rank = ranks[Rng.Next(ranks.Length)],
                    Role = roles[Rng.Next(roles.Length)],
                    Region = region,
                    Languages = langs.Distinct().ToList(),
                    ExperienceLevel = Pick<ExperienceLevel>(),
                    CommunicationPreference = Pick<CommunicationPreference>(),
                    HoursPlayed = Rng.Next(10, 5000),
                    LookingForTeam = Rng.NextDouble() < 0.45,
                    Note = Rng.NextDouble() < 0.3 ? "Looking for active teammates" : null,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                });
            }
        }

        var usersCol = _mongo.GetCollection<User>(CollectionNames.Users);
        var profilesCol = _mongo.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var settingsCol = _mongo.GetCollection<UserSettings>(CollectionNames.UserSettings);
        var gpCol = _mongo.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);

        await usersCol.InsertManyAsync(users, new InsertManyOptions { IsOrdered = false }, ct);
        await profilesCol.InsertManyAsync(profiles, new InsertManyOptions { IsOrdered = false }, ct);
        await settingsCol.InsertManyAsync(settings, new InsertManyOptions { IsOrdered = false }, ct);
        await gpCol.InsertManyAsync(gameProfiles, new InsertManyOptions { IsOrdered = false }, ct);
        _logger.LogInformation("Seeded {Users} users, {Profiles} profiles, {GP} game profiles",
            users.Count, profiles.Count, gameProfiles.Count);

        var userIds = users.Select(u => u.Id).ToList();
        var userMap = users.ToDictionary(u => u.Id);

        var rooms = new List<Room>();
        var roomMembers = new List<RoomMember>();
        var messages = new List<Message>();

        for (var r = 0; r < 150; r++)
        {
            var creatorId = userIds[Rng.Next(userIds.Count)];
            var game = games[Rng.Next(games.Count)];
            var maxMembers = Rng.Next(2, 11);
            var roomId = Guid.NewGuid().ToString();
            var createdAt = DateTime.UtcNow.AddDays(-Rng.Next(0, 30)).AddHours(-Rng.Next(0, 24));
            var region = Pick<Region>();
            var language = Pick<Language>();

            var memberCount = Rng.Next(1, maxMembers + 1);
            var status = memberCount >= maxMembers ? RoomStatus.Full
                : r < 10 ? RoomStatus.Closed
                : Rng.NextDouble() < 0.1 ? RoomStatus.InProgress
                : RoomStatus.Open;

            var roomTags = Enumerable.Range(0, Rng.Next(0, 4))
                .Select(_ => tags[Rng.Next(tags.Length)])
                .Distinct().ToList();

            rooms.Add(new Room
            {
                Id = roomId,
                Title = $"{game.Name} — {region} {(status == RoomStatus.Open ? "LFG" : "Team")} #{r + 1}",
                Description = Rng.NextDouble() < 0.7 ? $"Looking for players to play {game.Name}. {region} region, {language} preferred." : null,
                GameId = game.Id,
                CreatorId = creatorId,
                IsPublic = true,
                Region = region,
                Language = language,
                RankRange = Rng.NextDouble() < 0.4 ? new RankRange { Min = "Silver", Max = "Diamond" } : null,
                Tags = roomTags,
                MaxMembers = maxMembers,
                CurrentMemberCount = memberCount,
                Status = status,
                ClosedAt = status == RoomStatus.Closed ? createdAt.AddHours(Rng.Next(1, 48)) : null,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });

            roomMembers.Add(new RoomMember
            {
                Id = Guid.NewGuid().ToString(),
                RoomId = roomId,
                UserId = creatorId,
                Role = RoomMemberRole.Owner,
                JoinedAt = createdAt,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });

            var otherIds = userIds.Where(id => id != creatorId).OrderBy(_ => Rng.Next()).Take(memberCount - 1).ToList();
            foreach (var memberId in otherIds)
            {
                var joinedAt = createdAt.AddMinutes(Rng.Next(1, 1440));
                roomMembers.Add(new RoomMember
                {
                    Id = Guid.NewGuid().ToString(),
                    RoomId = roomId,
                    UserId = memberId,
                    Role = RoomMemberRole.Member,
                    JoinedAt = joinedAt,
                    CreatedAt = joinedAt,
                    UpdatedAt = joinedAt
                });
            }

            var allRoomMemberIds = new List<string> { creatorId };
            allRoomMemberIds.AddRange(otherIds);

            if (status != RoomStatus.Closed)
            {
                var msgCount = Rng.Next(0, 25);
                for (var m = 0; m < msgCount; m++)
                {
                    var senderId = allRoomMemberIds[Rng.Next(allRoomMemberIds.Count)];
                    var msgTime = createdAt.AddMinutes(Rng.Next(5, 10000));
                    messages.Add(new Message
                    {
                        Id = Guid.NewGuid().ToString(),
                        RoomId = roomId,
                        SenderId = senderId,
                        SenderUsername = userMap[senderId].Username,
                        Content = PickChatMessage(),
                        Type = MessageType.Text,
                        IsFiltered = false,
                        IsEdited = false,
                        IsDeleted = false,
                        CreatedAt = msgTime,
                        UpdatedAt = msgTime
                    });
                }
            }
        }

        var roomsCol = _mongo.GetCollection<Room>(CollectionNames.Rooms);
        var rmCol = _mongo.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        var msgCol = _mongo.GetCollection<Message>(CollectionNames.Messages);

        await roomsCol.InsertManyAsync(rooms, new InsertManyOptions { IsOrdered = false }, ct);
        await rmCol.InsertManyAsync(roomMembers, new InsertManyOptions { IsOrdered = false }, ct);
        if (messages.Count > 0)
            await msgCol.InsertManyAsync(messages, new InsertManyOptions { IsOrdered = false }, ct);
        _logger.LogInformation("Seeded {Rooms} rooms, {Members} memberships, {Msgs} room messages",
            rooms.Count, roomMembers.Count, messages.Count);

        var friendships = new List<Friendship>();
        var friendPairs = new HashSet<string>();
        for (var f = 0; f < 500; f++)
        {
            var a = userIds[Rng.Next(userIds.Count)];
            var b = userIds[Rng.Next(userIds.Count)];
            if (a == b) continue;
            var key = string.Compare(a, b, StringComparison.Ordinal) < 0 ? $"{a}_{b}" : $"{b}_{a}";
            if (!friendPairs.Add(key)) continue;

            var createdAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 90));
            var accepted = Rng.NextDouble() < 0.75;
            friendships.Add(new Friendship
            {
                Id = Guid.NewGuid().ToString(),
                RequesterId = a,
                AddresseeId = b,
                Status = accepted ? FriendshipStatus.Accepted : FriendshipStatus.Pending,
                RespondedAt = accepted ? createdAt.AddHours(Rng.Next(1, 48)) : null,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });
        }

        if (friendships.Count > 0)
        {
            var friendCol = _mongo.GetCollection<Friendship>(CollectionNames.Friendships);
            await friendCol.InsertManyAsync(friendships, new InsertManyOptions { IsOrdered = false }, ct);
            _logger.LogInformation("Seeded {Count} friendships", friendships.Count);
        }

        var conversations = new List<Conversation>();
        var directMessages = new List<DirectMessage>();
        var convPairs = new HashSet<string>();
        var dmChatMessages = new[]
        {
            "Hey, want to play later?",
            "GG last game!",
            "What rank are you?",
            "Sure, I'm down for a few rounds",
            "Nice play earlier!",
            "Do you have Discord?",
            "Let me know when you're online",
            "I'll be on around 8pm",
            "Added you as friend",
            "Looking for a 5 stack tonight",
            "That was a close match",
            "We should play together again",
            "I'm trying to improve my aim",
            "What sensitivity do you use?",
            "Wanna try ranked?",
            "I'm new to this game, any tips?"
        };

        for (var c = 0; c < 300; c++)
        {
            var p1 = userIds[Rng.Next(userIds.Count)];
            var p2 = userIds[Rng.Next(userIds.Count)];
            if (p1 == p2) continue;
            var sorted = string.Compare(p1, p2, StringComparison.Ordinal) < 0 ? (p1, p2) : (p2, p1);
            var key = $"{sorted.Item1}_{sorted.Item2}";
            if (!convPairs.Add(key)) continue;

            var convId = Guid.NewGuid().ToString();
            var convCreated = DateTime.UtcNow.AddDays(-Rng.Next(1, 60));
            var numMessages = Rng.Next(1, 12);
            var lastMsg = "";
            var lastSender = "";
            var lastTime = convCreated;

            for (var m = 0; m < numMessages; m++)
            {
                var sender = Rng.NextDouble() < 0.5 ? sorted.Item1 : sorted.Item2;
                var msgTime = lastTime.AddMinutes(Rng.Next(1, 300));
                var content = dmChatMessages[Rng.Next(dmChatMessages.Length)];
                lastMsg = content;
                lastSender = sender;
                lastTime = msgTime;

                directMessages.Add(new DirectMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    ConversationId = convId,
                    SenderId = sender,
                    SenderUsername = userMap[sender].Username,
                    Content = content,
                    IsRead = m < numMessages - 1 || Rng.NextDouble() < 0.6,
                    ReadAt = m < numMessages - 1 ? msgTime.AddMinutes(Rng.Next(1, 60)) : null,
                    CreatedAt = msgTime,
                    UpdatedAt = msgTime
                });
            }

            var unread1 = lastSender == sorted.Item2 && Rng.NextDouble() < 0.3 ? Rng.Next(1, 4) : 0;
            var unread2 = lastSender == sorted.Item1 && Rng.NextDouble() < 0.3 ? Rng.Next(1, 4) : 0;

            conversations.Add(new Conversation
            {
                Id = convId,
                Participant1Id = sorted.Item1,
                Participant2Id = sorted.Item2,
                LastMessageContent = lastMsg,
                LastMessageSenderId = lastSender,
                LastMessageAt = lastTime,
                Participant1UnreadCount = unread1,
                Participant2UnreadCount = unread2,
                CreatedAt = convCreated,
                UpdatedAt = lastTime
            });
        }

        var convCol = _mongo.GetCollection<Conversation>(CollectionNames.Conversations);
        var dmCol = _mongo.GetCollection<DirectMessage>(CollectionNames.DirectMessages);
        if (conversations.Count > 0)
            await convCol.InsertManyAsync(conversations, new InsertManyOptions { IsOrdered = false }, ct);
        if (directMessages.Count > 0)
            await dmCol.InsertManyAsync(directMessages, new InsertManyOptions { IsOrdered = false }, ct);
        _logger.LogInformation("Seeded {Convos} conversations, {DMs} direct messages",
            conversations.Count, directMessages.Count);

        var notifications = new List<Notification>();
        for (var n = 0; n < 400; n++)
        {
            var targetUser = userIds[Rng.Next(userIds.Count)];
            var notifType = Pick<NotificationType>();
            var createdAt = DateTime.UtcNow.AddDays(-Rng.Next(0, 30)).AddHours(-Rng.Next(0, 24));

            notifications.Add(new Notification
            {
                Id = Guid.NewGuid().ToString(),
                UserId = targetUser,
                Type = notifType,
                Title = notifType switch
                {
                    NotificationType.FriendRequest => "New friend request",
                    NotificationType.FriendAccepted => "Friend request accepted",
                    NotificationType.RoomInvite => "You have a room invite",
                    NotificationType.RoomJoined => "Someone joined your room",
                    NotificationType.DirectMessage => "New message",
                    _ => "Notification"
                },
                Body = notifType switch
                {
                    NotificationType.FriendRequest => $"Player_{Rng.Next(0, 1000):D4} sent you a friend request",
                    NotificationType.FriendAccepted => $"Player_{Rng.Next(0, 1000):D4} accepted your friend request",
                    NotificationType.RoomInvite => "You've been invited to join a room",
                    NotificationType.RoomJoined => $"Player_{Rng.Next(0, 1000):D4} joined your room",
                    NotificationType.DirectMessage => "You have a new direct message",
                    _ => "Check your notifications"
                },
                IsRead = Rng.NextDouble() < 0.6,
                ReadAt = Rng.NextDouble() < 0.6 ? createdAt.AddMinutes(Rng.Next(5, 300)) : null,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });
        }

        var notifCol = _mongo.GetCollection<Notification>(CollectionNames.Notifications);
        await notifCol.InsertManyAsync(notifications, new InsertManyOptions { IsOrdered = false }, ct);
        _logger.LogInformation("Seeded {Count} notifications", notifications.Count);

        var favorites = new List<Favorite>();
        var favPairs = new HashSet<string>();
        for (var f = 0; f < 200; f++)
        {
            var a = userIds[Rng.Next(userIds.Count)];
            var b = userIds[Rng.Next(userIds.Count)];
            if (a == b) continue;
            var key = $"{a}_{b}";
            if (!favPairs.Add(key)) continue;
            var createdAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 60));
            favorites.Add(new Favorite
            {
                Id = Guid.NewGuid().ToString(),
                UserId = a,
                FavoriteUserId = b,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });
        }

        if (favorites.Count > 0)
        {
            var favCol = _mongo.GetCollection<Favorite>(CollectionNames.Favorites);
            await favCol.InsertManyAsync(favorites, new InsertManyOptions { IsOrdered = false }, ct);
            _logger.LogInformation("Seeded {Count} favorites", favorites.Count);
        }
    }

    private static List<Game> GenerateFakeGames()
    {
        var fakeGames = new (string Name, string[] Genres, bool Mp, bool Coop, bool Pvp, bool F2P, int Meta)[]
        {
            ("Counter-Strike 2", new[] { "Shooter", "Action" }, true, false, true, true, 82),
            ("Valorant", new[] { "Shooter", "Action" }, true, false, true, true, 80),
            ("League of Legends", new[] { "MOBA", "Strategy" }, true, false, true, true, 78),
            ("Dota 2", new[] { "MOBA", "Strategy" }, true, false, true, true, 90),
            ("Overwatch 2", new[] { "Shooter", "Action" }, true, true, true, true, 79),
            ("Apex Legends", new[] { "Shooter", "Action" }, true, true, true, true, 88),
            ("Fortnite", new[] { "Shooter", "Action" }, true, true, true, true, 81),
            ("Minecraft", new[] { "Adventure", "Sandbox" }, true, true, false, false, 93),
            ("Rocket League", new[] { "Sports", "Racing" }, true, true, true, true, 86),
            ("PUBG: Battlegrounds", new[] { "Shooter", "Action" }, true, true, true, true, 76),
            ("World of Warcraft", new[] { "RPG", "MMORPG" }, true, true, true, false, 85),
            ("Final Fantasy XIV", new[] { "RPG", "MMORPG" }, true, true, false, false, 89),
            ("Destiny 2", new[] { "Shooter", "RPG" }, true, true, true, true, 83),
            ("Rainbow Six Siege", new[] { "Shooter", "Strategy" }, true, true, true, false, 79),
            ("Call of Duty: Warzone", new[] { "Shooter", "Action" }, true, true, true, true, 77),
            ("Elden Ring", new[] { "RPG", "Action" }, true, true, true, false, 96),
            ("Genshin Impact", new[] { "RPG", "Adventure" }, true, true, false, true, 84),
            ("Dead by Daylight", new[] { "Horror", "Action" }, true, false, true, false, 71),
            ("Rust", new[] { "Survival", "Action" }, true, false, true, false, 69),
            ("Escape from Tarkov", new[] { "Shooter", "RPG" }, true, false, true, false, 75),
            ("Sea of Thieves", new[] { "Adventure", "Action" }, true, true, true, false, 67),
            ("Diablo IV", new[] { "RPG", "Action" }, true, true, false, false, 77),
            ("Path of Exile", new[] { "RPG", "Action" }, true, true, false, true, 86),
            ("Palworld", new[] { "Survival", "Adventure" }, true, true, false, false, 72),
            ("Helldivers 2", new[] { "Shooter", "Action" }, true, true, false, false, 82),
        };

        return fakeGames.Select((g, i) => new Game
        {
            Id = Guid.NewGuid().ToString(),
            RawgId = 900_000 + i,
            Slug = g.Name.ToLowerInvariant().Replace(" ", "-").Replace(":", ""),
            Name = g.Name,
            NameNormalized = g.Name.Trim().ToLowerInvariant(),
            BackgroundImageUrl = $"https://placehold.co/600x400?text={Uri.EscapeDataString(g.Name)}",
            Rating = Math.Round(3.0 + Rng.NextDouble() * 2.0, 1),
            Metacritic = g.Meta,
            Genres = g.Genres.ToList(),
            Tags = g.Genres.ToList(),
            Platforms = new List<string> { "PC" },
            IsMultiplayer = g.Mp,
            IsCoop = g.Coop,
            IsPvp = g.Pvp,
            IsFreeToPlay = g.F2P,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-180),
            UpdatedAt = DateTime.UtcNow
        }).ToList();
    }

    private static T Pick<T>() where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        return values[Rng.Next(values.Length)];
    }

    private static string PickChatMessage()
    {
        var msgs = new[]
        {
            "Anyone up for ranked?",
            "GG WP!",
            "Let's go!",
            "Need one more for the team",
            "What's the plan?",
            "I'll play support",
            "Ready when you are",
            "Nice shot!",
            "Can someone switch roles?",
            "Let's push together",
            "One more game?",
            "Close round!",
            "We got this",
            "Good game everyone",
            "Who's calling strats?",
            "Need to warm up first",
            "I'm lagging a bit",
            "What server are we on?",
            "Let's practice that strat again",
            "gg ez",
            "wp team",
            "brb 2 min",
            "my mic is working now",
            "voice chat?",
        };
        return msgs[Rng.Next(msgs.Length)];
    }
}
