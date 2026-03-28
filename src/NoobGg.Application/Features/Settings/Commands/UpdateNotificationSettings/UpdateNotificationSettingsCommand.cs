using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Settings.DTOs;

namespace NoobGg.Application.Features.Settings.Commands.UpdateNotificationSettings;

public record UpdateNotificationSettingsCommand : IRequest<Result<UserSettingsResponse>>
{
    public bool NotifyFriendRequests { get; init; }
    public bool NotifyDirectMessages { get; init; }
    public bool NotifyRoomActivity { get; init; }
    public bool NotifySystemMessages { get; init; }
}
