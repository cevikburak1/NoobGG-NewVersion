namespace NoobGg.Application.Features.Auth.DTOs;

public record UserResponse
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public bool IsEmailVerified { get; init; }
    public bool IsProfileComplete { get; init; }
    public DateTime CreatedAt { get; init; }
}
