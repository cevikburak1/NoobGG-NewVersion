using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Guilds.Commands.KickGuildMember;

public record KickGuildMemberCommand : IRequest<Result>
{
    public string GuildId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
}
