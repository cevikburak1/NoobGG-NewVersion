using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Auth.DTOs;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenCommandHandler(IMongoContext mongoContext, IJwtTokenService jwtTokenService)
    {
        _mongoContext = mongoContext;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var refreshTokens = _mongoContext.GetCollection<Domain.Entities.RefreshToken>(CollectionNames.RefreshTokens);

        var filter = Builders<Domain.Entities.RefreshToken>.Filter.Eq(rt => rt.Token, request.Token);
        var storedToken = await refreshTokens.Find(filter).FirstOrDefaultAsync(ct);

        if (storedToken is null)
            return Result<AuthResponse>.Unauthorized("Invalid refresh token");

        if (!storedToken.IsActive)
        {
            // Possible token reuse attack — revoke entire family
            if (storedToken.IsRevoked)
            {
                await RevokeAllUserTokens(storedToken.UserId, ct);
                return Result<AuthResponse>.Unauthorized("Token reuse detected — all sessions revoked");
            }

            return Result<AuthResponse>.Unauthorized("Refresh token has expired");
        }

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var user = await users.Find(Builders<User>.Filter.Eq(u => u.Id, storedToken.UserId)).FirstOrDefaultAsync(ct);
        if (user is null)
            return Result<AuthResponse>.Unauthorized("User not found");

        if (user.IsBanned)
            return Result<AuthResponse>.Fail($"Account is banned: {user.BanReason ?? "No reason provided"}", 403);

        var newRefreshTokenString = _jwtTokenService.GenerateRefreshToken();

        // Rotate: revoke old token, link to new one
        var revokeUpdate = Builders<Domain.Entities.RefreshToken>.Update
            .Set(rt => rt.RevokedAt, DateTime.UtcNow)
            .Set(rt => rt.ReplacedByToken, newRefreshTokenString)
            .Set(rt => rt.UpdatedAt, DateTime.UtcNow);
        await refreshTokens.UpdateOneAsync(
            Builders<Domain.Entities.RefreshToken>.Filter.Eq(rt => rt.Id, storedToken.Id),
            revokeUpdate,
            cancellationToken: ct);

        var newRefreshToken = new Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtTokenService.RefreshTokenExpirationDays),
            CreatedByIp = request.IpAddress
        };
        await refreshTokens.InsertOneAsync(newRefreshToken, cancellationToken: ct);

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Username, user.Role.ToString());

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtTokenService.AccessTokenExpirationMinutes),
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role.ToString(),
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt
            }
        });
    }

    private async Task RevokeAllUserTokens(string userId, CancellationToken ct)
    {
        var refreshTokens = _mongoContext.GetCollection<Domain.Entities.RefreshToken>(CollectionNames.RefreshTokens);

        var filter = Builders<Domain.Entities.RefreshToken>.Filter.And(
            Builders<Domain.Entities.RefreshToken>.Filter.Eq(rt => rt.UserId, userId),
            Builders<Domain.Entities.RefreshToken>.Filter.Eq(rt => rt.RevokedAt, null));

        var update = Builders<Domain.Entities.RefreshToken>.Update
            .Set(rt => rt.RevokedAt, DateTime.UtcNow)
            .Set(rt => rt.UpdatedAt, DateTime.UtcNow);

        await refreshTokens.UpdateManyAsync(filter, update, cancellationToken: ct);
    }
}
