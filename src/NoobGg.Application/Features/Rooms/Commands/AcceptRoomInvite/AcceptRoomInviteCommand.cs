using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Rooms.Commands.AcceptRoomInvite;

public record AcceptRoomInviteCommand : IRequest<Result>
{
    public string InviteId { get; init; } = string.Empty;
}
