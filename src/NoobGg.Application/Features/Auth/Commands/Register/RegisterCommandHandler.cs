using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Auth.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterCommandHandler(
        IMongoContext mongoContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _mongoContext = mongoContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();

        var emailFilter = Builders<User>.Filter.Eq(u => u.Email, normalizedEmail);
        var existingEmail = await users.Find(emailFilter).FirstOrDefaultAsync(ct);
        if (existingEmail is not null)
            return Result<AuthResponse>.Fail("Email is already registered", 409);

        var usernameFilter = Builders<User>.Filter.Eq(u => u.Username, normalizedUsername);
        var existingUsername = await users.Find(usernameFilter).FirstOrDefaultAsync(ct);
        if (existingUsername is not null)
            return Result<AuthResponse>.Fail("Username is already taken", 409);

        var user = new User
        {
            Email = normalizedEmail,
            Username = normalizedUsername,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.User,
            LastLoginAt = DateTime.UtcNow
        };
        await users.InsertOneAsync(user, cancellationToken: ct);

        var userProfiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var profile = new UserProfile
        {
            UserId = user.Id,
            DisplayName = request.Username.Trim()
        };
        await userProfiles.InsertOneAsync(profile, cancellationToken: ct);

        var authResponse = await GenerateAuthTokens(user, request.IpAddress, ct);
        return Result<AuthResponse>.Created(authResponse);
    }

    private async Task<AuthResponse> GenerateAuthTokens(User user, string? ipAddress, CancellationToken ct)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Username, user.Role.ToString());
        var refreshTokenString = _jwtTokenService.GenerateRefreshToken();

        var refreshTokens = _mongoContext.GetCollection<Domain.Entities.RefreshToken>(CollectionNames.RefreshTokens);
        var refreshToken = new Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtTokenService.RefreshTokenExpirationDays),
            CreatedByIp = ipAddress
        };
        await refreshTokens.InsertOneAsync(refreshToken, cancellationToken: ct);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtTokenService.AccessTokenExpirationMinutes),
            User = MapToUserResponse(user)
        };
    }

    private static UserResponse MapToUserResponse(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Username = user.Username,
        Role = user.Role.ToString(),
        IsEmailVerified = user.IsEmailVerified,
        CreatedAt = user.CreatedAt
    };
}
