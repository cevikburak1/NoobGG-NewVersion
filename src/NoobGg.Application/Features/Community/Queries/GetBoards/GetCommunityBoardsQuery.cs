using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;

namespace NoobGg.Application.Features.Community.Queries.GetBoards;

public record GetCommunityBoardsQuery : IRequest<Result<CommunityBoardsOverviewResponse>>
{
    public string? Category { get; init; }
    public string? Sort { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 24;
}
