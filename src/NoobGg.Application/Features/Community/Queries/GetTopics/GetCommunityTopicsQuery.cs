using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;

namespace NoobGg.Application.Features.Community.Queries.GetTopics;

public record GetCommunityTopicsQuery : IRequest<Result<CommunityTopicListResponse>>
{
    public string BoardSlug { get; init; } = "general";
    public string? BoardId { get; init; }
    public string Sort { get; init; } = "latest";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
