using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Tournaments.DTOs;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Tournaments.Queries.GetTournaments;

public record GetTournamentsQuery : IRequest<Result<TournamentListResponse>>
{
    public string? GameId { get; init; }
    public string? GuildId { get; init; }
    public TournamentStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
