using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Rooms.Commands.DeclineRoomInvite;

public record DeclineRoomInviteCommand : IRequest<Result>
{
    public string InviteId { get; init; } = string.Empty;
}
