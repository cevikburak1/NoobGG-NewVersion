using NoobGg.Application.Common.Constants;
using NoobGg.Application.Features.Elo.Helpers;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Demo;

public sealed partial class CoherentDemoWorld
{
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        void Check(bool value, string message) { if (!value) errors.Add(message); }
        var users = Rows<User>(CollectionNames.Users).ToDictionary(x => x.Id);
        var games = Rows<Game>(CollectionNames.Games).ToDictionary(x => x.Id);
        var rooms = Rows<Room>(CollectionNames.Rooms).ToDictionary(x => x.Id);
        var guilds = Rows<Guild>(CollectionNames.Guilds).ToDictionary(x => x.Id);
        var gameProfiles = Rows<UserGameProfile>(CollectionNames.UserGameProfiles);
        var memberships = Rows<RoomMember>(CollectionNames.RoomMembers);
        var guildMembers = Rows<GuildMember>(CollectionNames.GuildMembers);
        bool HasGame(string u, string g) => gameProfiles.Any(p => p.UserId == u && p.GameId == g);
        bool IsMember(string u, string g) => guildMembers.Any(m => m.UserId == u && m.GuildId == g);
        bool IsStaff(string u, string g) => guildMembers.Any(m => m.UserId == u && m.GuildId == g && m.Role != GuildMemberRole.Member);
        var friends = Rows<Friendship>(CollectionNames.Friendships);
        bool AreFriends(string a, string b) => friends.Any(f => f.Status == FriendshipStatus.Accepted && ((f.RequesterId == a && f.AddresseeId == b) || (f.RequesterId == b && f.AddresseeId == a)));
        var blocks = Rows<Block>(CollectionNames.Blocks);
        bool Blocked(string a, string b) => blocks.Any(x => x.BlockerId == a && x.BlockedUserId == b || x.BlockerId == b && x.BlockedUserId == a);
        Check(users.Count == UserCount, "Exactly 200 demo users required");
        foreach (var pair in Collections)
            Check(pair.Value.Select(x => x.Id).Distinct().Count() == pair.Value.Count, $"Duplicate IDs in {pair.Key}");
        foreach (var user in users.Values)
        {
            Check(user.IsEmailVerified && user.IsProfileComplete && !user.IsBanned, $"Account prerequisites: {user.Id}");
            Check(Rows<UserProfile>(CollectionNames.UserProfiles).Count(p => p.UserId == user.Id && !string.IsNullOrWhiteSpace(p.Bio) && p.Availability != null) == 1, $"Complete profile: {user.Id}");
            Check(Rows<UserSettings>(CollectionNames.UserSettings).Count(p => p.UserId == user.Id) == 1, $"Settings: {user.Id}");
            Check(gameProfiles.Count(p => p.UserId == user.Id) == 2, $"Two game profiles: {user.Id}");
            Check(memberships.Any(p => p.UserId == user.Id) && guildMembers.Any(p => p.UserId == user.Id), $"Room/guild activity: {user.Id}");
            Check(friends.Any(p => p.Status == FriendshipStatus.Accepted && (p.RequesterId == user.Id || p.AddresseeId == user.Id)), $"Friends: {user.Id}");
            Check(Rows<Favorite>(CollectionNames.Favorites).Any(p => p.UserId == user.Id), $"Favorites: {user.Id}");
            Check(Rows<Conversation>(CollectionNames.Conversations).Any(p => p.Participant1Id == user.Id || p.Participant2Id == user.Id), $"Conversations: {user.Id}");
            Check(Rows<Notification>(CollectionNames.Notifications).Any(p => p.UserId == user.Id), $"Notifications: {user.Id}");
            Check(Rows<GameSession>(CollectionNames.GameSessions).Any(p => p.ParticipantIds.Contains(user.Id)), $"Session history: {user.Id}");
        }
        Check(gameProfiles.Select(p => (p.UserId, p.GameId)).Distinct().Count() == gameProfiles.Count, "Duplicate game profiles");
        foreach (var profile in gameProfiles)
        {
            Check(users.ContainsKey(profile.UserId) && games.ContainsKey(profile.GameId), $"Game profile FK: {profile.Id}");
            Check(profile.EloHistory.Count == 4 && profile.EloHistory.Last().Points == profile.EloPoints && profile.RankTier == EloCalculator.GetTier(profile.EloPoints), $"Elo history: {profile.Id}");
        }
        Check(friends.Select(f => string.Join('/', new[] { f.RequesterId, f.AddresseeId }.Order())).Distinct().Count() == friends.Count, "Duplicate friendship pair");
        foreach (var f in friends) Check(f.RequesterId != f.AddresseeId && users.ContainsKey(f.RequesterId) && users.ContainsKey(f.AddresseeId) && !Blocked(f.RequesterId, f.AddresseeId), $"Friendship prerequisites: {f.Id}");
        foreach (var b in blocks) Check(b.BlockerId != b.BlockedUserId && users.ContainsKey(b.BlockerId) && users.ContainsKey(b.BlockedUserId) && !AreFriends(b.BlockerId, b.BlockedUserId), $"Block relationship: {b.Id}");
        foreach (var f in Rows<Favorite>(CollectionNames.Favorites)) Check(AreFriends(f.UserId, f.FavoriteUserId) && !Blocked(f.UserId, f.FavoriteUserId), $"Favorite relationship: {f.Id}");
        foreach (var room in rooms.Values)
        {
            var members = memberships.Where(m => m.RoomId == room.Id).ToList();
            Check(members.Count == room.CurrentMemberCount && members.Count <= room.MaxMembers && members.Select(m => m.UserId).Distinct().Count() == members.Count, $"Room capacity: {room.Id}");
            Check(members.Count(m => m.Role == RoomMemberRole.Owner && m.UserId == room.CreatorId) == 1, $"Room owner: {room.Id}");
            Check(room.Status == RoomStatus.Open && room.CurrentMemberCount < room.MaxMembers, $"Room state: {room.Id}");
            var elo = gameProfiles.Where(p => p.GameId == room.GameId && members.Any(m => m.UserId == p.UserId)).ToList();
            Check(elo.Count == members.Count && (int)Math.Round(elo.Average(p => p.EloPoints)) == room.AverageElo, $"Room Elo/game prerequisites: {room.Id}");
            foreach (var member in members) Check(users.ContainsKey(member.UserId) && HasGame(member.UserId, room.GameId) && !Blocked(member.UserId, room.CreatorId), $"Room member prerequisites: {member.Id}");
        }
        foreach (var message in Rows<Message>(CollectionNames.Messages)) Check(rooms.ContainsKey(message.RoomId) && memberships.Any(m => m.RoomId == message.RoomId && m.UserId == message.SenderId && m.JoinedAt <= message.CreatedAt) && users.GetValueOrDefault(message.SenderId)?.Username == message.SenderUsername, $"Room message membership: {message.Id}");
        foreach (var invite in Rows<RoomInvite>(CollectionNames.RoomInvites))
        {
            Check(rooms.ContainsKey(invite.RoomId) && users.ContainsKey(invite.InvitedUserId) && memberships.Any(m => m.RoomId == invite.RoomId && m.UserId == invite.InviterId) && !Blocked(invite.InviterId, invite.InvitedUserId), $"Room invite FK/permission: {invite.Id}");
            Check(invite.Status != RoomInviteStatus.Pending || !memberships.Any(m => m.RoomId == invite.RoomId && m.UserId == invite.InvitedUserId), $"Already joined pending invite: {invite.Id}");
            Check(invite.Status != RoomInviteStatus.Accepted || memberships.Any(m => m.RoomId == invite.RoomId && m.UserId == invite.InvitedUserId), $"Accepted invite membership: {invite.Id}");
        }
        var directMessages = Rows<DirectMessage>(CollectionNames.DirectMessages);
        foreach (var conv in Rows<Conversation>(CollectionNames.Conversations))
        {
            var messages = directMessages.Where(m => m.ConversationId == conv.Id).OrderBy(m => m.CreatedAt).ToList();
            Check(AreFriends(conv.Participant1Id, conv.Participant2Id) && !Blocked(conv.Participant1Id, conv.Participant2Id) && messages.Count > 0, $"DM permission: {conv.Id}");
            Check(messages.All(m => m.SenderId == conv.Participant1Id || m.SenderId == conv.Participant2Id), $"DM sender: {conv.Id}");
            Check(conv.Participant1UnreadCount == messages.Count(m => m.SenderId == conv.Participant2Id && !m.IsRead) && conv.Participant2UnreadCount == messages.Count(m => m.SenderId == conv.Participant1Id && !m.IsRead), $"Unread counts: {conv.Id}");
            if (messages.Count > 0) Check(conv.LastMessageContent == messages.Last().Content && conv.LastMessageAt == messages.Last().CreatedAt && conv.LastMessageSenderId == messages.Last().SenderId, $"DM last message: {conv.Id}");
        }
        foreach (var guild in guilds.Values)
        {
            var members = guildMembers.Where(m => m.GuildId == guild.Id).ToList();
            Check(members.Count == guild.CurrentMemberCount && members.Count <= guild.MaxMembers && members.Select(m => m.UserId).Distinct().Count() == members.Count, $"Guild count: {guild.Id}");
            Check(members.Count(m => m.Role == GuildMemberRole.Owner && m.UserId == guild.CreatorId) == 1 && members.All(m => guild.GameIds.All(g => HasGame(m.UserId, g))), $"Guild membership prerequisites: {guild.Id}");
        }
        foreach (var chat in Rows<GuildChatMessage>(CollectionNames.GuildChatMessages)) Check(IsMember(chat.SenderId, chat.GuildId) && guildMembers.Any(m => m.GuildId == chat.GuildId && m.UserId == chat.SenderId && m.JoinedAt <= chat.CreatedAt), $"Guild chat permission: {chat.Id}");
        foreach (var e in Rows<GuildEvent>(CollectionNames.GuildEvents)) Check(IsStaff(e.CreatorId, e.GuildId) && e.StartsAt < e.EndsAt && e.GoingUserIds.All(u => IsMember(u, e.GuildId) && e.GameId != null && HasGame(u, e.GameId)), $"Guild event RSVP: {e.Id}");
        foreach (var invite in Rows<GuildInvite>(CollectionNames.GuildInvites)) Check(IsStaff(invite.InviterId, invite.GuildId) && users.ContainsKey(invite.InvitedUserId) && (invite.Status == GuildInviteStatus.Accepted ? IsMember(invite.InvitedUserId, invite.GuildId) : !IsMember(invite.InvitedUserId, invite.GuildId)), $"Guild invite permission/state: {invite.Id}");
        foreach (var request in Rows<GuildJoinRequest>(CollectionNames.GuildJoinRequests)) Check(users.ContainsKey(request.UserId) && (request.Status == GuildJoinRequestStatus.Approved ? request.ReviewedBy != null && IsStaff(request.ReviewedBy, request.GuildId) && IsMember(request.UserId, request.GuildId) : !IsMember(request.UserId, request.GuildId)), $"Guild request permission/state: {request.Id}");
        var posts = Rows<CommunityPost>(CollectionNames.CommunityPosts).ToDictionary(p => p.Id);
        var boards = Rows<CommunityBoard>(CollectionNames.CommunityBoards).ToDictionary(p => p.Id);
        var comments = Rows<CommunityComment>(CollectionNames.CommunityComments).ToDictionary(p => p.Id);
        var guides = Rows<Guide>(CollectionNames.Guides).ToDictionary(p => p.Id);
        var votes = Rows<ContentVote>(CollectionNames.ContentVotes);
        foreach (var board in boards.Values) Check(users.ContainsKey(board.CreatedByUserId) && posts.Values.Any(p => p.BoardId == board.Id), $"Board owner/content: {board.Id}");
        foreach (var post in posts.Values) Check(users.ContainsKey(post.AuthorId) && post.BoardId != null && boards.ContainsKey(post.BoardId) && (post.GameId == null || HasGame(post.AuthorId, post.GameId)) && post.CommentCount == comments.Values.Count(c => c.PostId == post.Id) && post.UpvoteCount == votes.Count(v => v.TargetType == ContentVoteTargetType.CommunityPost && v.TargetId == post.Id), $"Post references/counters: {post.Id}");
        foreach (var comment in comments.Values) Check(posts.ContainsKey(comment.PostId) && users.ContainsKey(comment.AuthorId) && comment.CreatedAt >= posts[comment.PostId].CreatedAt && comment.UpvoteCount == votes.Count(v => v.TargetType == ContentVoteTargetType.CommunityComment && v.TargetId == comment.Id), $"Comment references/counters: {comment.Id}");
        foreach (var guide in guides.Values) Check(HasGame(guide.AuthorId, guide.GameId) && guide.Status == GuideStatus.Published && guide.UpvoteCount == votes.Count(v => v.TargetType == ContentVoteTargetType.Guide && v.TargetId == guide.Id), $"Guide prerequisites/counters: {guide.Id}");
        Check(votes.Select(v => (v.UserId, v.TargetId, v.TargetType)).Distinct().Count() == votes.Count, "Duplicate content votes");
        foreach (var vote in votes) Check(users.ContainsKey(vote.UserId) && (vote.TargetType == ContentVoteTargetType.CommunityPost ? posts.ContainsKey(vote.TargetId) : vote.TargetType == ContentVoteTargetType.CommunityComment ? comments.ContainsKey(vote.TargetId) : guides.ContainsKey(vote.TargetId)), $"Vote target: {vote.Id}");
        var sessions = Rows<GameSession>(CollectionNames.GameSessions).ToDictionary(s => s.Id);
        foreach (var s in sessions.Values) Check(s.EndedAt <= Epoch && s.ParticipantIds.Contains(s.OwnerId) && s.ParticipantIds.Distinct().Count() == s.ParticipantIds.Count && s.ParticipantIds.All(u => HasGame(u, s.GameId)), $"Session prerequisites: {s.Id}");
        foreach (var f in Rows<SessionFeedback>(CollectionNames.SessionFeedback)) Check(sessions.TryGetValue(f.SessionId, out var s) && s.ParticipantIds.Contains(f.UserId) && f.CreatedAt >= s.EndedAt, $"Feedback participation: {f.Id}");
        foreach (var match in Rows<MatchResult>(CollectionNames.MatchResults)) Check(match.Player1Id != match.Player2Id && (match.WinnerId == match.Player1Id || match.WinnerId == match.Player2Id) && HasGame(match.Player1Id, match.GameId) && HasGame(match.Player2Id, match.GameId) && match.CreatedAt <= Epoch, $"Elo match prerequisites: {match.Id}");
        var entries = Rows<TournamentEntry>(CollectionNames.TournamentEntries);
        var matches = Rows<TournamentMatch>(CollectionNames.TournamentMatches);
        foreach (var t in Rows<Tournament>(CollectionNames.Tournaments))
        {
            var players = entries.Where(e => e.TournamentId == t.Id).ToList();
            Check(players.Count == t.CurrentParticipants && players.Count <= t.MaxParticipants && players.Select(e => e.ParticipantId).Distinct().Count() == players.Count && players.All(e => HasGame(e.ParticipantId, t.GameId) && e.CreatedAt <= t.RegistrationDeadline), $"Tournament registration: {t.Id}");
            Check(t.GuildId != null && IsStaff(t.OrganizerId, t.GuildId) && t.SeasonId != null && Rows<Season>(CollectionNames.Seasons).Any(s => s.Id == t.SeasonId && s.GameId == t.GameId), $"Tournament organizer/season: {t.Id}");
            foreach (var m in matches.Where(m => m.TournamentId == t.Id))
            {
                Check((m.Participant1Id == null || players.Any(e => e.ParticipantId == m.Participant1Id)) && (m.Participant2Id == null || players.Any(e => e.ParticipantId == m.Participant2Id)), $"Bracket participants: {m.Id}");
                Check(m.Status != TournamentMatchStatus.Completed || m.WinnerId != null && (m.WinnerId == m.Participant1Id || m.WinnerId == m.Participant2Id), $"Bracket result: {m.Id}");
                Check(m.NextMatchId == null || matches.Any(n => n.Id == m.NextMatchId && n.TournamentId == t.Id && n.Round == m.Round + 1 && (m.WinnerId == null || n.Participant1Id == m.WinnerId || n.Participant2Id == m.WinnerId)), $"Bracket advancement: {m.Id}");
            }
            if (t.Status == TournamentStatus.Completed) Check(players.Count(e => e.Placement == 1 && !e.IsEliminated) == 1 && matches.Where(m => m.TournamentId == t.Id).All(m => m.Status == TournamentMatchStatus.Completed), $"Completed tournament: {t.Id}");
        }
        foreach (var report in Rows<Report>(CollectionNames.Reports)) Check(users.ContainsKey(report.ReporterId) && users.ContainsKey(report.ReportedUserId) && report.ReporterId != report.ReportedUserId && (report.ReviewedBy == null || users.TryGetValue(report.ReviewedBy, out var reviewer) && reviewer.Role is UserRole.Admin or UserRole.Moderator), $"Report review authorization: {report.Id}");
        foreach (var n in Rows<Notification>(CollectionNames.Notifications)) Check(users.ContainsKey(n.UserId) && n.Data != null && !string.IsNullOrWhiteSpace(n.Body), $"Notification recipient: {n.Id}");
        return errors;
    }
}
