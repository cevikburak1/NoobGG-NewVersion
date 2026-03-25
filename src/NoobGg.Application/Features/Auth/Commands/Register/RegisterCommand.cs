using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Auth.DTOs;

namespace NoobGg.Application.Features.Auth.Commands.Register;

public record RegisterCommand : IRequest<Result<AuthResponse>>
{
    public string Email { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
}
