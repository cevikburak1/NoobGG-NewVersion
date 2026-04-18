using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;

namespace NoobGg.Application.Features.Community.Commands.CreateBoard;

public record CreateCommunityBoardCommand : IRequest<Result<CommunityBoardResponse>>
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = "General";
    public string? Slug { get; init; }
    public string? GameId { get; init; }
    public string? CoverImageUrl { get; init; }
}
