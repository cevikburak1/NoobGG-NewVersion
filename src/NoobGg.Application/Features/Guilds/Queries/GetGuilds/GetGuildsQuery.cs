using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Guilds.DTOs;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guilds.Queries.GetGuilds;

public record GetGuildsQuery : IRequest<Result<PagedResult<GuildResponse>>>
{
    public string? GameId { get; init; }
    public Region? Region { get; init; }
    public Language? Language { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
