namespace NoobGg.Application.Features.Guilds.DTOs;

public record GuildDetailResponse(
    string Id,
    string Name,
    string Tag,
    string? Description,
    string CreatorId,
    bool IsPublic,
    int MaxMembers,
    int CurrentMemberCount,
    string Region,
    string Language,
    List<string> GameIds,
    List<GuildGameInfo> Games,
    DateTime CreatedAt,
    List<GuildMemberResponse> Members,
    string? MyJoinRequestStatus,
    int PendingJoinRequestCount);

public record GuildGameInfo(
    string Id,
    string Name,
    string? BackgroundImageUrl);
