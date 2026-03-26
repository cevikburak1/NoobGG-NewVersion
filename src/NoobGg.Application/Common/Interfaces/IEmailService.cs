namespace NoobGg.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string username, string verificationToken, CancellationToken ct = default);
    Task SendPasswordResetEmailAsync(string toEmail, string username, string resetToken, CancellationToken ct = default);
}
