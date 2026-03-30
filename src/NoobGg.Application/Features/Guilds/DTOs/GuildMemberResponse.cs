namespace NoobGg.Application.Features.Guilds.DTOs;

public record GuildMemberResponse(
    string UserId,
    string Username,
    string? AvatarUrl,
    string Role,
    DateTime JoinedAt);
