using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Domain.Entities;

namespace NoobGg.Application.Features.Auth.Commands.ResendVerificationEmail;

public class ResendVerificationEmailCommandHandler : IRequestHandler<ResendVerificationEmailCommand, Result>
{
    private readonly IMongoContext _mongoContext;
    private readonly IEmailService _emailService;

    public ResendVerificationEmailCommandHandler(
        IMongoContext mongoContext,
        IEmailService emailService)
    {
        _mongoContext = mongoContext;
        _emailService = emailService;
    }

    public async Task<Result> Handle(ResendVerificationEmailCommand request, CancellationToken ct)
    {
        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await users
            .Find(Builders<User>.Filter.Eq(u => u.Email, normalizedEmail))
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return Result.Success();

        if (user.IsEmailVerified)
            return Result.Fail("Email is already verified");

        var tokens = _mongoContext.GetCollection<EmailVerificationToken>(CollectionNames.EmailVerificationTokens);

        var invalidateFilter = Builders<EmailVerificationToken>.Filter.And(
            Builders<EmailVerificationToken>.Filter.Eq(t => t.UserId, user.Id),
            Builders<EmailVerificationToken>.Filter.Eq(t => t.IsUsed, false)
        );
        var invalidateUpdate = Builders<EmailVerificationToken>.Update.Set(t => t.IsUsed, true);
        await tokens.UpdateManyAsync(invalidateFilter, invalidateUpdate, cancellationToken: ct);

        var verificationToken = new EmailVerificationToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        await tokens.InsertOneAsync(verificationToken, cancellationToken: ct);

        await _emailService.SendVerificationEmailAsync(user.Email, user.Username, verificationToken.Token, ct);

        return Result.Success();
    }
}
