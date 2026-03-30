using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Guilds.Commands.InviteToGuild;

public record InviteToGuildCommand : IRequest<Result>
{
    public string GuildId { get; init; } = string.Empty;
    public string InvitedUserId { get; init; } = string.Empty;
}
