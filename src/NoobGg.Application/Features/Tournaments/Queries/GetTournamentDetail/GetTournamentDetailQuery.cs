using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Tournaments.DTOs;

namespace NoobGg.Application.Features.Tournaments.Queries.GetTournamentDetail;

public record GetTournamentDetailQuery : IRequest<Result<TournamentDetailResponse>>
{
    public string TournamentId { get; init; } = string.Empty;
}
