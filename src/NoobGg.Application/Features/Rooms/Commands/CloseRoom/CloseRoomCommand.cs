using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Rooms.Commands.CloseRoom;

public record CloseRoomCommand : IRequest<Result>
{
    public string RoomId { get; init; } = string.Empty;
}
