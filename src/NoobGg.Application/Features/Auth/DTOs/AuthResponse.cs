namespace NoobGg.Application.Features.Auth.DTOs;

public record AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserResponse User { get; init; } = null!;
}
