using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Guides.DTOs;

namespace NoobGg.Application.Features.Guides.Queries.GetGuideDetail;

public record GetGuideDetailQuery : IRequest<Result<GuideDetailResponse>>
{
    public string GuideId { get; init; } = string.Empty;
}
