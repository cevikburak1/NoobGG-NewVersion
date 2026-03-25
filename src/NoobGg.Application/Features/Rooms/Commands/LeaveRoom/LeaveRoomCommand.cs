using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Rooms.Commands.LeaveRoom;

public record LeaveRoomCommand : IRequest<Result>
{
    public string RoomId { get; init; } = string.Empty;
}
