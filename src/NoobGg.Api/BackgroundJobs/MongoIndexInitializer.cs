using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Domain.Entities;

namespace NoobGg.Api.BackgroundJobs;

public class MongoIndexInitializer : IHostedService
{
    private readonly IMongoContext _mongoContext;
    private readonly ILogger<MongoIndexInitializer> _logger;

    public MongoIndexInitializer(IMongoContext mongoContext, ILogger<MongoIndexInitializer> logger)
    {
        _mongoContext = mongoContext;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            await CreateAllIndexes(ct);
            _logger.LogInformation("MongoDB indexes ensured for all collections");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MongoDB indexes");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private async Task CreateAllIndexes(CancellationToken ct)
    {
        await CreateUserIndexes(ct);
        await CreateRefreshTokenIndexes(ct);
        await CreateUserProfileIndexes(ct);
        await CreateUserGameProfileIndexes(ct);
        await CreateGameIndexes(ct);
        await CreateRoomIndexes(ct);
        await CreateRoomMemberIndexes(ct);
        await CreateRoomInviteIndexes(ct);
        await CreateMessageIndexes(ct);
        await CreateFriendshipIndexes(ct);
        await CreateFavoriteIndexes(ct);
        await CreateBlockIndexes(ct);
        await CreateReportIndexes(ct);
        await CreateNotificationIndexes(ct);
        await CreateSubscriptionPlanIndexes(ct);
        await CreateUserSubscriptionIndexes(ct);
        await CreatePresenceIndexes(ct);
        await CreateAuditIndexes(ct);
        await CreateEmailVerificationTokenIndexes(ct);
        await CreateConversationIndexes(ct);
        await CreateDirectMessageIndexes(ct);
        await CreateUserSettingsIndexes(ct);
        await CreateGuildIndexes(ct);
        await CreateGuildMemberIndexes(ct);
        await CreateGuildInviteIndexes(ct);
        await CreateGuildJoinRequestIndexes(ct);
        await CreateMatchResultIndexes(ct);
        await CreateEloIndexes(ct);
        await CreateMatchQueueIndexes(ct);
        await CreateCommunityPostIndexes(ct);
        await CreateCommunityCommentIndexes(ct);
        await CreateGuideIndexes(ct);
        await CreateContentVoteIndexes(ct);
        await CreateGuildEventIndexes(ct);
        await CreateTournamentIndexes(ct);
        await CreateTournamentEntryIndexes(ct);
        await CreateTournamentMatchIndexes(ct);
        await CreateRecentActivityIndexes(ct);
    }

    private async Task CreateUserIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<User>(CollectionNames.Users);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true, Name = "idx_email_unique" }),
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Username),
                new CreateIndexOptions { Unique = true, Name = "idx_username_unique" }),
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Role),
                new CreateIndexOptions { Name = "idx_role" })
        ], ct);
    }

    private async Task CreateRefreshTokenIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<RefreshToken>(CollectionNames.RefreshTokens);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(t => t.Token),
                new CreateIndexOptions { Unique = true, Name = "idx_token_unique" }),
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(t => t.UserId),
                new CreateIndexOptions { Name = "idx_userId" }),
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(t => t.ExpiresAt),
                new CreateIndexOptions { Name = "idx_expiresAt", ExpireAfter = TimeSpan.FromDays(30) })
        ], ct);
    }

    private async Task CreateUserProfileIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<UserProfile>(
                Builders<UserProfile>.IndexKeys.Ascending(p => p.UserId),
                new CreateIndexOptions { Unique = true, Name = "idx_userId_unique" })
        ], ct);
    }

    private async Task CreateUserGameProfileIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<UserGameProfile>(
                Builders<UserGameProfile>.IndexKeys
                    .Ascending(p => p.UserId)
                    .Ascending(p => p.GameId),
                new CreateIndexOptions { Unique = true, Name = "idx_userId_gameId_unique" }),
            new CreateIndexModel<UserGameProfile>(
                Builders<UserGameProfile>.IndexKeys.Ascending(p => p.GameId),
                new CreateIndexOptions { Name = "idx_gameId" }),
            new CreateIndexModel<UserGameProfile>(
                Builders<UserGameProfile>.IndexKeys.Ascending(p => p.LookingForTeam),
                new CreateIndexOptions { Name = "idx_lookingForTeam" })
        ], ct);
    }

    private async Task CreateGameIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<Game>(CollectionNames.Games);

        await DropLegacyIndexes(col, ["idx_steamAppId_unique"], ct);

        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Game>(
                Builders<Game>.IndexKeys.Ascending(g => g.RawgId),
                new CreateIndexOptions { Unique = true, Name = "idx_rawgId_unique" }),
            new CreateIndexModel<Game>(
                Builders<Game>.IndexKeys.Ascending(g => g.NameNormalized),
                new CreateIndexOptions { Name = "idx_nameNormalized" }),
            new CreateIndexModel<Game>(
                Builders<Game>.IndexKeys
                    .Ascending(g => g.IsActive)
                    .Ascending(g => g.NameNormalized),
                new CreateIndexOptions { Name = "idx_active_nameNormalized" }),
            new CreateIndexModel<Game>(
                Builders<Game>.IndexKeys.Ascending(g => g.Genres),
                new CreateIndexOptions { Name = "idx_genres" }),
            new CreateIndexModel<Game>(
                Builders<Game>.IndexKeys.Ascending(g => g.IsMultiplayer),
                new CreateIndexOptions { Name = "idx_multiplayer" }),
            new CreateIndexModel<Game>(
                Builders<Game>.IndexKeys.Ascending(g => g.LastEnrichedAt),
                new CreateIndexOptions { Name = "idx_lastEnrichedAt" })
        ], ct);
    }

    private async Task CreateRoomIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Room>(
                Builders<Room>.IndexKeys
                    .Ascending(r => r.GameId)
                    .Ascending(r => r.Status),
                new CreateIndexOptions { Name = "idx_gameId_status" }),
            new CreateIndexModel<Room>(
                Builders<Room>.IndexKeys.Ascending(r => r.CreatorId),
                new CreateIndexOptions { Name = "idx_creatorId" }),
            new CreateIndexModel<Room>(
                Builders<Room>.IndexKeys.Ascending(r => r.Status),
                new CreateIndexOptions { Name = "idx_status" }),
            new CreateIndexModel<Room>(
                Builders<Room>.IndexKeys.Ascending(r => r.Region),
                new CreateIndexOptions { Name = "idx_region" }),
            new CreateIndexModel<Room>(
                Builders<Room>.IndexKeys
                    .Ascending(r => r.IsPublic)
                    .Ascending(r => r.Status),
                new CreateIndexOptions { Name = "idx_isPublic_status" }),
            new CreateIndexModel<Room>(
                Builders<Room>.IndexKeys.Ascending(r => r.Tags),
                new CreateIndexOptions { Name = "idx_tags" }),
            new CreateIndexModel<Room>(
                Builders<Room>.IndexKeys.Ascending(r => r.Language),
                new CreateIndexOptions { Name = "idx_language" })
        ], ct);
    }

    private async Task CreateRoomMemberIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<RoomMember>(
                Builders<RoomMember>.IndexKeys
                    .Ascending(m => m.RoomId)
                    .Ascending(m => m.UserId),
                new CreateIndexOptions { Unique = true, Name = "idx_roomId_userId_unique" }),
            new CreateIndexModel<RoomMember>(
                Builders<RoomMember>.IndexKeys.Ascending(m => m.UserId),
                new CreateIndexOptions { Name = "idx_userId" })
        ], ct);
    }

    private async Task CreateRoomInviteIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<RoomInvite>(CollectionNames.RoomInvites);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<RoomInvite>(
                Builders<RoomInvite>.IndexKeys
                    .Ascending(i => i.RoomId)
                    .Ascending(i => i.InvitedUserId)
                    .Ascending(i => i.Status),
                new CreateIndexOptions { Name = "idx_roomId_invitedUserId_status" }),
            new CreateIndexModel<RoomInvite>(
                Builders<RoomInvite>.IndexKeys
                    .Ascending(i => i.InvitedUserId)
                    .Ascending(i => i.Status),
                new CreateIndexOptions { Name = "idx_invitedUserId_status" }),
            new CreateIndexModel<RoomInvite>(
                Builders<RoomInvite>.IndexKeys.Ascending(i => i.RoomId),
                new CreateIndexOptions { Name = "idx_roomId" })
        ], ct);
    }

    private async Task CreateMessageIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<Message>(CollectionNames.Messages);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Message>(
                Builders<Message>.IndexKeys
                    .Ascending(m => m.RoomId)
                    .Descending(m => m.CreatedAt),
                new CreateIndexOptions { Name = "idx_roomId_createdAt" }),
            new CreateIndexModel<Message>(
                Builders<Message>.IndexKeys.Ascending(m => m.SenderId),
                new CreateIndexOptions { Name = "idx_senderId" }),
            new CreateIndexModel<Message>(
                Builders<Message>.IndexKeys
                    .Ascending(m => m.RoomId)
                    .Ascending(m => m.IsDeleted)
                    .Descending(m => m.CreatedAt),
                new CreateIndexOptions { Name = "idx_roomId_isDeleted_createdAt" })
        ], ct);
    }

    private async Task CreateFriendshipIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<Friendship>(CollectionNames.Friendships);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Friendship>(
                Builders<Friendship>.IndexKeys
                    .Ascending(f => f.RequesterId)
                    .Ascending(f => f.AddresseeId),
                new CreateIndexOptions { Unique = true, Name = "idx_requester_addressee_unique" }),
            new CreateIndexModel<Friendship>(
                Builders<Friendship>.IndexKeys.Ascending(f => f.AddresseeId),
                new CreateIndexOptions { Name = "idx_addresseeId" }),
            new CreateIndexModel<Friendship>(
                Builders<Friendship>.IndexKeys.Ascending(f => f.Status),
                new CreateIndexOptions { Name = "idx_status" })
        ], ct);
    }

    private async Task CreateFavoriteIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<Favorite>(CollectionNames.Favorites);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Favorite>(
                Builders<Favorite>.IndexKeys
                    .Ascending(f => f.UserId)
                    .Ascending(f => f.FavoriteUserId),
                new CreateIndexOptions { Unique = true, Name = "idx_userId_favoriteUserId_unique" }),
            new CreateIndexModel<Favorite>(
                Builders<Favorite>.IndexKeys.Ascending(f => f.UserId),
                new CreateIndexOptions { Name = "idx_userId" })
        ], ct);
    }

    private async Task CreateBlockIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Block>(
                Builders<Block>.IndexKeys
                    .Ascending(b => b.BlockerId)
                    .Ascending(b => b.BlockedUserId),
                new CreateIndexOptions { Unique = true, Name = "idx_blocker_blocked_unique" }),
            new CreateIndexModel<Block>(
                Builders<Block>.IndexKeys.Ascending(b => b.BlockerId),
                new CreateIndexOptions { Name = "idx_blockerId" }),
            new CreateIndexModel<Block>(
                Builders<Block>.IndexKeys.Ascending(b => b.BlockedUserId),
                new CreateIndexOptions { Name = "idx_blockedUserId" })
        ], ct);
    }

    private async Task CreateReportIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<Report>(CollectionNames.Reports);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Report>(
                Builders<Report>.IndexKeys.Ascending(r => r.Status),
                new CreateIndexOptions { Name = "idx_status" }),
            new CreateIndexModel<Report>(
                Builders<Report>.IndexKeys.Ascending(r => r.ReportedUserId),
                new CreateIndexOptions { Name = "idx_reportedUserId" }),
            new CreateIndexModel<Report>(
                Builders<Report>.IndexKeys.Ascending(r => r.ReporterId),
                new CreateIndexOptions { Name = "idx_reporterId" }),
            new CreateIndexModel<Report>(
                Builders<Report>.IndexKeys
                    .Ascending(r => r.TargetType)
                    .Ascending(r => r.Status)
                    .Descending(r => r.CreatedAt),
                new CreateIndexOptions { Name = "idx_targetType_status_createdAt" })
        ], ct);
    }

    private async Task CreateNotificationIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<Notification>(CollectionNames.Notifications);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Notification>(
                Builders<Notification>.IndexKeys
                    .Ascending(n => n.UserId)
                    .Descending(n => n.CreatedAt),
                new CreateIndexOptions { Name = "idx_userId_createdAt" }),
            new CreateIndexModel<Notification>(
                Builders<Notification>.IndexKeys
                    .Ascending(n => n.UserId)
                    .Ascending(n => n.IsRead),
                new CreateIndexOptions { Name = "idx_userId_isRead" })
        ], ct);
    }

    private async Task CreateSubscriptionPlanIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<SubscriptionPlan>(CollectionNames.SubscriptionPlans);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<SubscriptionPlan>(
                Builders<SubscriptionPlan>.IndexKeys.Ascending(p => p.Tier),
                new CreateIndexOptions { Unique = true, Name = "idx_tier_unique" }),
            new CreateIndexModel<SubscriptionPlan>(
                Builders<SubscriptionPlan>.IndexKeys.Ascending(p => p.IsActive),
                new CreateIndexOptions { Name = "idx_isActive" })
        ], ct);
    }

    private async Task CreateUserSubscriptionIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<UserSubscription>(CollectionNames.UserSubscriptions);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<UserSubscription>(
                Builders<UserSubscription>.IndexKeys.Ascending(s => s.UserId),
                new CreateIndexOptions { Name = "idx_userId" }),
            new CreateIndexModel<UserSubscription>(
                Builders<UserSubscription>.IndexKeys
                    .Ascending(s => s.UserId)
                    .Ascending(s => s.Status),
                new CreateIndexOptions { Name = "idx_userId_status" }),
            new CreateIndexModel<UserSubscription>(
                Builders<UserSubscription>.IndexKeys
                    .Ascending(s => s.UserId)
                    .Ascending(s => s.Tier),
                new CreateIndexOptions { Name = "idx_userId_tier" })
        ], ct);
    }

    private async Task CreatePresenceIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<Presence>(CollectionNames.Presences);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Presence>(
                Builders<Presence>.IndexKeys.Ascending(p => p.UserId),
                new CreateIndexOptions { Unique = true, Name = "idx_userId_unique" }),
            new CreateIndexModel<Presence>(
                Builders<Presence>.IndexKeys.Ascending(p => p.Status),
                new CreateIndexOptions { Name = "idx_status" })
        ], ct);
    }

    private async Task CreateAuditIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<Audit>(CollectionNames.Audits);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Audit>(
                Builders<Audit>.IndexKeys.Ascending(a => a.ActorId),
                new CreateIndexOptions { Name = "idx_actorId" }),
            new CreateIndexModel<Audit>(
                Builders<Audit>.IndexKeys
                    .Ascending(a => a.TargetType)
                    .Ascending(a => a.TargetId),
                new CreateIndexOptions { Name = "idx_targetType_targetId" }),
            new CreateIndexModel<Audit>(
                Builders<Audit>.IndexKeys.Descending(a => a.CreatedAt),
                new CreateIndexOptions { Name = "idx_createdAt" })
        ], ct);
    }

    private async Task CreateEmailVerificationTokenIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<EmailVerificationToken>(CollectionNames.EmailVerificationTokens);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<EmailVerificationToken>(
                Builders<EmailVerificationToken>.IndexKeys.Ascending(t => t.Token),
                new CreateIndexOptions { Unique = true, Name = "idx_token_unique" }),
            new CreateIndexModel<EmailVerificationToken>(
                Builders<EmailVerificationToken>.IndexKeys.Ascending(t => t.UserId),
                new CreateIndexOptions { Name = "idx_userId" }),
            new CreateIndexModel<EmailVerificationToken>(
                Builders<EmailVerificationToken>.IndexKeys.Ascending(t => t.ExpiresAt),
                new CreateIndexOptions { Name = "idx_expiresAt", ExpireAfter = TimeSpan.FromDays(7) })
        ], ct);
    }

    private async Task CreateConversationIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<Conversation>(CollectionNames.Conversations);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Conversation>(
                Builders<Conversation>.IndexKeys
                    .Ascending(c => c.Participant1Id)
                    .Ascending(c => c.Participant2Id),
                new CreateIndexOptions { Unique = true, Name = "idx_participants_unique" }),
            new CreateIndexModel<Conversation>(
                Builders<Conversation>.IndexKeys.Ascending(c => c.Participant2Id),
                new CreateIndexOptions { Name = "idx_participant2Id" }),
            new CreateIndexModel<Conversation>(
                Builders<Conversation>.IndexKeys.Descending(c => c.LastMessageAt),
                new CreateIndexOptions { Name = "idx_lastMessageAt" })
        ], ct);
    }

    private async Task CreateDirectMessageIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<DirectMessage>(CollectionNames.DirectMessages);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<DirectMessage>(
                Builders<DirectMessage>.IndexKeys
                    .Ascending(m => m.ConversationId)
                    .Descending(m => m.CreatedAt),
                new CreateIndexOptions { Name = "idx_conversationId_createdAt" }),
            new CreateIndexModel<DirectMessage>(
                Builders<DirectMessage>.IndexKeys.Ascending(m => m.SenderId),
                new CreateIndexOptions { Name = "idx_senderId" })
        ], ct);
    }

    private async Task CreateUserSettingsIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<UserSettings>(CollectionNames.UserSettings);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<UserSettings>(
                Builders<UserSettings>.IndexKeys.Ascending(s => s.UserId),
                new CreateIndexOptions { Unique = true, Name = "idx_userId_unique" })
        ], ct);
    }

    private async Task CreateGuildIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<Guild>(CollectionNames.Guilds);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Guild>(
                Builders<Guild>.IndexKeys.Ascending(g => g.Tag),
                new CreateIndexOptions { Unique = true, Name = "idx_tag_unique" }),
            new CreateIndexModel<Guild>(
                Builders<Guild>.IndexKeys.Ascending(g => g.CreatorId),
                new CreateIndexOptions { Name = "idx_creatorId" }),
            new CreateIndexModel<Guild>(
                Builders<Guild>.IndexKeys.Ascending(g => g.IsPublic),
                new CreateIndexOptions { Name = "idx_isPublic" }),
            new CreateIndexModel<Guild>(
                Builders<Guild>.IndexKeys.Ascending(g => g.Region),
                new CreateIndexOptions { Name = "idx_region" }),
            new CreateIndexModel<Guild>(
                Builders<Guild>.IndexKeys.Ascending(g => g.Language),
                new CreateIndexOptions { Name = "idx_language" }),
            new CreateIndexModel<Guild>(
                Builders<Guild>.IndexKeys.Ascending(g => g.GameIds),
                new CreateIndexOptions { Name = "idx_gameIds" })
        ], ct);
    }

    private async Task CreateGuildMemberIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<GuildMember>(CollectionNames.GuildMembers);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<GuildMember>(
                Builders<GuildMember>.IndexKeys
                    .Ascending(m => m.GuildId)
                    .Ascending(m => m.UserId),
                new CreateIndexOptions { Unique = true, Name = "idx_guildId_userId_unique" }),
            new CreateIndexModel<GuildMember>(
                Builders<GuildMember>.IndexKeys.Ascending(m => m.UserId),
                new CreateIndexOptions { Name = "idx_userId" }),
            new CreateIndexModel<GuildMember>(
                Builders<GuildMember>.IndexKeys.Ascending(m => m.GuildId),
                new CreateIndexOptions { Name = "idx_guildId" })
        ], ct);
    }

    private async Task CreateGuildInviteIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<GuildInvite>(CollectionNames.GuildInvites);

        await DropLegacyIndexes(col, ["idx_guildInvitedUserId_status"], ct);

        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<GuildInvite>(
                Builders<GuildInvite>.IndexKeys
                    .Ascending(i => i.GuildId)
                    .Ascending(i => i.InvitedUserId)
                    .Ascending(i => i.Status),
                new CreateIndexOptions { Name = "idx_guildId_invitedUserId_status" }),
            new CreateIndexModel<GuildInvite>(
                Builders<GuildInvite>.IndexKeys
                    .Ascending(i => i.InvitedUserId)
                    .Ascending(i => i.Status),
                new CreateIndexOptions { Name = "idx_invitedUserId_status" }),
            new CreateIndexModel<GuildInvite>(
                Builders<GuildInvite>.IndexKeys.Ascending(i => i.GuildId),
                new CreateIndexOptions { Name = "idx_guildId" })
        ], ct);
    }

    private async Task CreateGuildJoinRequestIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<GuildJoinRequest>(CollectionNames.GuildJoinRequests);
        await DropLegacyIndexes(col, ["idx_guildJoin_guildId_userId_status"], ct);

        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<GuildJoinRequest>(
                Builders<GuildJoinRequest>.IndexKeys
                    .Ascending(r => r.GuildId)
                    .Ascending(r => r.UserId)
                    .Ascending(r => r.Status),
                new CreateIndexOptions { Name = "idx_guildId_userId_status" }),
            new CreateIndexModel<GuildJoinRequest>(
                Builders<GuildJoinRequest>.IndexKeys
                    .Ascending(r => r.GuildId)
                    .Ascending(r => r.Status),
                new CreateIndexOptions { Name = "idx_guildId_status" }),
            new CreateIndexModel<GuildJoinRequest>(
                Builders<GuildJoinRequest>.IndexKeys.Ascending(r => r.UserId),
                new CreateIndexOptions { Name = "idx_userId" })
        ], ct);
    }

    private async Task CreateMatchResultIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<MatchResult>(CollectionNames.MatchResults);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<MatchResult>(
                Builders<MatchResult>.IndexKeys
                    .Ascending(m => m.GameId)
                    .Descending(m => m.CreatedAt),
                new CreateIndexOptions { Name = "idx_gameId_createdAt" }),
            new CreateIndexModel<MatchResult>(
                Builders<MatchResult>.IndexKeys.Ascending(m => m.Player1Id),
                new CreateIndexOptions { Name = "idx_player1Id" }),
            new CreateIndexModel<MatchResult>(
                Builders<MatchResult>.IndexKeys.Ascending(m => m.Player2Id),
                new CreateIndexOptions { Name = "idx_player2Id" })
        ], ct);
    }

    private async Task CreateEloIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<UserGameProfile>(
                Builders<UserGameProfile>.IndexKeys
                    .Ascending(p => p.GameId)
                    .Descending(p => p.EloPoints),
                new CreateIndexOptions { Name = "idx_gameId_eloPoints" })
        ], ct);
    }

    private async Task CreateMatchQueueIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<MatchQueueEntry>(CollectionNames.MatchQueueEntries);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<MatchQueueEntry>(
                Builders<MatchQueueEntry>.IndexKeys
                    .Ascending(e => e.UserId)
                    .Descending(e => e.UpdatedAt),
                new CreateIndexOptions { Name = "idx_userId_updatedAt" }),
            new CreateIndexModel<MatchQueueEntry>(
                Builders<MatchQueueEntry>.IndexKeys
                    .Ascending(e => e.Status)
                    .Ascending(e => e.GameId)
                    .Ascending(e => e.Region)
                    .Ascending(e => e.Language)
                    .Ascending(e => e.CreatedAt),
                new CreateIndexOptions { Name = "idx_status_game_region_lang_created" })
        ], ct);
    }

    private async Task CreateCommunityPostIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<CommunityPost>(CollectionNames.CommunityPosts);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<CommunityPost>(
                Builders<CommunityPost>.IndexKeys.Ascending(p => p.GameId).Descending(p => p.CreatedAt),
                new CreateIndexOptions { Name = "idx_gameId_createdAt" }),
            new CreateIndexModel<CommunityPost>(
                Builders<CommunityPost>.IndexKeys.Ascending(p => p.BoardType).Descending(p => p.LastActivityAt),
                new CreateIndexOptions { Name = "idx_boardType_lastActivityAt" }),
            new CreateIndexModel<CommunityPost>(
                Builders<CommunityPost>.IndexKeys
                    .Ascending(p => p.BoardType)
                    .Ascending(p => p.GameId)
                    .Descending(p => p.LastActivityAt),
                new CreateIndexOptions { Name = "idx_boardType_gameId_lastActivityAt" }),
            new CreateIndexModel<CommunityPost>(
                Builders<CommunityPost>.IndexKeys.Ascending(p => p.Slug),
                new CreateIndexOptions { Name = "idx_slug" }),
            new CreateIndexModel<CommunityPost>(
                Builders<CommunityPost>.IndexKeys.Ascending(p => p.AuthorId),
                new CreateIndexOptions { Name = "idx_authorId" })
        ], ct);
    }

    private async Task CreateCommunityCommentIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<CommunityComment>(CollectionNames.CommunityComments);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<CommunityComment>(
                Builders<CommunityComment>.IndexKeys.Ascending(c => c.PostId).Ascending(c => c.CreatedAt),
                new CreateIndexOptions { Name = "idx_postId_createdAt" })
        ], ct);
    }

    private async Task CreateGuideIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<Guide>(CollectionNames.Guides);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Guide>(
                Builders<Guide>.IndexKeys.Ascending(g => g.GameId).Ascending(g => g.Status).Descending(g => g.CreatedAt),
                new CreateIndexOptions { Name = "idx_gameId_status_createdAt" }),
            new CreateIndexModel<Guide>(
                Builders<Guide>.IndexKeys.Ascending(g => g.AuthorId),
                new CreateIndexOptions { Name = "idx_authorId" })
        ], ct);
    }

    private async Task CreateContentVoteIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<ContentVote>(CollectionNames.ContentVotes);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<ContentVote>(
                Builders<ContentVote>.IndexKeys
                    .Ascending(v => v.UserId)
                    .Ascending(v => v.TargetId)
                    .Ascending(v => v.TargetType),
                new CreateIndexOptions { Unique = true, Name = "idx_userId_targetId_targetType_unique" }),
            new CreateIndexModel<ContentVote>(
                Builders<ContentVote>.IndexKeys.Ascending(v => v.TargetId),
                new CreateIndexOptions { Name = "idx_targetId" })
        ], ct);
    }

    private async Task CreateGuildEventIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<GuildEvent>(CollectionNames.GuildEvents);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<GuildEvent>(
                Builders<GuildEvent>.IndexKeys.Ascending(e => e.GuildId).Ascending(e => e.StartsAt),
                new CreateIndexOptions { Name = "idx_guildId_startsAt" })
        ], ct);
    }

    private async Task CreateTournamentIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<Tournament>(CollectionNames.Tournaments);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Tournament>(
                Builders<Tournament>.IndexKeys.Ascending(t => t.GameId).Ascending(t => t.Status).Descending(t => t.CreatedAt),
                new CreateIndexOptions { Name = "idx_gameId_status_createdAt" }),
            new CreateIndexModel<Tournament>(
                Builders<Tournament>.IndexKeys.Ascending(t => t.GuildId),
                new CreateIndexOptions { Name = "idx_guildId" })
        ], ct);
    }

    private async Task CreateTournamentEntryIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<TournamentEntry>(CollectionNames.TournamentEntries);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<TournamentEntry>(
                Builders<TournamentEntry>.IndexKeys.Ascending(e => e.TournamentId).Ascending(e => e.ParticipantId),
                new CreateIndexOptions { Unique = true, Name = "idx_tournamentId_participantId_unique" }),
            new CreateIndexModel<TournamentEntry>(
                Builders<TournamentEntry>.IndexKeys.Ascending(e => e.TournamentId).Ascending(e => e.Seed),
                new CreateIndexOptions { Name = "idx_tournamentId_seed" })
        ], ct);
    }

    private async Task CreateTournamentMatchIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<TournamentMatch>(CollectionNames.TournamentMatches);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<TournamentMatch>(
                Builders<TournamentMatch>.IndexKeys.Ascending(m => m.TournamentId).Ascending(m => m.Round).Ascending(m => m.MatchNumber),
                new CreateIndexOptions { Name = "idx_tournamentId_round_matchNumber" })
        ], ct);
    }

    private async Task CreateRecentActivityIndexes(CancellationToken ct)
    {
        var col = _mongoContext.GetCollection<RecentActivity>(CollectionNames.RecentActivities);
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<RecentActivity>(
                Builders<RecentActivity>.IndexKeys
                    .Ascending(r => r.UserId)
                    .Ascending(r => r.TargetId)
                    .Ascending(r => r.TargetType),
                new CreateIndexOptions { Unique = true, Name = "idx_userId_targetId_targetType_unique" }),
            new CreateIndexModel<RecentActivity>(
                Builders<RecentActivity>.IndexKeys
                    .Ascending(r => r.UserId)
                    .Ascending(r => r.TargetType)
                    .Descending(r => r.SeenAt),
                new CreateIndexOptions { Name = "idx_userId_targetType_seenAt" }),
            new CreateIndexModel<RecentActivity>(
                Builders<RecentActivity>.IndexKeys.Ascending(r => r.SeenAt),
                new CreateIndexOptions { Name = "idx_seenAt_ttl", ExpireAfter = TimeSpan.FromDays(30) })
        ], ct);
    }

    private static async Task DropLegacyIndexes<T>(IMongoCollection<T> collection, string[] legacyNames, CancellationToken ct)
    {
        try
        {
            using var cursor = await collection.Indexes.ListAsync(ct);
            var existing = await cursor.ToListAsync(ct);
            var existingNames = existing.Select(i => i["name"].AsString).ToHashSet();

            foreach (var name in legacyNames.Where(existingNames.Contains))
                await collection.Indexes.DropOneAsync(name, ct);
        }
        catch (Exception)
        {
            // Collection may not exist yet
        }
    }
}
