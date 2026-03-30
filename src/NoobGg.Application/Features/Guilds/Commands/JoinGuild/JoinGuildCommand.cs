using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Guilds.Commands.JoinGuild;

public record JoinGuildCommand : IRequest<Result>
{
    public string GuildId { get; init; } = string.Empty;
    public string? Message { get; init; }
}
