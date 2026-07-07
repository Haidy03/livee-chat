using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using VoiceFlow.Core.Interfaces.Services;
using VoiceFlow.Infrastructure.Options;

namespace VoiceFlow.Infrastructure.ExternalServices;

public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default)
    {
        var link = ToAbsoluteLink(resetLink);
        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:520px;margin:0 auto;padding:24px;">
              <h2 style="color:#1a1a2e;">Reset your password</h2>
              <p>We received a request to reset your password. Click the button below to choose a new one. This link expires in 2 hours.</p>
              <p style="margin:28px 0;">
                <a href="{link}" style="background:#4f46e5;color:#ffffff;padding:12px 24px;border-radius:6px;text-decoration:none;display:inline-block;">Reset Password</a>
              </p>
              <p style="color:#666;font-size:13px;">If the button doesn't work, copy this link into your browser:<br/><a href="{link}">{link}</a></p>
              <p style="color:#666;font-size:13px;">If you didn't request this, you can safely ignore this email.</p>
            </div>
            """;
        var text = $"Reset your password using this link (expires in 2 hours): {link}\n\nIf you didn't request this, ignore this email.";

        return SendAsync(toEmail, "Reset your password", html, text, cancellationToken);
    }

    public Task SendWelcomeEmailAsync(string toEmail, string displayName, CancellationToken cancellationToken = default)
    {
        var loginLink = ToAbsoluteLink("/auth/login");
        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:520px;margin:0 auto;padding:24px;">
              <h2 style="color:#1a1a2e;">Welcome, {displayName}!</h2>
              <p>Your account has been created. You can sign in and start using the contact center right away.</p>
              <p style="margin:28px 0;">
                <a href="{loginLink}" style="background:#4f46e5;color:#ffffff;padding:12px 24px;border-radius:6px;text-decoration:none;display:inline-block;">Sign In</a>
              </p>
            </div>
            """;
        var text = $"Welcome, {displayName}! Your account has been created. Sign in at {loginLink}";

        return SendAsync(toEmail, $"Welcome to {_options.FromName}", html, text, cancellationToken);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, string textBody, CancellationToken cancellationToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("Email sending disabled; would have sent \"{Subject}\" to {Email}. Body: {Body}",
                subject, toEmail, textBody);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody, TextBody = textBody }.ToMessageBody();

        // Email is best-effort at every call site (signup must not roll back because SMTP is down),
        // so failures are logged rather than propagated.
        try
        {
            using var client = new SmtpClient();
            var socketOption = _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;
            await client.ConnectAsync(_options.Host, _options.Port, socketOption, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.Username))
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            _logger.LogInformation("Sent email \"{Subject}\" to {Email}", subject, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email \"{Subject}\" to {Email} via {Host}:{Port}",
                subject, toEmail, _options.Host, _options.Port);
        }
    }

    private string ToAbsoluteLink(string link)
    {
        if (Uri.IsWellFormedUriString(link, UriKind.Absolute))
            return link;

        return $"{_options.FrontendBaseUrl.TrimEnd('/')}/{link.TrimStart('/')}";
    }
}
