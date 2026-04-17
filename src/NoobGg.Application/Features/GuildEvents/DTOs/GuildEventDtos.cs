namespace NoobGg.Application.Features.GuildEvents.DTOs;

public record GuildEventResponse(
    string Id,
    string GuildId,
    string CreatorId,
    string CreatorUsername,
    string Title,
    string? Description,
    DateTime StartsAt,
    DateTime EndsAt,
    string? GameId,
    string? TournamentId,
    DateTime CreatedAt);

public record GuildEventListResponse(List<GuildEventResponse> Events, int TotalCount);
