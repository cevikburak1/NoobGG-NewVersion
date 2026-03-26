using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Notifications.Commands.MarkNotificationRead;

public record MarkNotificationReadCommand : IRequest<Result>
{
    public string NotificationId { get; init; } = string.Empty;
}
