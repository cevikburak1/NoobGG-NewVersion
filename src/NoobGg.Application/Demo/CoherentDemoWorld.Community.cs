using NoobGg.Application.Common.Constants;
using NoobGg.Application.Features.Elo.Helpers;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Demo;

public sealed partial class CoherentDemoWorld
{
    private void BuildRoomsAndGuilds(List<User> users, List<Game> games)
    {
        var profiles = Rows<UserGameProfile>(CollectionNames.UserGameProfiles);
        for (var c = 0; c < 10; c++)
        {
            var cohort = users.Skip(c * 20).Take(20).ToList();
            var region = profiles.First(p => p.UserId == cohort[0].Id && p.GameId == games[c].Id).Region;
            var lang = region == Region.TR ? Language.Turkish : Language.English;
            var guild = Add(CollectionNames.Guilds, $"c{c}", new Guild { Name = GuildNames[c], Tag = $"NG{c:D2}", TagNormalized = $"NG{c:D2}",
                Description = $"{games[c].Name} için haftalık buluşmalar, rol paylaşımı ve yeni oyunculara destek. Akşam 19:30 oturumlarında birbirimizi tanıyoruz.",
                CreatorId = cohort[0].Id, IsPublic = true, Region = region, Language = lang, GameIds = [games[c].Id], MaxMembers = 50, CurrentMemberCount = 20 }, Epoch.AddDays(-40));
            for (var m = 0; m < 20; m++)
            {
                var joined = Epoch.AddDays(-35).AddMinutes(m);
                Add(CollectionNames.GuildMembers, $"{c}-{m}", new GuildMember { GuildId = guild.Id, UserId = cohort[m].Id,
                    Role = m == 0 ? GuildMemberRole.Owner : m is 5 or 6 ? GuildMemberRole.Admin : GuildMemberRole.Member, JoinedAt = joined }, joined);
                if (m > 0 && m % 2 == 0) Add(CollectionNames.GuildInvites, $"accepted-{c}-{m}", new GuildInvite { GuildId = guild.Id, InviterId = cohort[0].Id, InvitedUserId = cohort[m].Id,
                    Status = GuildInviteStatus.Accepted, RespondedAt = joined }, joined.AddHours(-1));
                else if (m > 0) Add(CollectionNames.GuildJoinRequests, $"approved-{c}-{m}", new GuildJoinRequest { GuildId = guild.Id, UserId = cohort[m].Id,
                    Message = "Akşam oturumlarına düzenli katılmak istiyorum.", Status = GuildJoinRequestStatus.Approved, ReviewedBy = cohort[0].Id, ReviewedAt = joined }, joined.AddHours(-2));
                for (var n = 0; n < 2; n++) Add(CollectionNames.GuildChatMessages, $"{c}-{m}-{n}", new GuildChatMessage { GuildId = guild.Id, SenderId = cohort[m].Id, SenderUsername = cohort[m].Username,
                    Body = n == 0 ? $"{games[c].Name} için bu akşam 19:30'da hazırım, rolüm {GameRoles[c][m % GameRoles[c].Length]}." : "Etkinlikte görüşürüz. Önce rolleri netleştirelim, herkesin rahat oynayacağı bir plan yapalım." }, Epoch.AddDays(-1).AddMinutes(m * 3 + n));
            }
            for (var e = 0; e < 3; e++)
            {
                var start = e == 0 ? Epoch.AddDays(-4) : Epoch.AddDays(e).Date.AddHours(19);
                Add(CollectionNames.GuildEvents, $"{c}-{e}", new GuildEvent { GuildId = guild.Id, CreatorId = cohort[0].Id, GameId = games[c].Id,
                    Title = $"{games[c].Name} — {(e == 0 ? "Geçen haftanın ekip oturumu" : e == 1 ? "Akşam takım buluşması" : "Yeni oyuncularla antrenman")}",
                    Description = "Rolleri başlangıçta paylaşacağız. İlk 15 dakika ısınma, ardından dengeli takımlar ve oturum sonu kısa değerlendirme.",
                    StartsAt = start, EndsAt = start.AddHours(2), GoingUserIds = cohort.Select(u => u.Id).ToList() }, start.AddDays(-3));
            }
            for (var g = 0; g < 5; g++)
            {
                var players = cohort.Skip(g * 4).Take(4).ToList();
                var elos = profiles.Where(p => players.Any(u => u.Id == p.UserId) && p.GameId == games[c].Id).ToList();
                var average = (int)Math.Round(elos.Average(p => p.EloPoints));
                var room = Add(CollectionNames.Rooms, $"{c}-{g}", new Room { Title = $"{games[c].Name} · {new[] { "Akşam Ekibi", "Isınma & Takım", "Rahat Oyun", "Rol Paylaşımı", "Davetli Antrenman" }[g]}",
                    Description = "İletişim odaklı bir ekip kuruyoruz. Oyun profiline uygun rolünü paylaş; yeni oyunculara yardımcı oluyoruz.", GameId = games[c].Id,
                    CreatorId = players[0].Id, IsPublic = g != 4, Region = region, Language = lang, MaxMembers = 5, CurrentMemberCount = 4, Status = RoomStatus.Open,
                    Tags = ["teamplay", g % 2 == 0 ? "casual" : "ranked"], AverageElo = average, AverageRankTier = EloCalculator.GetTier(average).ToString() }, Epoch.AddDays(-1));
                for (var m = 0; m < players.Count; m++)
                {
                    var membership = Add(CollectionNames.RoomMembers, $"{c}-{g}-{m}", new RoomMember { RoomId = room.Id, UserId = players[m].Id,
                        Role = m == 0 ? RoomMemberRole.Owner : RoomMemberRole.Member, JoinedAt = room.CreatedAt.AddMinutes(m * 2) }, room.CreatedAt.AddMinutes(m * 2));
                    if (m > 0) Add(CollectionNames.RoomInvites, $"accepted-{c}-{g}-{m}", new RoomInvite { RoomId = room.Id, InviterId = players[0].Id, InvitedUserId = players[m].Id,
                        Status = RoomInviteStatus.Accepted, RespondedAt = membership.JoinedAt }, room.CreatedAt.AddMinutes(m * 2 - 1));
                    Add(CollectionNames.RecentActivities, $"room-{players[m].Id}", new RecentActivity { UserId = players[m].Id, TargetId = room.Id, TargetType = RecentActivityTargetType.Room, SeenAt = Epoch.AddMinutes(-20) }, Epoch.AddMinutes(-20));
                    Add(CollectionNames.RecentActivities, $"player-{players[m].Id}", new RecentActivity { UserId = players[m].Id, TargetId = players[(m + 1) % 4].Id, TargetType = RecentActivityTargetType.Player, SeenAt = Epoch.AddMinutes(-25) }, Epoch.AddMinutes(-25));
                }
                for (var m = 0; m < 16; m++)
                {
                    var sender = players[m % 4];
                    var texts = new[] { $"{games[c].Name} için herkes hazır mı?", "Isınmaya başlayabiliriz, sesli iletişim kullanıyorum.", "Rolümü oyun profilime ekledim. Eksik role göre yardımcı olabilirim.", "Önce birlikte hareket edelim, oturum sonunda değerlendirme yaparız." };
                    Add(CollectionNames.Messages, $"{room.Id}-{m}", new Message { RoomId = room.Id, SenderId = sender.Id, SenderUsername = sender.Username, Content = texts[m % 4] }, Epoch.AddHours(-3).AddMinutes(m * 2));
                }
                for (var s = 0; s < 2; s++)
                {
                    var ended = Epoch.AddDays(-5 + s * 2).AddHours(-1);
                    var session = Add(CollectionNames.GameSessions, $"{c}-{g}-{s}", new GameSession { RoomId = Key($"historic-room-{c}-{g}-{s}"), RoomTitle = $"{games[c].Name} — tamamlanan ekip oturumu {s + 1}",
                        GameId = games[c].Id, OwnerId = players[0].Id, ParticipantIds = players.Select(p => p.Id).ToList(), Region = region, Language = lang, EndedAt = ended }, ended.AddHours(-2));
                    foreach (var player in players) Add(CollectionNames.SessionFeedback, $"{session.Id}-{player.Id}", new SessionFeedback { SessionId = session.Id, UserId = player.Id,
                        WouldPlayAgain = true, Tags = ["good-comms", "team-player"] }, ended.AddMinutes(20));
                }
            }
        }
        var guilds = Rows<Guild>(CollectionNames.Guilds); var rooms = Rows<Room>(CollectionNames.Rooms);
        for (var i = 0; i < UserCount; i++)
        {
            var c = i / 20; var targetGuild = guilds[(c + 1) % 10]; var invitee = users[i];
            if (i % 2 == 0)
            {
                var invite = Add(CollectionNames.GuildInvites, $"pending-{i}", new GuildInvite { GuildId = targetGuild.Id, InviterId = targetGuild.CreatorId, InvitedUserId = invitee.Id, Status = GuildInviteStatus.Pending }, Epoch.AddHours(-10));
                Notify(invitee.Id, NotificationType.GuildInvite, "Lonca davetin var", targetGuild.Name + " ekibi birlikte oynamak istiyor.", new() { ["guildId"] = targetGuild.Id, ["inviteId"] = invite.Id });
            }
            else
            {
                var join = Add(CollectionNames.GuildJoinRequests, $"pending-{i}", new GuildJoinRequest { GuildId = targetGuild.Id, UserId = invitee.Id,
                    Message = $"{games[(c + 1) % 10].Name} oyun profilim hazır. Hafta içi akşam oturumlarına katılabilirim.", Status = GuildJoinRequestStatus.Pending }, Epoch.AddHours(-10));
                Notify(targetGuild.CreatorId, NotificationType.GuildJoinRequestReceived, "Yeni katılım isteği", invitee.Username + " loncana katılmak istiyor.", new() { ["guildId"] = targetGuild.Id, ["joinRequestId"] = join.Id });
            }
            var targetRoom = rooms[c * 5 + (i % 20 / 4 + 1) % 5];
            var roomInvite = Add(CollectionNames.RoomInvites, $"pending-{i}", new RoomInvite { RoomId = targetRoom.Id, InviterId = targetRoom.CreatorId, InvitedUserId = invitee.Id, Status = RoomInviteStatus.Pending }, Epoch.AddMinutes(-45));
            Notify(invitee.Id, NotificationType.RoomInvite, "Oda davetin hazır", targetRoom.Title, new() { ["roomId"] = targetRoom.Id, ["inviteId"] = roomInvite.Id });
        }
    }

