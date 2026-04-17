using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Community.Commands.CreatePost;

public record CreateCommunityPostCommand : IRequest<Result<CommunityPostResponse>>
{
    public string? GameId { get; init; }
    public CommunityBoardType BoardType { get; init; } = CommunityBoardType.Game;
    public string Category { get; init; } = "Discussion";
    public string? Title { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
}
