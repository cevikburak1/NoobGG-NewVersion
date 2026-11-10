using System.Security.Cryptography;
using System.Text;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Features.Elo.Helpers;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;
using NoobGg.Domain.ValueObjects;

namespace NoobGg.Application.Demo;

/// <summary>A reproducible, explicitly synthetic community. All counters derive from real related records.</summary>
public sealed partial class CoherentDemoWorld
{
    public const string Version = "coherent-200-v1";
    public const int UserCount = 200;
    public Dictionary<string, List<BaseEntity>> Collections { get; } = [];
    public DateTime Epoch { get; }
    private CoherentDemoWorld(DateTime epoch) => Epoch = epoch;
    public List<T> Rows<T>(string collection) where T : BaseEntity => Collections.GetValueOrDefault(collection, []).Cast<T>().ToList();
    private T Add<T>(string collection, string key, T entity, DateTime? at = null) where T : BaseEntity
    {
        entity.Id = Key(collection + "/" + key);
        entity.CreatedAt = at ?? Epoch.AddDays(-7);
        entity.UpdatedAt = entity.CreatedAt;
        if (!Collections.TryGetValue(collection, out var rows)) Collections[collection] = rows = [];
        rows.Add(entity);
        return entity;
    }
    public static string Key(string value) => new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(Version + "/" + value))[..16]).ToString();

    private static readonly string[] GameNames = ["Counter-Strike 2", "Valorant", "League of Legends", "Dota 2", "Apex Legends", "Overwatch 2", "Rocket League", "Fortnite", "Destiny 2", "Diablo IV"];
    private static readonly string[][] GameRoles = [
        ["IGL", "Entry", "AWPer", "Support", "Lurker"], ["Duelist", "Initiator", "Controller", "Sentinel", "Flex"],
        ["Top", "Jungle", "Mid", "ADC", "Support"], ["Carry", "Mid", "Offlane", "Soft Support", "Hard Support"],
        ["Recon", "Assault", "Support", "Skirmisher", "Controller"], ["Tank", "Damage", "Support", "Flex"],
        ["Striker", "Defender", "Playmaker"], ["Builder", "Fragger", "Support", "IGL"],
        ["Titan", "Hunter", "Warlock"], ["Barbarian", "Sorcerer", "Rogue", "Necromancer", "Druid"]
    ];
    private static readonly string[] FirstNames = ["Deniz", "Ece", "Arda", "Selin", "Mert", "Ada", "Bora", "Elif", "Can", "Duru", "Emir", "İpek", "Alp", "Defne", "Eren", "Aslı", "Kaan", "Lara", "Ozan", "Mina"];
    private static readonly string[] LastNames = ["Aydın", "Kaya", "Demir", "Yıldız", "Arslan", "Aksoy", "Güneş", "Çelik", "Yılmaz", "Deniz"];
    private static readonly string[] GuildNames = ["Marmara Taktik", "Gece Vardiyası", "Vadi Ekibi", "Ancient Guardians", "Outlands Crew", "Payload Union", "Overtime Club", "Build Together", "Last City Fireteam", "Sanctuary Circle"];

    public static CoherentDemoWorld Build(DateTime now, string passwordHash, IReadOnlyList<SubscriptionPlan> plans)
    {
        var w = new CoherentDemoWorld(DateTime.SpecifyKind(now, DateTimeKind.Utc));
        var games = new List<Game>();
        for (var g = 0; g < 10; g++) games.Add(w.Add(CollectionNames.Games, $"g{g}", new Game {
            RawgId = -2000 - g, Name = GameNames[g], NameNormalized = GameNames[g].ToLowerInvariant(), Slug = $"demo-{g}",
            Description = $"{GameNames[g]} oyuncuları için takım bulma, rol paylaşımı, topluluk sohbetleri ve planlı oyun oturumları. Bu katalog kaydı demo senaryosuna aittir.",
            BackgroundImageUrl = $"/uploads/demo200/games/{g}.svg", ReleasedAt = g == 9 ? "2023-06-06" : "2020-01-01",
            Genres = [g == 2 || g == 3 ? "MOBA" : g == 6 ? "Sports" : g == 9 ? "Action RPG" : "Shooter"],
            Tags = ["teamplay", "multiplayer", "demo"], Platforms = ["PC"], IsMultiplayer = true,
            IsCoop = g >= 8, IsPvp = g < 9, IsFreeToPlay = g < 8, IsActive = true
        }, now.AddDays(-120)));
        var users = new List<User>();
        for (var i = 0; i < UserCount; i++)
        {
            var cohort = i / 20;
            var region = cohort % 3 == 0 ? Region.TR : cohort % 3 == 1 ? Region.EU : Region.NA;
            var language = cohort % 3 == 0 ? Language.Turkish : Language.English;
            var joined = now.AddDays(-90).AddMinutes(i);
            var user = w.Add(CollectionNames.Users, $"u{i}", new User {
                Username = $"Player_{i:D4}", Email = $"player{i:D4}@demo200.example.test", PasswordHash = passwordHash,
                Role = i < 2 ? UserRole.Admin : i < 4 ? UserRole.Moderator : UserRole.User,
                IsEmailVerified = true, IsProfileComplete = true, LastLoginAt = now.AddHours(-i % 12 - 1)
            }, joined);
            users.Add(user);
            w.Add(CollectionNames.UserProfiles, $"u{i}", new UserProfile {
                UserId = user.Id, DisplayName = $"{FirstNames[i % 20]} {LastNames[cohort]}", AvatarUrl = $"/uploads/demo200/avatars/{i}.svg",
                BannerUrl = $"/uploads/demo200/games/{cohort}.svg", Bio = $"{GameNames[cohort]} oynuyorum. {GameRoles[cohort][i % GameRoles[cohort].Length]} rolünde, iletişimi güçlü bir ekiple akşam oturumlarına katılıyorum.",
                Country = region == Region.TR ? "TR" : region == Region.EU ? "DE" : "US", Timezone = region == Region.TR ? "Europe/Istanbul" : region == Region.EU ? "Europe/Berlin" : "America/New_York",
                Availability = new Availability { Weekdays = new TimeSlot { From = "19:00", To = "23:00" }, Weekends = new TimeSlot { From = "14:00", To = "23:00" } }
            }, joined);
            w.Add(CollectionNames.UserSettings, $"u{i}", new UserSettings { UserId = user.Id, ProfileVisibility = ProfileVisibility.Public,
                DmPermission = i % 4 == 0 ? DmPermission.FriendsOnly : DmPermission.Everyone, ShowOnlineStatus = true, DefaultLookingForTeam = true }, joined);
            var tier = i == 6 ? SubscriptionTier.Pro : i % 5 < 2 ? SubscriptionTier.Free : i % 5 < 4 ? SubscriptionTier.Plus : SubscriptionTier.Pro;
            var plan = plans.Single(p => p.Tier == tier);
            if (tier != SubscriptionTier.Free)
            {
                w.Add(CollectionNames.UserSubscriptions, $"u{i}", new UserSubscription { UserId = user.Id, PlanId = plan.Id, Tier = tier,
                    StartDate = now.AddDays(-10), EndDate = now.AddDays(20), AutoRenew = false,
                    PaymentProvider = "Demo grant — no payment collected", ExternalSubscriptionId = $"demo-sub-{i:D4}" }, now.AddDays(-10));
                w.Add(CollectionNames.BillingOrders, $"u{i}", new BillingOrder { UserId = user.Id, PlanId = plan.Id,
                    Status = "Paid", ProviderEventId = $"DEMO-NO-CHARGE-{i:D4}", ExternalSubscriptionId = $"demo-sub-{i:D4}" }, now.AddDays(-10));
            }
            for (var slot = 0; slot < 2; slot++)
            {
                var game = (cohort + slot) % 10;
                var points = 1500 + (i % 5 - 2) * 60;
                w.Add(CollectionNames.UserGameProfiles, $"u{i}-g{game}", new UserGameProfile { UserId = user.Id, GameId = games[game].Id,
                    Role = GameRoles[game][i % GameRoles[game].Length], Region = region, Languages = new[] { language, Language.English }.Distinct().ToList(),
                    ExperienceLevel = (ExperienceLevel)(i % 4), CommunicationPreference = CommunicationPreference.Both, HoursPlayed = 120 + i * 7,
                    LookingForTeam = true, Note = $"{GameNames[game]} için planlı akşam oyunları ve saygılı iletişim.",
                    EloPoints = points, Rank = EloCalculator.GetTier(points).ToString(), RankTier = EloCalculator.GetTier(points),
                    EloHistory = [new EloSnapshot { Points = points, RecordedAt = now.AddDays(-30) }]
                }, joined.AddDays(1));
            }
        }
        w.BuildMatches(games);
        w.BuildSocial(users, games);
        w.BuildRoomsAndGuilds(users, games);
        w.BuildCommunity(users, games);
        w.BuildTournaments(users, games);
        return w;
    }

    private void BuildMatches(List<Game> games)
    {
        foreach (var game in games)
        {
            var profiles = Rows<UserGameProfile>(CollectionNames.UserGameProfiles).Where(p => p.GameId == game.Id).ToList();
            for (var round = 0; round < 3; round++)
            for (var n = 0; n < profiles.Count; n += 2)
            {
                var a = profiles[(n + round) % profiles.Count]; var b = profiles[(n + round + 1) % profiles.Count];
                var won = (n / 2 + round) % 2 == 0;
                var (da, db) = EloCalculator.Calculate(a.EloPoints, b.EloPoints, won);
                var at = Epoch.AddDays(-6 + round * 2).AddMinutes(n);
                Add(CollectionNames.MatchResults, $"{game.Id}-{round}-{n}", new MatchResult { GameId = game.Id,
                    Player1Id = a.UserId, Player2Id = b.UserId, WinnerId = won ? a.UserId : b.UserId,
                    Player1EloBefore = a.EloPoints, Player2EloBefore = b.EloPoints, Player1EloChange = da, Player2EloChange = db }, at);
                foreach (var (p, delta) in new[] { (a, da), (b, db) }) { p.EloPoints = Math.Max(0, p.EloPoints + delta); p.RankTier = EloCalculator.GetTier(p.EloPoints); p.Rank = p.RankTier.ToString(); p.EloHistory.Add(new EloSnapshot { Points = p.EloPoints, RecordedAt = at }); p.UpdatedAt = at; }
            }
        }
    }

    private void BuildSocial(List<User> users, List<Game> games)
    {
        var accepted = new HashSet<string>(); var conversations = new HashSet<string>();
        for (var i = 0; i < UserCount; i++)
        {
            var start = i / 20 * 20; var local = i % 20;
            for (var d = 1; d <= 4; d++)
            {
                var j = start + (local + d) % 20; var key = $"{Math.Min(i, j)}-{Math.Max(i, j)}";
                if (accepted.Add(key)) Add(CollectionNames.Friendships, key, new Friendship { RequesterId = users[Math.Min(i, j)].Id, AddresseeId = users[Math.Max(i, j)].Id,
                    Status = FriendshipStatus.Accepted, RespondedAt = Epoch.AddDays(-21) }, Epoch.AddDays(-22));
            }
            var pending = users[start + (local + 5) % 20];
            var request = Add(CollectionNames.Friendships, $"pending-{i}", new Friendship { RequesterId = users[i].Id, AddresseeId = pending.Id, Status = FriendshipStatus.Pending }, Epoch.AddHours(-6));
            Add(CollectionNames.Favorites, $"u{i}", new Favorite { UserId = users[i].Id, FavoriteUserId = users[start + (local + 1) % 20].Id });
            Add(CollectionNames.Blocks, $"u{i}", new Block { BlockerId = users[i].Id, BlockedUserId = users[start + (local + 9) % 20].Id }, Epoch.AddDays(-8));
            Notify(pending.Id, NotificationType.FriendRequest, "Yeni arkadaşlık isteği", $"{users[i].Username} birlikte oynamak istiyor.", new() { ["userId"] = users[i].Id, ["friendshipId"] = request.Id });
            Notify(users[i].Id, NotificationType.FriendAccepted, "Ekibin büyüdü", "Birlikte oynadığın oyuncular arkadaş listende.", new() { ["userId"] = users[start + (local + 1) % 20].Id });
            for (var d = 1; d <= 2; d++)
            {
                var j = start + (local + d) % 20; var key = $"{Math.Min(i, j)}-{Math.Max(i, j)}";
                if (!conversations.Add(key)) continue;
                var pair = new[] { users[i], users[j] }.OrderBy(u => u.Id, StringComparer.Ordinal).ToArray();
                var conv = Add(CollectionNames.Conversations, key, new Conversation { Participant1Id = pair[0].Id, Participant2Id = pair[1].Id }, Epoch.AddDays(-3));
                var texts = new[] { $"Akşam {games[i / 20].Name} için uygun musun?", "19:30 bana uyuyor. Loncanın etkinliğine de katıldım.", "Harika, rolleri oyun başlamadan odada paylaşalım.", "Sesli iletişim kullanabiliriz; yeni oyunculara da yardımcı olurum.", "Önce kısa bir ısınma yapalım, sonra takım olarak devam ederiz.", "Tamam, odada görüşürüz!" };
                for (var m = 0; m < texts.Length; m++)
                {
                    var sender = pair[m % 2]; var read = m < 4; var at = Epoch.AddHours(-2).AddMinutes(m * 3);
                    Add(CollectionNames.DirectMessages, $"{key}-{m}", new DirectMessage { ConversationId = conv.Id, SenderId = sender.Id,
                        SenderUsername = sender.Username, Content = texts[m], IsRead = read, ReadAt = read ? at.AddMinutes(1) : null }, at);
                    if (!read) { if (sender.Id == pair[0].Id) conv.Participant2UnreadCount++; else conv.Participant1UnreadCount++; }
                    conv.LastMessageContent = texts[m]; conv.LastMessageSenderId = sender.Id; conv.LastMessageAt = at; conv.UpdatedAt = at;
                }
                foreach (var recipient in pair) Notify(recipient.Id, NotificationType.DirectMessage, "Takım arkadaşından mesaj", games[i / 20].Name + " oturumunuzun planı hazır.", new() { ["conversationId"] = conv.Id, ["userId"] = pair.First(u => u.Id != recipient.Id).Id });
            }
        }
    }

    private void Notify(string userId, NotificationType type, string title, string body, Dictionary<string, string> data) =>
        Add(CollectionNames.Notifications, $"{userId}-{type}-{string.Join("|", data.OrderBy(p => p.Key).Select(p => p.Key + "=" + p.Value))}", new Notification { UserId = userId, Type = type, Title = title, Body = body, Data = data }, Epoch.AddMinutes(-30));
}
