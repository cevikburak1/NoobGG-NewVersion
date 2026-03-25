using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Auth.DTOs;

namespace NoobGg.Application.Features.Auth.Commands.Login;

public record LoginCommand : IRequest<Result<AuthResponse>>
{
    public string EmailOrUsername { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
}
