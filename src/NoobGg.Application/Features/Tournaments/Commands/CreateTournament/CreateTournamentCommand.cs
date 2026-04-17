using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Tournaments.DTOs;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Tournaments.Commands.CreateTournament;

public record CreateTournamentCommand : IRequest<Result<TournamentDetailResponse>>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string GameId { get; init; } = string.Empty;
    public string? GuildId { get; init; }
    public TournamentFormat Format { get; init; }
    public int MaxParticipants { get; init; }
    public DateTime RegistrationDeadline { get; init; }
    public DateTime? StartsAt { get; init; }
    public List<string> PrizeBadges { get; init; } = [];
}
