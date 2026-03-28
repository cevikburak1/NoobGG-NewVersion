using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Rooms.Commands.InviteToRoom;

public record InviteToRoomCommand : IRequest<Result>
{
    public string RoomId { get; init; } = string.Empty;
    public string InvitedUserId { get; init; } = string.Empty;
}
