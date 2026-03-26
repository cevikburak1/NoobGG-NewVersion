using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Notifications.DTOs;

namespace NoobGg.Application.Features.Notifications.Queries.GetNotifications;

public record GetNotificationsQuery : IRequest<Result<PagedResult<NotificationResponse>>>
{
    public bool? UnreadOnly { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
