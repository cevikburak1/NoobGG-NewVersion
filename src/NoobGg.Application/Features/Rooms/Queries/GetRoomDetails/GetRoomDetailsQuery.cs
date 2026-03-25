using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Rooms.DTOs;

namespace NoobGg.Application.Features.Rooms.Queries.GetRoomDetails;

public record GetRoomDetailsQuery : IRequest<Result<RoomDetailResponse>>
{
    public string RoomId { get; init; } = string.Empty;
}
