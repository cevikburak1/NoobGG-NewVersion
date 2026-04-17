using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.GuildEvents.DTOs;

namespace NoobGg.Application.Features.GuildEvents.Commands.CreateEvent;

public record CreateGuildEventCommand : IRequest<Result<GuildEventResponse>>
{
    public string GuildId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartsAt { get; init; }
    public DateTime EndsAt { get; init; }
    public string? GameId { get; init; }
    public string? TournamentId { get; init; }
}
