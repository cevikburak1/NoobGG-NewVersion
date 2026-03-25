using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Chat.DTOs;

namespace NoobGg.Application.Features.Chat.Queries.GetChatHistory;

public record GetChatHistoryQuery : IRequest<Result<PagedResult<ChatMessageResponse>>>
{
    public string RoomId { get; init; } = string.Empty;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;

    /// <summary>
    /// Load messages created before this timestamp. Useful for infinite-scroll pagination.
    /// When null, loads the most recent messages.
    /// </summary>
    public DateTime? Before { get; init; }
}
