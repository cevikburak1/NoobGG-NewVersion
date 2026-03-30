using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Guilds.Commands.UpdateGuildMemberRole;

public record UpdateGuildMemberRoleCommand : IRequest<Result>
{
    public string GuildId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public GuildMemberRole NewRole { get; init; }
}
