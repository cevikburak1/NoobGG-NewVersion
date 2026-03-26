using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Auth.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IMongoContext mongoContext,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        ILogger<RegisterCommandHandler> logger)
    {
        _mongoContext = mongoContext;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();

        var existingEmail = await users
            .Find(Builders<User>.Filter.Eq(u => u.Email, normalizedEmail))
            .FirstOrDefaultAsync(ct);
        if (existingEmail is not null)
            return Result<RegisterResponse>.Fail("Email is already registered", 409);

        var existingUsername = await users
            .Find(Builders<User>.Filter.Eq(u => u.Username, normalizedUsername))
            .FirstOrDefaultAsync(ct);
        if (existingUsername is not null)
            return Result<RegisterResponse>.Fail("Username is already taken", 409);

        var user = new User
        {
            Email = normalizedEmail,
            Username = normalizedUsername,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.User,
        };
        await users.InsertOneAsync(user, cancellationToken: ct);

        var userProfiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var profile = new UserProfile
        {
            UserId = user.Id,
            DisplayName = request.Username.Trim()
        };
        await userProfiles.InsertOneAsync(profile, cancellationToken: ct);

        await SendVerificationCodeAsync(user, ct);

        return Result<RegisterResponse>.Created(new RegisterResponse
        {
            Email = user.Email,
            Message = "A verification code has been sent to your email address"
        });
    }

    private async Task SendVerificationCodeAsync(User user, CancellationToken ct)
    {
        try
        {
            var tokens = _mongoContext.GetCollection<EmailVerificationToken>(CollectionNames.EmailVerificationTokens);

            var code = Random.Shared.Next(100_000, 999_999).ToString();

            var verificationToken = new EmailVerificationToken
            {
                UserId = user.Id,
                Token = code,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
            await tokens.InsertOneAsync(verificationToken, cancellationToken: ct);

            await _emailService.SendVerificationEmailAsync(user.Email, user.Username, code, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification code to {Email}. User was still created", user.Email);
        }
    }
}
