using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Matchmaking.DTOs;
using NoobGg.Application.Features.Rooms.Helpers;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Matchmaking.Commands.JoinMatchQueue;

public class JoinMatchQueueCommandHandler : IRequestHandler<JoinMatchQueueCommand, Result<JoinMatchQueueResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IRoomNotificationService _roomNotification;
    private readonly INotificationService _notificationService;

    public JoinMatchQueueCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        IRoomNotificationService roomNotification,
        INotificationService notificationService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _roomNotification = roomNotification;
        _notificationService = notificationService;
    }

    public async Task<Result<JoinMatchQueueResponse>> Handle(JoinMatchQueueCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<JoinMatchQueueResponse>.Unauthorized();

        var userId = _currentUser.UserId;
        var queue = _mongoContext.GetCollection<MatchQueueEntry>(CollectionNames.MatchQueueEntries);
        var gameProfiles = _mongoContext.GetCollection<UserGameProfile>(CollectionNames.UserGameProfiles);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);

        var game = await games.Find(g => g.Id == request.GameId && g.IsActive).FirstOrDefaultAsync(ct);
        if (game is null)
            return Result<JoinMatchQueueResponse>.NotFound("Game not found or inactive");

        // Profile is best-effort now: users can quick-match a game even without a dedicated profile.
        // Filters fall back to whatever we know (profile values + explicit UI overrides).
        var profile = await gameProfiles
            .Find(gp => gp.UserId == userId && gp.GameId == request.GameId)
            .FirstOrDefaultAsync(ct);

        Region? effectiveRegion = request.Region ?? profile?.Region;
        Language? effectiveLanguage = request.Language ?? profile?.Languages.FirstOrDefault();

        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var hasOpenRoom = await rooms.Find(r =>
                r.CreatorId == userId &&
                (r.Status == RoomStatus.Open || r.Status == RoomStatus.Full))
            .AnyAsync(ct);

        if (hasOpenRoom)
            return Result<JoinMatchQueueResponse>.Fail("You already have an active room. Close it before quick match.");

        var existingMatched = await queue.Find(e =>
                e.UserId == userId &&
                e.Status == MatchQueueEntryStatus.Matched &&
                e.MatchedRoomId != null)
            .SortByDescending(e => e.UpdatedAt)
            .FirstOrDefaultAsync(ct);

        if (existingMatched?.MatchedRoomId is { } existingRoomId)
            return Result<JoinMatchQueueResponse>.Success(new JoinMatchQueueResponse("Matched", existingRoomId, false));

        // Primary path: auto-join an existing Open room. Strict pass respects region/language filters;
        // if that turns up empty (or we had nothing to filter on), fall back to a loose gameId-only pass
        // so users can still join any compatible public room just by picking a game.
        var autoJoinedRoomId = await TryAutoJoinOpenRoomAsync(
            userId, request.GameId, effectiveRegion, effectiveLanguage, ct);

        if (autoJoinedRoomId is null && (effectiveRegion.HasValue || effectiveLanguage.HasValue))
        {
            autoJoinedRoomId = await TryAutoJoinOpenRoomAsync(
                userId, request.GameId, null, null, ct);
        }

        if (autoJoinedRoomId is not null)
        {
            await queue.UpdateManyAsync(
                e => e.UserId == userId &&
                     (e.Status == MatchQueueEntryStatus.Searching ||
                      e.Status == MatchQueueEntryStatus.FallbackSuggested),
                Builders<MatchQueueEntry>.Update
                    .Set(e => e.Status, MatchQueueEntryStatus.Cancelled)
                    .Set(e => e.UpdatedAt, DateTime.UtcNow),
                cancellationToken: ct);

            return Result<JoinMatchQueueResponse>.Success(
                new JoinMatchQueueResponse("Matched", autoJoinedRoomId, false));
        }

        // Queue path requires a profile so we have a deterministic region/language/elo to match on.
        // Without those we surface a soft "no rooms available" status instead of inserting a broken entry.
        if (profile is null || !effectiveRegion.HasValue || !effectiveLanguage.HasValue)
        {
            return Result<JoinMatchQueueResponse>.Success(
                new JoinMatchQueueResponse("NoRoomsAvailable", null, false));
        }

        var queueRegion = effectiveRegion.Value;
        var queueLanguage = effectiveLanguage.Value;

        MatchQueueEntry? workingEntry = await queue.Find(e =>
                e.UserId == userId &&
                (e.Status == MatchQueueEntryStatus.Searching ||
                 e.Status == MatchQueueEntryStatus.FallbackSuggested))
            .SortByDescending(e => e.UpdatedAt)
            .FirstOrDefaultAsync(ct);

        if (workingEntry is not null && workingEntry.GameId != request.GameId)
        {
            await queue.UpdateOneAsync(
                e => e.Id == workingEntry.Id,
                Builders<MatchQueueEntry>.Update
                    .Set(e => e.Status, MatchQueueEntryStatus.Cancelled)
                    .Set(e => e.UpdatedAt, DateTime.UtcNow),
                cancellationToken: ct);
            workingEntry = null;
        }

        if (workingEntry is not null && workingEntry.ExpiresAt < DateTime.UtcNow)
        {
            await queue.UpdateOneAsync(
                e => e.Id == workingEntry.Id,
                Builders<MatchQueueEntry>.Update
                    .Set(e => e.Status, MatchQueueEntryStatus.Expired)
                    .Set(e => e.UpdatedAt, DateTime.UtcNow),
                cancellationToken: ct);
            workingEntry = null;
        }

        if (workingEntry is null)
        {
            await queue.UpdateManyAsync(
                e => e.UserId == userId &&
                     (e.Status == MatchQueueEntryStatus.Searching ||
                      e.Status == MatchQueueEntryStatus.FallbackSuggested),
                Builders<MatchQueueEntry>.Update
                    .Set(e => e.Status, MatchQueueEntryStatus.Cancelled)
                    .Set(e => e.UpdatedAt, DateTime.UtcNow),
                cancellationToken: ct);

            workingEntry = new MatchQueueEntry
            {
                UserId = userId,
                GameId = request.GameId,
                Region = queueRegion,
                Language = queueLanguage,
                EloPoints = profile.EloPoints,
                Role = profile.Role,
                Status = MatchQueueEntryStatus.Searching,
                ExpiresAt = DateTime.UtcNow.AddMinutes(MatchmakingConstants.QueueEntryLifetimeMinutes),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await queue.InsertOneAsync(workingEntry, cancellationToken: ct);
        }
        else
        {
            await queue.UpdateOneAsync(
                e => e.Id == workingEntry.Id,
                Builders<MatchQueueEntry>.Update
                    .Set(e => e.EloPoints, profile.EloPoints)
                    .Set(e => e.Role, profile.Role)
                    .Set(e => e.Region, queueRegion)
                    .Set(e => e.Language, queueLanguage)
                    .Set(e => e.UpdatedAt, DateTime.UtcNow),
                cancellationToken: ct);
            workingEntry.EloPoints = profile.EloPoints;
            workingEntry.Role = profile.Role;
            workingEntry.Region = queueRegion;
            workingEntry.Language = queueLanguage;
        }

        var matched = await TryPairAsync(workingEntry, ct);
        if (matched is not null)
            return matched;

        var ageSeconds = (int)(DateTime.UtcNow - workingEntry.CreatedAt).TotalSeconds;
        var fallbackReady = ageSeconds >= MatchmakingConstants.FallbackAfterSeconds;

        if (fallbackReady && workingEntry.Status == MatchQueueEntryStatus.Searching)
        {
            await queue.UpdateOneAsync(
                e => e.Id == workingEntry.Id && e.Status == MatchQueueEntryStatus.Searching,
                Builders<MatchQueueEntry>.Update
                    .Set(e => e.Status, MatchQueueEntryStatus.FallbackSuggested)
                    .Set(e => e.UpdatedAt, DateTime.UtcNow),
                cancellationToken: ct);
            workingEntry.Status = MatchQueueEntryStatus.FallbackSuggested;
        }

        var statusLabel = workingEntry.Status == MatchQueueEntryStatus.FallbackSuggested || fallbackReady
            ? "FallbackSuggested"
            : "Searching";

        return Result<JoinMatchQueueResponse>.Success(new JoinMatchQueueResponse(statusLabel, null, fallbackReady));
    }

    private async Task<Result<JoinMatchQueueResponse>?> TryPairAsync(MatchQueueEntry myEntry, CancellationToken ct)
    {
        var queue = _mongoContext.GetCollection<MatchQueueEntry>(CollectionNames.MatchQueueEntries);
        var tolerance = MatchmakingConstants.EloTolerancePoints;
        var minElo = myEntry.EloPoints - tolerance;
        var maxElo = myEntry.EloPoints + tolerance;

        var activeQueueFilter =
            Builders<MatchQueueEntry>.Filter.In(
                e => e.Status,
                [MatchQueueEntryStatus.Searching, MatchQueueEntryStatus.FallbackSuggested]);

        var candidates = await queue.Find(
                Builders<MatchQueueEntry>.Filter.And(
                    Builders<MatchQueueEntry>.Filter.Ne(e => e.UserId, myEntry.UserId),
                    activeQueueFilter,
                    Builders<MatchQueueEntry>.Filter.Eq(e => e.GameId, myEntry.GameId),
                    Builders<MatchQueueEntry>.Filter.Eq(e => e.Region, myEntry.Region),
                    Builders<MatchQueueEntry>.Filter.Eq(e => e.Language, myEntry.Language),
                    Builders<MatchQueueEntry>.Filter.Gte(e => e.EloPoints, minElo),
                    Builders<MatchQueueEntry>.Filter.Lte(e => e.EloPoints, maxElo)))
            .SortBy(e => e.CreatedAt)
            .Limit(25)
            .ToListAsync(ct);

        foreach (var partner in candidates)
        {
            if (await HaveBlockBetweenAsync(myEntry.UserId, partner.UserId, ct))
                continue;

            var partnerPreviousStatus = partner.Status;

            var creatorId = partner.CreatedAt <= myEntry.CreatedAt ? partner.UserId : myEntry.UserId;
            var joinerId = creatorId == partner.UserId ? myEntry.UserId : partner.UserId;

            string? roomId = null;
            try
            {
                roomId = await CreateQuickMatchRoomAsync(
                    creatorId,
                    joinerId,
                    myEntry.GameId,
                    myEntry.Region,
                    myEntry.Language,
                    ct);

                if (string.IsNullOrEmpty(roomId))
                    continue;

                var partnerUpdate = await queue.UpdateOneAsync(
                    Builders<MatchQueueEntry>.Filter.And(
                        Builders<MatchQueueEntry>.Filter.Eq(e => e.Id, partner.Id),
                        activeQueueFilter),
                    Builders<MatchQueueEntry>.Update
                        .Set(e => e.Status, MatchQueueEntryStatus.Matched)
                        .Set(e => e.MatchedRoomId, roomId)
                        .Set(e => e.MatchedWithUserId, myEntry.UserId)
                        .Set(e => e.UpdatedAt, DateTime.UtcNow),
                    cancellationToken: ct);

                if (partnerUpdate.ModifiedCount == 0)
                {
                    await DeleteQuickMatchRoomAsync(roomId, ct);
                    continue;
                }

                var selfUpdate = await queue.UpdateOneAsync(
                    Builders<MatchQueueEntry>.Filter.And(
                        Builders<MatchQueueEntry>.Filter.Eq(e => e.Id, myEntry.Id),
                        activeQueueFilter),
                    Builders<MatchQueueEntry>.Update
                        .Set(e => e.Status, MatchQueueEntryStatus.Matched)
                        .Set(e => e.MatchedRoomId, roomId)
                        .Set(e => e.MatchedWithUserId, partner.UserId)
                        .Set(e => e.UpdatedAt, DateTime.UtcNow),
                    cancellationToken: ct);

                if (selfUpdate.ModifiedCount == 0)
                {
                    await queue.UpdateOneAsync(
                        e => e.Id == partner.Id,
                        Builders<MatchQueueEntry>.Update
                            .Set(e => e.Status, partnerPreviousStatus)
                            .Set(e => e.MatchedRoomId, (string?)null)
                            .Set(e => e.MatchedWithUserId, (string?)null)
                            .Set(e => e.UpdatedAt, DateTime.UtcNow),
                        cancellationToken: ct);
                    await DeleteQuickMatchRoomAsync(roomId, ct);
                    continue;
                }

                await _roomNotification.NotifyRoomListChangedAsync(ct);
                return Result<JoinMatchQueueResponse>.Success(new JoinMatchQueueResponse("Matched", roomId, false));
            }
            catch
            {
                if (roomId != null)
                    await DeleteQuickMatchRoomAsync(roomId, ct);
                throw;
            }
        }

        return null;
    }

    private async Task<string?> CreateQuickMatchRoomAsync(
        string creatorId,
        string joinerId,
        string gameId,
        Region region,
        Language language,
        CancellationToken ct)
    {
        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);

        var game = await games.Find(g => g.Id == gameId && g.IsActive).FirstOrDefaultAsync(ct);
        if (game is null)
            return null;

        var room = new Room
        {
            Title = "Quick Match",
            Description = "Auto-created by quick match",
            GameId = gameId,
            CreatorId = creatorId,
            IsPublic = true,
            Region = region,
            Language = language,
            Tags = ["quick-match", "hemen-eslestir"],
            MaxMembers = 5,
            CurrentMemberCount = 2,
            Status = RoomStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await rooms.InsertOneAsync(room, cancellationToken: ct);

        var ownerMember = new RoomMember
        {
            RoomId = room.Id,
            UserId = creatorId,
            Role = RoomMemberRole.Owner,
            JoinedAt = DateTime.UtcNow
        };

        var joinerMember = new RoomMember
        {
            RoomId = room.Id,
            UserId = joinerId,
            Role = RoomMemberRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        await roomMembers.InsertManyAsync([ownerMember, joinerMember], cancellationToken: ct);
        await RoomEloHelper.RecalculateAsync(_mongoContext, room.Id, ct);
        return room.Id;
    }

    private async Task DeleteQuickMatchRoomAsync(string roomId, CancellationToken ct)
    {
        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        await roomMembers.DeleteManyAsync(m => m.RoomId == roomId, ct);
        await rooms.DeleteOneAsync(r => r.Id == roomId, cancellationToken: ct);
    }

    private async Task<bool> HaveBlockBetweenAsync(string userA, string userB, CancellationToken ct)
    {
        var blocks = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);
        return await blocks.Find(b =>
                (b.BlockerId == userA && b.BlockedUserId == userB) ||
                (b.BlockerId == userB && b.BlockedUserId == userA))
            .AnyAsync(ct);
    }

    private async Task<string?> TryAutoJoinOpenRoomAsync(
        string userId,
        string gameId,
        Region? region,
        Language? language,
        CancellationToken ct)
    {
        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        var blocksCol = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);

        // Resolve both directions separately to keep the LINQ provider happy.
        var blockedByMe = await blocksCol
            .Find(b => b.BlockerId == userId)
            .Project(b => b.BlockedUserId)
            .ToListAsync(ct);
        var blockedMe = await blocksCol
            .Find(b => b.BlockedUserId == userId)
            .Project(b => b.BlockerId)
            .ToListAsync(ct);
        var blockedUserIds = blockedByMe.Concat(blockedMe).Distinct().ToList();

        var candidateFilter = Builders<Room>.Filter.And(
            Builders<Room>.Filter.Eq(r => r.GameId, gameId),
            Builders<Room>.Filter.Eq(r => r.Status, RoomStatus.Open),
            Builders<Room>.Filter.Eq(r => r.IsPublic, true),
            Builders<Room>.Filter.Ne(r => r.CreatorId, userId),
            Builders<Room>.Filter.Where(r => r.CurrentMemberCount < r.MaxMembers));

        if (region.HasValue)
        {
            candidateFilter = Builders<Room>.Filter.And(
                candidateFilter,
                Builders<Room>.Filter.Eq(r => r.Region, region.Value));
        }

        if (language.HasValue)
        {
            candidateFilter = Builders<Room>.Filter.And(
                candidateFilter,
                Builders<Room>.Filter.Eq(r => r.Language, language.Value));
        }

        if (blockedUserIds.Count > 0)
        {
            candidateFilter = Builders<Room>.Filter.And(
                candidateFilter,
                Builders<Room>.Filter.Nin(r => r.CreatorId, blockedUserIds));
        }

        var candidates = await rooms
            .Find(candidateFilter)
            .SortBy(r => r.CreatedAt)
            .Limit(10)
            .ToListAsync(ct);

        foreach (var candidate in candidates)
        {
            var atomicFilter = Builders<Room>.Filter.And(
                Builders<Room>.Filter.Eq(r => r.Id, candidate.Id),
                Builders<Room>.Filter.Eq(r => r.Status, RoomStatus.Open),
                Builders<Room>.Filter.Where(r => r.CurrentMemberCount < r.MaxMembers));

            var update = Builders<Room>.Update
                .Inc(r => r.CurrentMemberCount, 1)
                .Set(r => r.UpdatedAt, DateTime.UtcNow);

            var options = new FindOneAndUpdateOptions<Room>
            {
                ReturnDocument = ReturnDocument.After
            };

            var updatedRoom = await rooms.FindOneAndUpdateAsync(atomicFilter, update, options, ct);
            if (updatedRoom is null)
                continue;

            var member = new RoomMember
            {
                RoomId = updatedRoom.Id,
                UserId = userId,
                Role = RoomMemberRole.Member,
                JoinedAt = DateTime.UtcNow
            };

            try
            {
                await roomMembers.InsertOneAsync(member, cancellationToken: ct);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // Already a member — rollback the count bump and move on.
                await rooms.UpdateOneAsync(
                    Builders<Room>.Filter.Eq(r => r.Id, updatedRoom.Id),
                    Builders<Room>.Update.Inc(r => r.CurrentMemberCount, -1),
                    cancellationToken: ct);
                continue;
            }

            if (updatedRoom.CurrentMemberCount >= updatedRoom.MaxMembers)
            {
                await rooms.UpdateOneAsync(
                    Builders<Room>.Filter.Eq(r => r.Id, updatedRoom.Id),
                    Builders<Room>.Update.Set(r => r.Status, RoomStatus.Full),
                    cancellationToken: ct);
            }

            await RoomEloHelper.RecalculateAsync(_mongoContext, updatedRoom.Id, ct);

            var username = _currentUser.Username ?? "Unknown";
            await _roomNotification.NotifyMemberJoinedAsync(updatedRoom.Id, userId, username, ct);
            await _roomNotification.NotifyRoomListChangedAsync(ct);

            if (updatedRoom.CreatorId != userId)
            {
                await _notificationService.CreateAsync(
                    updatedRoom.CreatorId,
                    NotificationType.RoomJoined,
                    "New member joined your room",
                    $"{username} joined \"{updatedRoom.Title}\"",
                    new Dictionary<string, string> { { "roomId", updatedRoom.Id } },
                    ct);
            }

            return updatedRoom.Id;
        }

        return null;
    }
}
