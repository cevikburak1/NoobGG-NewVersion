using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Rooms.Commands.JoinRoom;

public record JoinRoomCommand : IRequest<Result>
{
    public string RoomId { get; init; } = string.Empty;
}
