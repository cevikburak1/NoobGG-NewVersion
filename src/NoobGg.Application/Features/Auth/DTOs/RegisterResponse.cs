namespace NoobGg.Application.Features.Auth.DTOs;

public record RegisterResponse
{
    public string Email { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
