using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;

namespace NoobGg.Application.Features.Community.Queries.GetFeed;

public record GetCommunityFeedQuery : IRequest<Result<CommunityFeedResponse>>
{
    public string GameId { get; init; } = string.Empty;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
