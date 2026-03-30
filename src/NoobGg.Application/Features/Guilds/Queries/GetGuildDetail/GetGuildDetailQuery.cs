using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Guilds.DTOs;

namespace NoobGg.Application.Features.Guilds.Queries.GetGuildDetail;

public record GetGuildDetailQuery : IRequest<Result<GuildDetailResponse>>
{
    public string GuildId { get; init; } = string.Empty;
}
