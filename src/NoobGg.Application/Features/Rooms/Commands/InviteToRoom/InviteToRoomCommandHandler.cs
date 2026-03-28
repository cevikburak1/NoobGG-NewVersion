using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Rooms.Commands.InviteToRoom;

public class InviteToRoomCommandHandler : IRequestHandler<InviteToRoomCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notificationService;

    public InviteToRoomCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        INotificationService notificationService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result> Handle(InviteToRoomCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Fail("Unauthorized", 401);

        var inviterId = _currentUser.UserId;

        if (inviterId == request.InvitedUserId)
            return Result.Fail("You cannot invite yourself");

        var rooms = _mongoContext.GetCollection<Room>(CollectionNames.Rooms);
        var room = await rooms.Find(r => r.Id == request.RoomId).FirstOrDefaultAsync(ct);
        if (room is null)
            return Result.Fail("Room not found", 404);

        if (room.Status == RoomStatus.Closed)
            return Result.Fail("Room is closed");

        var roomMembers = _mongoContext.GetCollection<RoomMember>(CollectionNames.RoomMembers);
        var isInviterMember = await roomMembers
            .Find(m => m.RoomId == request.RoomId && m.UserId == inviterId)
            .AnyAsync(ct);

        if (!isInviterMember)
            return Result.Fail("You must be a room member to invite players");

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var targetExists = await users.Find(u => u.Id == request.InvitedUserId).AnyAsync(ct);
        if (!targetExists)
            return Result.Fail("User not found", 404);

        var blocks = _mongoContext.GetCollection<Block>(CollectionNames.Blocks);
        var hasBlock = await blocks.Find(b =>
            (b.BlockerId == inviterId && b.BlockedUserId == request.InvitedUserId) ||
            (b.BlockerId == request.InvitedUserId && b.BlockedUserId == inviterId)
        ).AnyAsync(ct);

        if (hasBlock)
            return Result.Fail("Cannot invite this user");

        var isAlreadyMember = await roomMembers
            .Find(m => m.RoomId == request.RoomId && m.UserId == request.InvitedUserId)
            .AnyAsync(ct);

        if (isAlreadyMember)
            return Result.Fail("User is already a member of this room");

        var invites = _mongoContext.GetCollection<RoomInvite>(CollectionNames.RoomInvites);
        var hasPending = await invites.Find(i =>
            i.RoomId == request.RoomId &&
            i.InvitedUserId == request.InvitedUserId &&
            i.Status == RoomInviteStatus.Pending
        ).AnyAsync(ct);

        if (hasPending)
            return Result.Fail("An invite is already pending for this user");

        var invite = new RoomInvite
        {
            RoomId = request.RoomId,
            InviterId = inviterId,
            InvitedUserId = request.InvitedUserId
        };

        await invites.InsertOneAsync(invite, cancellationToken: ct);

        var inviterName = _currentUser.Username ?? "Someone";
        await _notificationService.CreateAsync(
            request.InvitedUserId,
            NotificationType.RoomInvite,
            "Room Invite",
            $"{inviterName} invited you to \"{room.Title}\"",
            new Dictionary<string, string>
            {
                { "roomId", request.RoomId },
                { "inviteId", invite.Id },
                { "inviterId", inviterId }
            },
            ct);

        return Result.Success();
    }
}
