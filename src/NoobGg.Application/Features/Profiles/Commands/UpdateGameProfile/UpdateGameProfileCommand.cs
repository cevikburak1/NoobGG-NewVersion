using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Profiles.DTOs;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Profiles.Commands.UpdateGameProfile;

public record UpdateGameProfileCommand : IRequest<Result<GameProfileResponse>>
{
    public string Id { get; init; } = string.Empty;
    public string? Rank { get; init; }
    public string? Role { get; init; }
    public Region? Region { get; init; }
    public ExperienceLevel? ExperienceLevel { get; init; }
    public CommunicationPreference? CommunicationPreference { get; init; }
    public int? HoursPlayed { get; init; }
    public bool? LookingForTeam { get; init; }
    public string? Note { get; init; }
    public string? InGameName { get; init; }
}
