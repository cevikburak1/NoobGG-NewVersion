namespace NoobGg.Application.Features.Guilds.DTOs;

public record GuildResponse(
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
    List<string> GameNames,
    DateTime CreatedAt);
