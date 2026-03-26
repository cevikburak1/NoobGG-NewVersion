using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Profiles.DTOs;

namespace NoobGg.Application.Features.Profiles.Commands.UpdateProfile;

public record UpdateProfileCommand : IRequest<Result<ProfileDetailResponse>>
{
    public string? DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public string? Country { get; init; }
    public string? Timezone { get; init; }
    public string? WeekdaysFrom { get; init; }
    public string? WeekdaysTo { get; init; }
    public string? WeekendsFrom { get; init; }
    public string? WeekendsTo { get; init; }
}
