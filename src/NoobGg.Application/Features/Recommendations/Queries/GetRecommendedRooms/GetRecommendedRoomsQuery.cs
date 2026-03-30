using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Recommendations.DTOs;

namespace NoobGg.Application.Features.Recommendations.Queries.GetRecommendedRooms;

public record GetRecommendedRoomsQuery : IRequest<Result<List<RecommendedRoomResponse>>>
{
    public int Limit { get; init; } = 6;
}
