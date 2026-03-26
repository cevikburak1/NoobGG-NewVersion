using MediatR;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Auth.DTOs;

namespace NoobGg.Application.Features.Auth.Commands.VerifyEmail;

public record VerifyEmailCommand : IRequest<Result<AuthResponse>>
{
    public string Email { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
}
