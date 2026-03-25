namespace NoobGg.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(string userId, string username, string role);
    string GenerateRefreshToken();
    (string UserId, string Username, string Role)? ValidateToken(string token);
    int AccessTokenExpirationMinutes { get; }
    int RefreshTokenExpirationDays { get; }
}
