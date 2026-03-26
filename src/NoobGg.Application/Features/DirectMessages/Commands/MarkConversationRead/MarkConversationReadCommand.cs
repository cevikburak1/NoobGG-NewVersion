using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.DirectMessages.Commands.MarkConversationRead;

public record MarkConversationReadCommand : IRequest<Result>
{
    public string ConversationId { get; init; } = string.Empty;
}
