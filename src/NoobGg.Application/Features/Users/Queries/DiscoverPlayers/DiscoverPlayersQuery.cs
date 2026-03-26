using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Users.DTOs;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Users.Queries.DiscoverPlayers;

public record DiscoverPlayersQuery : IRequest<Result<PagedResult<DiscoverPlayerResponse>>>
{
    public string? Search { get; init; }
    public Region? Region { get; init; }
    public ExperienceLevel? ExperienceLevel { get; init; }
    public bool? LookingForTeam { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
}
