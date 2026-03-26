using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.DirectMessages.DTOs;

namespace NoobGg.Application.Features.DirectMessages.Commands.CreateConversation;

public record CreateConversationCommand : IRequest<Result<ConversationResponse>>
{
    public string ParticipantId { get; init; } = string.Empty;
}
