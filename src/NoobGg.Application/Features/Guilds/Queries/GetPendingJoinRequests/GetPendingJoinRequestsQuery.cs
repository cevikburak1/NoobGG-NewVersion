using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Guilds.DTOs;

namespace NoobGg.Application.Features.Guilds.Queries.GetPendingJoinRequests;

public record GetPendingJoinRequestsQuery : IRequest<Result<List<GuildJoinRequestResponse>>>
{
    public string GuildId { get; init; } = string.Empty;
}
