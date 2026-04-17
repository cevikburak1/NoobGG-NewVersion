using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;

namespace NoobGg.Application.Features.Community.Commands.AddComment;

public record AddCommunityCommentCommand : IRequest<Result<CommunityCommentResponse>>
{
    public string PostId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}
