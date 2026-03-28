namespace NoobGg.Application.Features.Rooms.DTOs;

public record RoomMemberResponse(
    string UserId,
    string Username,
    string? AvatarUrl,
    string Role,
    DateTime JoinedAt);
