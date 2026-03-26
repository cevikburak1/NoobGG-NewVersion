using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Profiles.Commands.DeleteGameProfile;

public record DeleteGameProfileCommand : IRequest<Result>
{
    public string Id { get; init; } = string.Empty;
}
