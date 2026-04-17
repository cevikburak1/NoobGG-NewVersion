using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Guides.DTOs;

namespace NoobGg.Application.Features.Guides.Queries.GetGuides;

public record GetGuidesQuery : IRequest<Result<GuideListResponse>>
{
    public string? GameId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; } = "recent";
}
