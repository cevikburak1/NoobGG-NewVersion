using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.DirectMessages.DTOs;

namespace NoobGg.Application.Features.DirectMessages.Commands.SendDirectMessage;

public record SendDirectMessageCommand : IRequest<Result<DirectMessageResponse>>
{
    public string ConversationId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}
