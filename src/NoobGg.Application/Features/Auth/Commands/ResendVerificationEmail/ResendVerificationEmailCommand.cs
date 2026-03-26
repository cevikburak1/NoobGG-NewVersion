using MediatR;
using NoobGg.Application.Common.Models;

namespace NoobGg.Application.Features.Auth.Commands.ResendVerificationEmail;

public record ResendVerificationEmailCommand : IRequest<Result>
{
    public string Email { get; init; } = string.Empty;
}