    private void BuildCommunity(List<User> users, List<Game> games)
    {
        var general = Add(CollectionNames.CommunityBoards, "general", new CommunityBoard { Name = "Oyuncu Meydanı", Slug = "general", Category = "General",
            Description = "Tanışma, oyun saatlerini planlama ve birlikte oynama kültürü.", CreatedByUserId = users[0].Id }, Epoch.AddDays(-60));
        for (var c = 0; c < 10; c++)
        {
            var cohort = users.Skip(c * 20).Take(20).ToList();
            var board = Add(CollectionNames.CommunityBoards, $"g{c}", new CommunityBoard { Name = games[c].Name + " Topluluğu", Slug = $"game-{c}", Category = games[c].Genres[0], GameId = games[c].Id,
                Description = games[c].Name + " için rol önerileri, ekip arayışları ve tecrübe paylaşımı.", CreatedByUserId = cohort[0].Id, CoverImageUrl = games[c].BackgroundImageUrl }, Epoch.AddDays(-50));
            for (var i = 0; i < 20; i++)
            for (var section = 0; section < 2; section++)
            {
                var role = GameRoles[c][i % GameRoles[c].Length]; var at = Epoch.AddDays(-6).AddHours(i * 2);
                var post = Add(CollectionNames.CommunityPosts, $"{c}-{i}-{section}", new CommunityPost { BoardId = section == 0 ? board.Id : general.Id,
                    BoardType = section == 0 ? CommunityBoardType.Game : CommunityBoardType.General, GameId = section == 0 ? games[c].Id : null, AuthorId = cohort[i].Id,
                    Title = section == 0 ? $"{games[c].Name}: {role} rolünde takım iletişimi — ekip notları {i + 1}" : $"{FirstNames[i]} ile akşam oyun planı — {games[c].Name}",
                    Slug = $"demo-topic-{c}-{i}-{section}", Category = section == 0 ? "Strategy" : "Squad Finder",
                    Content = $"{games[c].Name} oturumlarımızda {role} rolüne odaklanıyorum. Hafta içi 19:30–22:00 arasında oynayabilirim.\n\nÖnce kısa bir ısınma, ardından görev paylaşımı yapıyoruz. Yeni oyuncuların soru sormasını ve oturum sonunda yapıcı geri bildirim vermeyi önemsiyorum. Siz takım iletişimini nasıl planlıyorsunuz?",
                    CommentCount = 3, UpvoteCount = 3, LastActivityAt = at.AddMinutes(90), IsPinned = i == 0 && section == 0 }, at);
                for (var n = 1; n <= 3; n++)
                {
                    var author = cohort[(i + n) % 20];
                    var comment = Add(CollectionNames.CommunityComments, $"{post.Id}-{n}", new CommunityComment { PostId = post.Id, AuthorId = author.Id,
                        Content = new[] { "Rol dağılımını oyun başlamadan netleştirmek çok yardımcı oluyor. Ben de bu saatlerde uygunum.", "Son lonca etkinliğinde önce ısınma yaptık; iletişimimiz belirgin şekilde daha iyi oldu.", "Bir sonraki oturumda birlikte deneyelim. Profilimde oyun saatlerimi güncelledim." }[n - 1], UpvoteCount = 1 }, at.AddMinutes(n * 30));
                    Add(CollectionNames.ContentVotes, $"post-{post.Id}-{n}", new ContentVote { UserId = author.Id, TargetId = post.Id, TargetType = ContentVoteTargetType.CommunityPost }, at.AddMinutes(n * 15));
                    Add(CollectionNames.ContentVotes, $"comment-{comment.Id}", new ContentVote { UserId = cohort[i].Id, TargetId = comment.Id, TargetType = ContentVoteTargetType.CommunityComment }, at.AddMinutes(n * 30 + 5));
                }
            }
            for (var g = 0; g < 3; g++)
            {
                var guide = Add(CollectionNames.Guides, $"{c}-{g}", new Guide { AuthorId = cohort[g * 4 + 3].Id, GameId = games[c].Id, Title = $"{games[c].Name}: {new[] { "Takım iletişimi ve rol paylaşımı", "Yeni ekipler için antrenman planı", "Oturum sonrası yapıcı değerlendirme" }[g]}",
                    Content = $"# {games[c].Name} için ekip rehberi\n\n## Hazırlık\nOyun profilindeki rolünü ve saat uygunluğunu ekibinle paylaş.\n\n## Birlikte oyun\nİlk 15 dakika ısınma yapın; ardından {string.Join(", ", GameRoles[c].Take(3))} rollerini netleştirin.\n\n## Değerlendirme\nOturum sonrasında bir olumlu nokta ve geliştirebileceğiniz bir noktayı konuşun. Yeni oyunculara zaman tanıyın.",
                    CoverImageUrl = games[c].BackgroundImageUrl, Tags = ["teamplay", "communication", "practice"], UpvoteCount = 5, ViewCount = 5 }, Epoch.AddDays(-12 + g));
                for (var n = 0; n < 5; n++) Add(CollectionNames.ContentVotes, $"guide-{guide.Id}-{n}", new ContentVote { UserId = cohort[(g * 4 + 4 + n) % 20].Id, TargetId = guide.Id, TargetType = ContentVoteTargetType.Guide }, Epoch.AddDays(-6));
            }
            var reporter = cohort[6]; var target = cohort[15];
            var report = Add(CollectionNames.Reports, $"pending-{c}", new Report { ReporterId = reporter.Id, ReportedUserId = target.Id, TargetType = ReportTargetType.User,
                Reason = ReportReason.Spam, Description = "Demo moderasyon senaryosu: aynı ekip davetinin kısa aralıklarla tekrarlanması. İnceleme bekliyor.", Status = ReportStatus.Pending }, Epoch.AddHours(-8));
            var reviewed = Add(CollectionNames.Reports, $"reviewed-{c}", new Report { ReporterId = cohort[7].Id, ReportedUserId = cohort[16].Id, TargetType = ReportTargetType.User,
                Reason = ReportReason.Inappropriate, Description = "Demo senaryosu: oyun sonrası tartışmada uygunsuz dil bildirimi.", Status = ReportStatus.Resolved,
                ReviewedBy = users[2].Id, ReviewNote = "Kullanıcıya topluluk iletişim kuralları hatırlatıldı; uyarıyla kapatıldı.", ReviewedAt = Epoch.AddDays(-1) }, Epoch.AddDays(-2));
            Add(CollectionNames.Audits, $"report-{reviewed.Id}", new Audit { ActorId = users[2].Id, Action = AuditAction.UserWarned, TargetType = "User", TargetId = cohort[16].Id,
                Details = $"Demo raporu {reviewed.Id}: uyarı uygulandı." }, Epoch.AddDays(-1));
            Notify(cohort[7].Id, NotificationType.ReportResolved, "Bildirimin incelendi", "Topluluk bildirimin uyarıyla sonuçlandırıldı.", new() { ["reportId"] = reviewed.Id });
        }
    }
}
