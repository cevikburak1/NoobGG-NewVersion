namespace NoobGg.Application.Features.Guilds.DTOs;

public record GuildJoinRequestResponse(
    string Id,
    string GuildId,
    string UserId,
    string Username,
    string? AvatarUrl,
    string? Message,
    string Status,
    DateTime CreatedAt);
