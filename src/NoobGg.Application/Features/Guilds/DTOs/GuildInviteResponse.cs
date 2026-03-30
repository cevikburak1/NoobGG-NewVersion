namespace NoobGg.Application.Features.Guilds.DTOs;

public record GuildInviteResponse(
    string Id,
    string GuildId,
    string GuildName,
    string GuildTag,
    string InviterId,
    string InviterUsername,
    string? InviterAvatarUrl,
    string Status,
    DateTime CreatedAt);
