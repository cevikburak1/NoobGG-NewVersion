using NoobGg.Domain.ValueObjects;

namespace NoobGg.Application.Features.Rooms.DTOs;

public record RoomDetailResponse(
    string Id,
    string Title,
    string? Description,
    string GameId,
    string? GameName,
    string? GameImageUrl,
    string CreatorId,
    bool IsPublic,
    int MaxMembers,
    int CurrentMemberCount,
    string Region,
    string Language,
    RankRange? RankRange,
    List<string> Tags,
    string Status,
    string? VoiceChannelId,
    DateTime CreatedAt,
    List<RoomMemberResponse> Members);
