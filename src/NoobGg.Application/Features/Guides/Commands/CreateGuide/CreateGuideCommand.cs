using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Guides.DTOs;

namespace NoobGg.Application.Features.Guides.Commands.CreateGuide;

public record CreateGuideCommand : IRequest<Result<GuideDetailResponse>>
{
    public string GameId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? CoverImageUrl { get; init; }
    public List<string> Tags { get; init; } = [];
}
