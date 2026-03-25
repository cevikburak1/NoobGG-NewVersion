using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Auth.DTOs;

namespace NoobGg.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand : IRequest<Result<AuthResponse>>
{
    public string Token { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
}
