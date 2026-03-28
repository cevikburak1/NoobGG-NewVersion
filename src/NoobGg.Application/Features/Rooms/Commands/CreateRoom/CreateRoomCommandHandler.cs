using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Rooms.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Rooms.Commands.CreateRoom;

public class CreateRoomCommandHandler : IRequestHandler<CreateRoomCommand, Result<RoomDetailResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly IRoomNotificationService _roomNotification;

    public CreateRoomCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        IRoomNotificationService roomNotification)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _roomNotification = roomNotification;
    }

    public async Task<Result<RoomDetailResponse>> Handle(CreateRoomCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<RoomDetailResponse>.Unauthorized();

        var userId = _currentUser.UserId;
        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        var games = _mongoContext.GetCollection<Game>(CollectionNames.Games);

        var game = await games.Find(g => g.Id == request.GameId && g.IsActive)
            .FirstOrDefaultAsync(ct);

        if (game is null)
            return Result<RoomDetailResponse>.Fail("Game not found or inactive", 404);

        var hasOpenRoom = await rooms.Find(r =>
                r.CreatorId == userId &&
                (r.Status == RoomStatus.Open || r.Status == RoomStatus.Full))
            .AnyAsync(ct);

        if (hasOpenRoom)
            return Result<RoomDetailResponse>.Fail("You already have an active room. Close it before creating a new one.");

        var room = new Room
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            GameId = request.GameId,
            CreatorId = userId,
            IsPublic = request.IsPublic,
            Region = request.Region,
            Language = request.Language,
            RankRange = request.RankRange,
            Tags = request.Tags.Select(t => t.Trim().ToLowerInvariant()).Distinct().ToList(),
            MaxMembers = 5,
            CurrentMemberCount = 1,
            Status = RoomStatus.Open
        };

        await rooms.InsertOneAsync(room, cancellationToken: ct);

        var ownerMember = new RoomMember
        {
            RoomId = room.Id,
            UserId = userId,
            Role = RoomMemberRole.Owner,
            JoinedAt = DateTime.UtcNow
        };

        await roomMembers.InsertOneAsync(ownerMember, cancellationToken: ct);

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var user = await users.Find(u => u.Id == userId).FirstOrDefaultAsync(ct);

        var profilesCol = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var ownerProfile = await profilesCol.Find(p => p.UserId == userId).FirstOrDefaultAsync(ct);

        var memberResponse = new RoomMemberResponse(
            userId,
            user?.Username ?? "Unknown",
            ownerProfile?.AvatarUrl,
            ownerMember.Role.ToString(),
            ownerMember.JoinedAt);

        var response = new RoomDetailResponse(
            room.Id,
            room.Title,
            room.Description,
            room.GameId,
            game.Name,
            game.BackgroundImageUrl,
            room.CreatorId,
            room.IsPublic,
            room.MaxMembers,
            room.CurrentMemberCount,
            room.Region.ToString(),
            room.Language.ToString(),
            room.RankRange,
            room.Tags,
            room.Status.ToString(),
            room.VoiceChannelId,
            room.CreatedAt,
            [memberResponse]);

        await _roomNotification.NotifyRoomListChangedAsync(ct);

        return Result<RoomDetailResponse>.Created(response);
    }
}
