using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Recommendations.DTOs;

namespace NoobGg.Application.Features.Recommendations.Queries.GetRecommendedRooms;

public record GetRecommendedRoomsQuery : IRequest<Result<List<RecommendedRoomResponse>>>
{
    public string? GameId { get; init; }
    public int Limit { get; init; } = 10;
}
