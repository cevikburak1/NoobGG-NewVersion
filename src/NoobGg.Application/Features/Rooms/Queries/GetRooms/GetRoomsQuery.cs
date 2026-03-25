using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Rooms.DTOs;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Rooms.Queries.GetRooms;

public record GetRoomsQuery : IRequest<Result<PagedResult<RoomResponse>>>
{
    public string? GameId { get; init; }
    public Region? Region { get; init; }
    public Language? Language { get; init; }
    public string? Tag { get; init; }
    public RoomStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
