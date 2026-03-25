using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NoobGg.Application.Common.Interfaces;

namespace NoobGg.Api.Services;

public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Username =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

    public string? Role =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
