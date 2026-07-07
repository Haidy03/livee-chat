using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using VoiceFlow.Core.Interfaces.Services;
using VoiceFlow.Infrastructure.Options;

namespace VoiceFlow.Infrastructure.ExternalServices;

/// <summary>
/// Outbound side of the digital-workspace email channel. Unlike SmtpEmailService
/// (best-effort transactional mail), send failures propagate so the agent sees the
/// error and the message is never stored as sent when SMTP rejected it.
/// The From account is resolved against Email:Inbound:Mailboxes (falling back to the
/// top-level Email:Username/Password single account).
/// </summary>
public sealed class SmtpEmailChannelSender : IEmailChannelSender
{
    private readonly EmailOptions _options;

    public SmtpEmailChannelSender(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public string ResolveMailbox(string? requested)
    {
        var account = ResolveAccount(requested);
        return account.Username;
    }

    public async Task<string> SendAsync(OutboundEmail email, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Host))
            throw new InvalidOperationException("Email sending is disabled (Email:Enabled is false).");

        var account = ResolveAccount(email.FromMailbox);
        var fromName = string.IsNullOrWhiteSpace(account.DisplayName) ? _options.FromName : account.DisplayName;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, account.Username));
        message.To.Add(new MailboxAddress(email.ToName, email.ToEmail));
        foreach (var cc in email.Cc)
        {
            if (!string.IsNullOrWhiteSpace(cc))
                message.Cc.Add(MailboxAddress.Parse(cc.Trim()));
        }
        message.Subject = email.Subject;
        message.MessageId = MimeUtils.GenerateMessageId(DomainOf(account.Username));

        if (!string.IsNullOrWhiteSpace(email.InReplyToMessageId))
            message.InReplyTo = email.InReplyToMessageId;
        foreach (var reference in email.References)
            message.References.Add(reference);

        var builder = new BodyBuilder
        {
            TextBody = email.TextBody,
            HtmlBody = string.IsNullOrWhiteSpace(email.HtmlBody) ? null : email.HtmlBody,
        };
        foreach (var attachment in email.Attachments)
        {
            builder.Attachments.Add(
                attachment.FileName,
                attachment.Content,
                ContentType.Parse(string.IsNullOrWhiteSpace(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType));
        }
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        var socketOption = _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;
        await client.ConnectAsync(_options.Host, _options.Port, socketOption, cancellationToken);

        if (!string.IsNullOrWhiteSpace(account.Username))
            await client.AuthenticateAsync(account.Username, account.Password, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        return message.MessageId;
    }

    private EmailAccountOptions ResolveAccount(string? requested)
    {
        var accounts = Accounts(_options);
        if (accounts.Count == 0)
            throw new InvalidOperationException("No email accounts configured (Email:Username or Email:Inbound:Mailboxes).");

        if (!string.IsNullOrWhiteSpace(requested))
        {
            var match = accounts.FirstOrDefault(a =>
                string.Equals(a.Username, requested, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match;
        }
        return accounts[0];
    }

    /// <summary>Configured accounts; the top-level Username/Password act as the single fallback account.</summary>
    public static List<EmailAccountOptions> Accounts(EmailOptions o)
    {
        if (o.Inbound.Mailboxes.Count > 0)
            return o.Inbound.Mailboxes.Where(m => !string.IsNullOrWhiteSpace(m.Username)).ToList();

        if (string.IsNullOrWhiteSpace(o.Username)) return [];
        return [new EmailAccountOptions { Username = o.Username, Password = o.Password, DisplayName = o.FromName }];
    }

    private static string DomainOf(string address)
    {
        var at = address.LastIndexOf('@');
        return at >= 0 && at < address.Length - 1 ? address[(at + 1)..] : "localhost";
    }
}
