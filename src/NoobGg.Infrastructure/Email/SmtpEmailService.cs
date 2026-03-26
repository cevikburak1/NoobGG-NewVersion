using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using NoobGg.Application.Common.Interfaces;

namespace NoobGg.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string username, string verificationCode, CancellationToken ct = default)
    {
        var htmlBody = BuildVerificationEmailHtml(username, verificationCode);
        await SendAsync(toEmail, "Your NoobGg verification code", htmlBody, ct);
        _logger.LogInformation("Verification email sent to {Email}", toEmail);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string username, string resetToken, CancellationToken ct = default)
    {
        var htmlBody = BuildPasswordResetEmailHtml(username, resetToken);
        await SendAsync(toEmail, "Reset your NoobGg password", htmlBody, ct);
        _logger.LogInformation("Password reset email sent to {Email}", toEmail);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var secureOption = _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, secureOption, ct);
            await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword, ct);
            await client.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} via SMTP {Host}:{Port}", toEmail, _settings.SmtpHost, _settings.SmtpPort);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }

    private static string BuildVerificationEmailHtml(string username, string code)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8"/>
            <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
        </head>
        <body style="margin:0;padding:0;background-color:#0f0f23;font-family:'Segoe UI',Roboto,Arial,sans-serif;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#0f0f23;padding:40px 20px;">
                <tr>
                    <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" style="background-color:#1a1a2e;border-radius:12px;overflow:hidden;">
                            <tr>
                                <td style="background:linear-gradient(135deg,#6c5ce7,#a855f7);padding:32px;text-align:center;">
                                    <h1 style="color:#ffffff;margin:0;font-size:28px;font-weight:700;">NoobGg</h1>
                                    <p style="color:#e0d4ff;margin:8px 0 0;font-size:14px;">Find Your Squad, Win Together</p>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding:40px 32px;">
                                    <h2 style="color:#ffffff;margin:0 0 16px;font-size:22px;">Welcome, {username}!</h2>
                                    <p style="color:#a0a0b8;font-size:16px;line-height:1.6;margin:0 0 24px;">
                                        Thanks for signing up for NoobGg. Enter the code below to verify your email and activate your account.
                                    </p>
                                    <table width="100%" cellpadding="0" cellspacing="0">
                                        <tr>
                                            <td align="center" style="padding:8px 0 32px;">
                                                <div style="display:inline-block;background-color:#12121f;border:2px solid #6c5ce7;border-radius:12px;padding:20px 48px;">
                                                    <span style="font-size:36px;font-weight:700;letter-spacing:12px;color:#ffffff;font-family:'Courier New',monospace;">{code}</span>
                                                </div>
                                            </td>
                                        </tr>
                                    </table>
                                    <p style="color:#6b6b80;font-size:13px;margin:0;">
                                        This code will expire in 24 hours. If you didn't create an account, you can safely ignore this email.
                                    </p>
                                </td>
                            </tr>
                            <tr>
                                <td style="background-color:#12121f;padding:20px 32px;text-align:center;">
                                    <p style="color:#4a4a5e;font-size:12px;margin:0;">&copy; 2025 NoobGg. All rights reserved.</p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }

    private static string BuildPasswordResetEmailHtml(string username, string resetUrl)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8"/>
            <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
        </head>
        <body style="margin:0;padding:0;background-color:#0f0f23;font-family:'Segoe UI',Roboto,Arial,sans-serif;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#0f0f23;padding:40px 20px;">
                <tr>
                    <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" style="background-color:#1a1a2e;border-radius:12px;overflow:hidden;">
                            <tr>
                                <td style="background:linear-gradient(135deg,#6c5ce7,#a855f7);padding:32px;text-align:center;">
                                    <h1 style="color:#ffffff;margin:0;font-size:28px;font-weight:700;">NoobGg</h1>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding:40px 32px;">
                                    <h2 style="color:#ffffff;margin:0 0 16px;font-size:22px;">Password Reset</h2>
                                    <p style="color:#a0a0b8;font-size:16px;line-height:1.6;margin:0 0 24px;">
                                        Hi {username}, we received a request to reset your password.
                                    </p>
                                    <table width="100%" cellpadding="0" cellspacing="0">
                                        <tr>
                                            <td align="center" style="padding:8px 0 32px;">
                                                <a href="{resetUrl}"
                                                   style="display:inline-block;background:linear-gradient(135deg,#6c5ce7,#a855f7);color:#ffffff;text-decoration:none;padding:14px 40px;border-radius:8px;font-size:16px;font-weight:600;">
                                                    Reset Password
                                                </a>
                                            </td>
                                        </tr>
                                    </table>
                                    <p style="color:#6b6b80;font-size:13px;margin:0;">
                                        This link will expire in 1 hour. If you didn't request this, you can safely ignore this email.
                                    </p>
                                </td>
                            </tr>
                            <tr>
                                <td style="background-color:#12121f;padding:20px 32px;text-align:center;">
                                    <p style="color:#4a4a5e;font-size:12px;margin:0;">&copy; 2025 NoobGg. All rights reserved.</p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }
}
