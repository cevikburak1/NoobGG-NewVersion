using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Recent.DTOs;

namespace NoobGg.Application.Features.Recent.Queries.GetRecentPlayers;

public record GetRecentPlayersQuery : IRequest<Result<List<RecentPlayerResponse>>>
{
    public int Limit { get; init; } = 5;
}
