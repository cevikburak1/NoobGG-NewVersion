using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Guilds.Commands.LeaveGuild;

public record LeaveGuildCommand : IRequest<Result>
{
    public string GuildId { get; init; } = string.Empty;
}
