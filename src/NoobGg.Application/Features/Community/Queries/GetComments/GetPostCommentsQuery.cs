using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;

namespace NoobGg.Application.Features.Community.Queries.GetComments;

public record GetPostCommentsQuery : IRequest<Result<CommunityCommentsResponse>>
{
    public string PostId { get; init; } = string.Empty;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
