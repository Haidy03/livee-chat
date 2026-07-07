using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MongoDB.Bson;
using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Options;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.ExternalServices;

/// <summary>
/// Polls the channel mailbox's INBOX over IMAP and ingests new customer emails into
/// email_threads / email_messages. Threading: References/In-Reply-To against stored
/// message ids, falling back to normalized subject + sender. Sync position (last seen
/// IMAP UID per mailbox) lives in the email_sync_state collection; on the very first
/// run only the last InitialLookbackDays of history are ingested.
/// </summary>
public sealed class EmailInboundWorker : BackgroundService
{
    private const string SyncCollection = "email_sync_state";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<EmailOptions> _options;
    private readonly ILogger<EmailInboundWorker> _logger;

    public EmailInboundWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<EmailOptions> options,
        ILogger<EmailInboundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var o = _options.Value;
        if (!o.Inbound.Enabled)
        {
            _logger.LogInformation("Email inbound worker disabled (Email:Inbound:Enabled = false).");
            return;
        }

        var accounts = SmtpEmailChannelSender.Accounts(o);
        if (accounts.Count == 0)
        {
            _logger.LogWarning("Email inbound worker not started: no accounts configured (Email:Username or Email:Inbound:Mailboxes).");
            return;
        }

        _logger.LogInformation("Email inbound worker polling {Host} for {Count} mailbox(es) every {Seconds}s.",
            o.Inbound.ImapHost, accounts.Count, o.Inbound.PollSeconds);

