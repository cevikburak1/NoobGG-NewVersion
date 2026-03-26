using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Auth.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Auth.Commands.VerifyEmail;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<AuthResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly IJwtTokenService _jwtTokenService;

    public VerifyEmailCommandHandler(IMongoContext mongoContext, IJwtTokenService jwtTokenService)
    {
        _mongoContext = mongoContext;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthResponse>> Handle(VerifyEmailCommand request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var user = await users
            .Find(Builders<User>.Filter.Eq(u => u.Email, normalizedEmail))
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return Result<AuthResponse>.Fail("Invalid email or verification code");

        if (user.IsEmailVerified)
            return Result<AuthResponse>.Fail("Email is already verified");

        var tokens = _mongoContext.GetCollection<EmailVerificationToken>(CollectionNames.EmailVerificationTokens);
        var tokenFilter = Builders<EmailVerificationToken>.Filter.And(
            Builders<EmailVerificationToken>.Filter.Eq(t => t.UserId, user.Id),
            Builders<EmailVerificationToken>.Filter.Eq(t => t.Token, request.Code.Trim()),
            Builders<EmailVerificationToken>.Filter.Eq(t => t.IsUsed, false),
            Builders<EmailVerificationToken>.Filter.Gt(t => t.ExpiresAt, DateTime.UtcNow)
        );

        var verificationToken = await tokens.Find(tokenFilter).FirstOrDefaultAsync(ct);
        if (verificationToken is null)
            return Result<AuthResponse>.Fail("Invalid or expired verification code");

        var userUpdate = Builders<User>.Update
            .Set(u => u.IsEmailVerified, true)
            .Set(u => u.LastLoginAt, DateTime.UtcNow)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);
        await users.UpdateOneAsync(Builders<User>.Filter.Eq(u => u.Id, user.Id), userUpdate, cancellationToken: ct);

        var tokenUpdate = Builders<EmailVerificationToken>.Update
            .Set(t => t.IsUsed, true)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);
        await tokens.UpdateOneAsync(tokenFilter, tokenUpdate, cancellationToken: ct);

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
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role.ToString(),
                IsEmailVerified = true,
                IsProfileComplete = user.IsProfileComplete,
                CreatedAt = user.CreatedAt
            }
        });
    }
}
