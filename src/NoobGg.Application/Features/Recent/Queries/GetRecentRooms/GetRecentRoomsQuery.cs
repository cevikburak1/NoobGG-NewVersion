using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Recent.DTOs;

namespace NoobGg.Application.Features.Recent.Queries.GetRecentRooms;

public record GetRecentRoomsQuery : IRequest<Result<List<RecentRoomResponse>>>
{
    public int Limit { get; init; } = 5;
}
