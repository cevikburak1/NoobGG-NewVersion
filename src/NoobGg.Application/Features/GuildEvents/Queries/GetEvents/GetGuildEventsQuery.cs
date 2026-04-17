using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.GuildEvents.DTOs;

namespace NoobGg.Application.Features.GuildEvents.Queries.GetEvents;

public record GetGuildEventsQuery : IRequest<Result<GuildEventListResponse>>
{
    public string GuildId { get; init; } = string.Empty;
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}
