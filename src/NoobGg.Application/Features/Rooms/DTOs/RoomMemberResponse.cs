namespace NoobGg.Application.Features.Rooms.DTOs;

public record RoomMemberResponse(
    string UserId,
    string Username,
    string Role,
    DateTime JoinedAt);
