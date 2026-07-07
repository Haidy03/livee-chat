using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.Email;

public sealed class EmailThreadResponse
{
    public string Id { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string CounterpartName { get; set; } = string.Empty;
    public string CounterpartEmail { get; set; } = string.Empty;
    public string Mailbox { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public string LastMessageSnippet { get; set; } = string.Empty;
    public string LastMessageDirection { get; set; } = "inbound";
    public bool LastMessageHasAttachments { get; set; }
    public int MessageCount { get; set; }
    public int UnreadCount { get; set; }
    public string Status { get; set; } = "open";
    public string? AssignedTo { get; set; }
    public DateTime? SnoozedUntil { get; set; }
    public bool Starred { get; set; }
}

public sealed class EmailMessageResponse
{
    public string Id { get; set; } = string.Empty;
    public string ThreadId { get; set; } = string.Empty;
    public string Direction { get; set; } = "inbound";
    public string FromName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string ToEmail { get; set; } = string.Empty;
    public List<string> CcEmails { get; set; } = [];
    public string Subject { get; set; } = string.Empty;
    public string TextBody { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public List<string> AttachmentNames { get; set; } = [];
    public DateTime SentAt { get; set; }
    public string? AgentId { get; set; }
    public string? AgentName { get; set; }
}

public sealed class EmailAttachmentUpload
{
    [Required(AllowEmptyStrings = false)]
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>File bytes, base64 encoded.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Base64Content { get; set; } = string.Empty;
}

public sealed class SendEmailReplyRequest
{
    [Required(AllowEmptyStrings = false)]
    public string Body { get; set; } = string.Empty;

    /// <summary>Optional HTML version of the body (rich composer output).</summary>
    public string? HtmlBody { get; set; }

    public List<string> Cc { get; set; } = [];

    public List<EmailAttachmentUpload> Attachments { get; set; } = [];
}

public sealed class ComposeEmailRequest
{
    /// <summary>Sending account; defaults to the first configured mailbox.</summary>
    public string? Mailbox { get; set; }

    [Required]
    [EmailAddress]
    public string To { get; set; } = string.Empty;

    public string? ToName { get; set; }

    public List<string> Cc { get; set; } = [];

    [Required(AllowEmptyStrings = false)]
    public string Subject { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Body { get; set; } = string.Empty;

    public string? HtmlBody { get; set; }

    public List<EmailAttachmentUpload> Attachments { get; set; } = [];
}

public sealed class EmailSignatureResponse
{
    public string Html { get; set; } = string.Empty;
}

public sealed class UpdateEmailSignatureRequest
{
    /// <summary>HTML signature; empty clears it.</summary>
    public string Html { get; set; } = string.Empty;
}

public sealed class StarEmailThreadRequest
{
    public bool Starred { get; set; }
}

public sealed class SnoozeEmailThreadRequest
{
    /// <summary>UTC time to snooze until; null clears the snooze.</summary>
    public DateTime? Until { get; set; }
}

public sealed class EmailMailboxResponse
{
    public string Address { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
