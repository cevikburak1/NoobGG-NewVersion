using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Community.Commands.CreatePost;

public record CreateCommunityPostCommand : IRequest<Result<CommunityPostResponse>>
{
    public string? BoardId { get; init; }
    public string Category { get; init; } = "Discussion";
    public string? Title { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
}
