namespace VoiceFlow.Infrastructure.Options;

/// <summary>
/// Bound from the "Email" configuration section. Configured for Google Workspace by default
/// (smtp.gmail.com:587 + STARTTLS with a per-mailbox App Password, or smtp-relay.gmail.com
/// when the Admin console SMTP relay is enabled for the server's IP).
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>When false the service logs the email instead of sending (safe for local dev).</summary>
    public bool Enabled { get; init; }

    public string Host { get; init; } = "smtp.gmail.com";

    public int Port { get; init; } = 587;

    /// <summary>STARTTLS on 587. Set false only for port 465 (implicit TLS) setups.</summary>
    public bool UseStartTls { get; init; } = true;

    /// <summary>SMTP login — the Workspace mailbox address. Leave empty for IP-allowlisted smtp-relay.gmail.com.</summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>App Password for the mailbox. Supply via env var Email__Password, never commit it.</summary>
    public string Password { get; init; } = string.Empty;

    public string FromAddress { get; init; } = string.Empty;

    public string FromName { get; init; } = "VoiceFlow Contact Center";

    /// <summary>
    /// Public base URL of the frontend (e.g. https://contact-center.alkhwarizmi.pro). Prefixed onto
    /// relative links (password reset) so emailed links are absolute.
    /// </summary>
    public string FrontendBaseUrl { get; init; } = string.Empty;

    public EmailInboundOptions Inbound { get; init; } = new();
}

/// <summary>
/// Digital-workspace email channel: IMAP polling of the mailbox's INBOX. Uses the same
/// Username/Password (Gmail App Password) as outbound SMTP.
/// </summary>
public sealed class EmailInboundOptions
{
    /// <summary>Master switch for the IMAP poller (independent of outbound Enabled).</summary>
    public bool Enabled { get; init; }

    public string ImapHost { get; init; } = "imap.gmail.com";

    public int ImapPort { get; init; } = 993;

    public int PollSeconds { get; init; } = 30;

    /// <summary>On the very first sync only, how far back to ingest existing mailbox history.</summary>
    public int InitialLookbackDays { get; init; } = 7;

    /// <summary>
    /// IMAP folders to ingest. Gmail: "INBOX" plus e.g. "[Gmail]/Spam" or a label folder.
    /// Own sent mail is skipped automatically, so adding Sent is unnecessary.
    /// </summary>
    public List<string> ImapFolders { get; init; } = ["INBOX"];

    /// <summary>
    /// Tenant that owns the mailbox's conversations. When empty, threads are stamped with an
    /// empty tenant id and every tenant can see them (fine for single-org deployments).
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// Channel mailbox accounts. When empty, the top-level Email:Username/Password act as
    /// the single account. Each is polled over IMAP and can be chosen as the From account.
    /// </summary>
    public List<EmailAccountOptions> Mailboxes { get; init; } = [];
}

public sealed class EmailAccountOptions
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    /// <summary>Shown as the From display name; falls back to Email:FromName.</summary>
    public string DisplayName { get; init; } = string.Empty;
}
