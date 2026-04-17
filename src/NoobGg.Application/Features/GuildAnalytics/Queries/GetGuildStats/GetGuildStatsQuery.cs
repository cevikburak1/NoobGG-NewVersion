using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.GuildAnalytics.DTOs;

namespace NoobGg.Application.Features.GuildAnalytics.Queries.GetGuildStats;

public record GetGuildStatsQuery : IRequest<Result<GuildStatsResponse>>
{
    public string GuildId { get; init; } = string.Empty;
    public string? GameId { get; init; }
    public int Days { get; init; } = 30;
}
