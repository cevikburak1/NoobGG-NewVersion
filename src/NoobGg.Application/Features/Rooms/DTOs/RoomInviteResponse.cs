namespace NoobGg.Application.Features.Rooms.DTOs;

public record RoomInviteResponse(
    string Id,
    string RoomId,
    string RoomTitle,
    string? GameName,
    string? GameImageUrl,
    string InviterId,
    string InviterUsername,
    string? InviterAvatarUrl,
    string Status,
    DateTime CreatedAt);
