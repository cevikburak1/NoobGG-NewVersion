using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Settings.DTOs;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Settings.Commands.UpdatePrivacySettings;

public record UpdatePrivacySettingsCommand : IRequest<Result<UserSettingsResponse>>
{
    public ProfileVisibility ProfileVisibility { get; init; }
    public DmPermission DmPermission { get; init; }
    public bool ShowOnlineStatus { get; init; }
    public bool DefaultLookingForTeam { get; init; }
}
