namespace VoiceFlow.Core.Interfaces.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(string toEmail, string displayName, CancellationToken cancellationToken = default);
}
