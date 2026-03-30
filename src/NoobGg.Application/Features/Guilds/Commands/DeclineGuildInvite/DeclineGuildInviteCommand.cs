using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Guilds.Commands.DeclineGuildInvite;

public record DeclineGuildInviteCommand : IRequest<Result>
{
    public string InviteId { get; init; } = string.Empty;
}
