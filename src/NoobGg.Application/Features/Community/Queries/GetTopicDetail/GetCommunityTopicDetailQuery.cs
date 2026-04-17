using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;

namespace NoobGg.Application.Features.Community.Queries.GetTopicDetail;

public record GetCommunityTopicDetailQuery : IRequest<Result<CommunityTopicDetailResponse>>
{
    public string TopicId { get; init; } = string.Empty;
}
