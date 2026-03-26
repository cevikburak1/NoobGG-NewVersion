using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Profiles.DTOs;

namespace NoobGg.Application.Features.Profiles.Queries.GetProfile;

public record GetProfileQuery : IRequest<Result<ProfileDetailResponse>>
{
    public string UserId { get; init; } = string.Empty;
}
