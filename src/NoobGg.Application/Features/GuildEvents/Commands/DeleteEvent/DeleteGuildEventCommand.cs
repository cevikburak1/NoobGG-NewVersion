using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.GuildEvents.Commands.DeleteEvent;

public record DeleteGuildEventCommand : IRequest<Result>
{
    public string EventId { get; init; } = string.Empty;
}
