namespace NoobGg.Application.Features.Rooms.DTOs;

public record RoomResponse(
    string Id,
    string Title,
    string GameId,
    string? GameName,
    string? GameImageUrl,
    string CreatorId,
    bool IsPublic,
    int MaxMembers,
    int CurrentMemberCount,
    string Region,
    string Language,
    List<string> Tags,
    string Status,
    DateTime CreatedAt);