        var delay = TimeSpan.FromSeconds(Math.Max(10, o.Inbound.PollSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var account in accounts)
            {
                try
                {
                    await PollAccountAsync(o, account, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Email inbound poll failed for {Mailbox}; retrying in {Seconds}s.",
                        account.Username, delay.TotalSeconds);
                }
            }

            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task PollAccountAsync(EmailOptions o, EmailAccountOptions account, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var threads = scope.ServiceProvider.GetRequiredService<IEmailThreadRepository>();
        var messages = scope.ServiceProvider.GetRequiredService<IEmailMessageRepository>();
        var mongo = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var syncState = mongo.GetCollection<BsonDocument>(SyncCollection);

        using var client = new ImapClient();
        await client.ConnectAsync(o.Inbound.ImapHost, o.Inbound.ImapPort, SecureSocketOptions.SslOnConnect, ct);
        await client.AuthenticateAsync(account.Username, account.Password, ct);

        var folders = o.Inbound.ImapFolders.Count > 0 ? o.Inbound.ImapFolders : ["INBOX"];
        foreach (var folderName in folders)
        {
            ct.ThrowIfCancellationRequested();

            IMailFolder folder;
            try
            {
                folder = string.Equals(folderName, "INBOX", StringComparison.OrdinalIgnoreCase)
                    ? client.Inbox
                    : await client.GetFolderAsync(folderName, ct);
            }
            catch (FolderNotFoundException)
            {
                _logger.LogWarning("IMAP folder {Folder} not found for {Mailbox}; skipping.", folderName, account.Username);
                continue;
            }
            await folder.OpenAsync(FolderAccess.ReadOnly, ct);

            // INBOX keeps its legacy sync key (plain mailbox address); other folders are scoped.
            var syncKey = string.Equals(folderName, "INBOX", StringComparison.OrdinalIgnoreCase)
                ? account.Username
                : $"{account.Username}:{folderName}";
            var lastUid = await GetLastUidAsync(syncState, syncKey, ct);

            IList<UniqueId> uids;
            if (lastUid == 0)
            {
                uids = await folder.SearchAsync(
                    SearchQuery.DeliveredAfter(DateTime.UtcNow.AddDays(-Math.Max(1, o.Inbound.InitialLookbackDays))), ct);
            }
            else
            {
                // start:* can echo back the highest existing UID even when it's below start,
                // so results are re-filtered against lastUid below.
                var range = new UniqueIdRange(new UniqueId((uint)lastUid + 1), UniqueId.MaxValue);
                uids = await folder.SearchAsync(SearchQuery.Uids(range), ct);
            }

            var maxUid = lastUid;
            foreach (var uid in uids.OrderBy(u => u.Id))
            {
                if (uid.Id <= lastUid) continue;
                ct.ThrowIfCancellationRequested();

                var mime = await folder.GetMessageAsync(uid, ct);
                await IngestAsync(o, account, folder.FullName, threads, messages, mime, uid.Id, ct);
                maxUid = Math.Max(maxUid, uid.Id);
            }

            if (maxUid > lastUid)
                await SetLastUidAsync(syncState, syncKey, maxUid, ct);

            await folder.CloseAsync(cancellationToken: ct);
        }

        await client.DisconnectAsync(quit: true, ct);
    }

    private async Task IngestAsync(
        EmailOptions o,
        EmailAccountOptions account,
        string folderName,
        IEmailThreadRepository threads,
        IEmailMessageRepository messages,
        MimeMessage mime,
        uint uid,
        CancellationToken ct)
    {
        var from = mime.From.Mailboxes.FirstOrDefault();
        if (from is null) return;

        // Our own replies (e.g. BCC'd back or moved into INBOX) are already stored on send.
        if (string.Equals(from.Address, account.Username, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(from.Address, o.FromAddress, StringComparison.OrdinalIgnoreCase))
            return;

        var messageId = Clean(mime.MessageId) ?? $"vf-uid-{account.Username}-{uid}";
        if (await messages.ExistsByMessageIdAsync(messageId, ct)) return;

        var subject = string.IsNullOrWhiteSpace(mime.Subject) ? "(no subject)" : mime.Subject.Trim();
        var normalizedSubject = NormalizeSubject(subject);
        var fromName = string.IsNullOrWhiteSpace(from.Name) ? from.Address : from.Name;
        var textBody = mime.TextBody ?? StripHtml(mime.HtmlBody) ?? string.Empty;
        var attachmentNames = mime.Attachments
            .Select(a => (a as MimePart)?.FileName ?? a.ContentType.Name ?? "attachment")
            .ToList();

        var references = mime.References.Select(Clean).OfType<string>().ToList();
        var inReplyTo = Clean(mime.InReplyTo);
        var lookup = new List<string>(references);
        if (inReplyTo is not null && !lookup.Contains(inReplyTo)) lookup.Add(inReplyTo);

        var threadId = await messages.FindThreadIdByMessageIdsAsync(lookup, ct);
        EmailThread? thread = threadId is null ? null : await threads.GetByIdAsync(threadId, ct);

        thread ??= await threads.FindBySubjectAndCounterpartAsync(normalizedSubject, from.Address, ct);

        if (thread is null)
        {
            thread = new EmailThread
            {
                Id = ObjectId.GenerateNewId().ToString(),
                TenantId = o.Inbound.TenantId,
                Subject = subject,
                NormalizedSubject = normalizedSubject,
                CounterpartName = fromName,
                CounterpartEmail = from.Address,
                Mailbox = account.Username,
                LastMessageAt = mime.Date.UtcDateTime,
            };
            await threads.InsertAsync(thread, ct);
            _logger.LogInformation("Email thread created for {From}: {Subject}", from.Address, subject);
        }

        var message = new EmailMessage
        {
            Id = ObjectId.GenerateNewId().ToString(),
            TenantId = thread.TenantId,
            ThreadId = thread.Id,
            Direction = "inbound",
            MessageId = messageId,
            InReplyTo = inReplyTo,
            References = references,
            FromName = fromName,
            FromEmail = from.Address,
            ToEmail = account.Username,
            CcEmails = mime.Cc.Mailboxes.Select(m => m.Address).ToList(),
            Subject = subject,
            TextBody = textBody,
            HtmlBody = mime.HtmlBody,
            AttachmentNames = attachmentNames,
            SentAt = mime.Date.UtcDateTime,
            ImapUid = uid,
            ImapFolder = folderName,
        };
        await messages.InsertAsync(message, ct);

        await threads.ApplyNewMessageAsync(
            thread.Id, Snippet(textBody), message.SentAt, "inbound", attachmentNames.Count > 0, ct);
    }

    private static async Task<long> GetLastUidAsync(
        IMongoCollection<BsonDocument> col, string mailbox, CancellationToken ct)
    {
        var doc = await col.Find(new BsonDocument("_id", mailbox)).FirstOrDefaultAsync(ct);
        return doc is null ? 0 : doc.GetValue("lastUid", 0).ToInt64();
    }

    private static Task SetLastUidAsync(
        IMongoCollection<BsonDocument> col, string mailbox, long uid, CancellationToken ct)
    {
        return col.UpdateOneAsync(
            new BsonDocument("_id", mailbox),
            Builders<BsonDocument>.Update
                .Set("lastUid", uid)
                .Set("updatedAt", DateTime.UtcNow),
            new UpdateOptions { IsUpsert = true },
            ct);
    }

    private static string? Clean(string? messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId)) return null;
        return messageId.Trim().TrimStart('<').TrimEnd('>');
    }

    internal static string NormalizeSubject(string subject)
    {
        var s = subject.Trim();
        while (true)
        {
            var m = System.Text.RegularExpressions.Regex.Match(
                s, @"^(re|fw|fwd)\s*:\s*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success) break;
            s = s[m.Length..].Trim();
        }
        return s.ToLowerInvariant();
    }

    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        return System.Net.WebUtility.HtmlDecode(text).Trim();
    }

    private static string Snippet(string body)
    {
        var text = body.ReplaceLineEndings(" ").Trim();
        return text.Length <= 140 ? text : text[..140];
    }
}
