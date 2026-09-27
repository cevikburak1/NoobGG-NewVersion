using NoobGg.Application.Common.Interfaces;

namespace NoobGg.Api.Services;

public sealed class DevelopmentEmailService(ILogger<DevelopmentEmailService> logger) : IEmailService
{
    public Task SendVerificationEmailAsync(string toEmail, string username, string verificationCode, CancellationToken ct = default)
    {
        logger.LogInformation("Development verification code for {Email}: {Code}", toEmail, verificationCode);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string username, string resetToken, CancellationToken ct = default)
    {
        logger.LogInformation("Development password reset token for {Email}: {Token}", toEmail, resetToken);
        return Task.CompletedTask;
    }
}
