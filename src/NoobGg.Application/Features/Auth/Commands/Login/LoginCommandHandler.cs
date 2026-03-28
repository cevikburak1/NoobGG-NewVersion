using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Auth.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IMongoContext mongoContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _mongoContext = mongoContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var normalizedInput = request.EmailOrUsername.Trim().ToLowerInvariant();

        var filter = Builders<User>.Filter.Or(
            Builders<User>.Filter.Eq(u => u.Email, normalizedInput),
            Builders<User>.Filter.Eq(u => u.Username, normalizedInput));

        var user = await users.Find(filter).FirstOrDefaultAsync(ct);
        if (user is null)
            return Result<AuthResponse>.Unauthorized("Invalid credentials");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Unauthorized("Invalid credentials");

        if (!user.IsEmailVerified)
            return Result<AuthResponse>.Fail("Please verify your email address before logging in", 403);

        if (user.IsBanned)
            return Result<AuthResponse>.Fail($"Account is banned: {user.BanReason ?? "No reason provided"}", 403);

        var settingsCol = _mongoContext.GetCollection<UserSettings>(CollectionNames.UserSettings);
        var userSettings = await settingsCol.Find(s => s.UserId == user.Id).FirstOrDefaultAsync(ct);
        var isDeactivated = userSettings?.IsDeactivated ?? false;

        var updateDef = Builders<User>.Update
            .Set(u => u.LastLoginAt, DateTime.UtcNow)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);
        await users.UpdateOneAsync(Builders<User>.Filter.Eq(u => u.Id, user.Id), updateDef, cancellationToken: ct);

        var profilesCol = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var profile = await profilesCol.Find(p => p.UserId == user.Id).FirstOrDefaultAsync(ct);

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Username, user.Role.ToString());
        var refreshTokenString = _jwtTokenService.GenerateRefreshToken();

        var refreshTokens = _mongoContext.GetCollection<Domain.Entities.RefreshToken>(CollectionNames.RefreshTokens);
        var refreshToken = new Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtTokenService.RefreshTokenExpirationDays),
            CreatedByIp = request.IpAddress
        };
        await refreshTokens.InsertOneAsync(refreshToken, cancellationToken: ct);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtTokenService.AccessTokenExpirationMinutes),
            User = MapToUserResponse(user, profile?.AvatarUrl),
            IsDeactivated = isDeactivated
        });
    }

    private static UserResponse MapToUserResponse(User user, string? avatarUrl) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Username = user.Username,
        Role = user.Role.ToString(),
        AvatarUrl = avatarUrl,
        IsEmailVerified = user.IsEmailVerified,
        IsProfileComplete = user.IsProfileComplete,
        CreatedAt = user.CreatedAt
    };
}
