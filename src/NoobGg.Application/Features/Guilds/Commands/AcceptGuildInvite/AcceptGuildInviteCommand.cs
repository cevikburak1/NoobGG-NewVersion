using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Guilds.Commands.AcceptGuildInvite;

public record AcceptGuildInviteCommand : IRequest<Result>
{
    public string InviteId { get; init; } = string.Empty;
}
