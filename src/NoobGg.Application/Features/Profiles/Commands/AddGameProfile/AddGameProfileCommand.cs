using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Profiles.DTOs;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Profiles.Commands.AddGameProfile;

public record AddGameProfileCommand : IRequest<Result<GameProfileResponse>>
{
    public string GameId { get; init; } = string.Empty;
    public string Rank { get; init; } = string.Empty;
    public string? Role { get; init; }
    public Region Region { get; init; }
    public ExperienceLevel ExperienceLevel { get; init; }
    public CommunicationPreference CommunicationPreference { get; init; }
    public int? HoursPlayed { get; init; }
    public bool LookingForTeam { get; init; }
    public string? Note { get; init; }
    public string? InGameName { get; init; }
}
