using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.DirectMessages.DTOs;

namespace NoobGg.Application.Features.DirectMessages.Queries.GetMessages;

public record GetMessagesQuery : IRequest<Result<List<DirectMessageResponse>>>
{
    public string ConversationId { get; init; } = string.Empty;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
