using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Profiles.DTOs;

namespace NoobGg.Application.Features.Profiles.Queries.GetGameProfiles;

public record GetGameProfilesQuery : IRequest<Result<List<GameProfileResponse>>>
{
    public string UserId { get; init; } = string.Empty;
}
