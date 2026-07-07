using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IEmailThreadRepository : IRepository<EmailThread>
{
    /// <summary>
    /// Threads for the inbox list, newest activity first. Includes threads stamped with an
    /// empty tenant id (mailbox not mapped to a tenant) so a fresh deployment still shows mail.
    /// </summary>
    Task<IReadOnlyList<EmailThread>> ListAsync(string tenantId, string? status, CancellationToken cancellationToken = default);

    /// <summary>Fallback threading match: open thread with same normalized subject and counterpart.</summary>
    Task<EmailThread?> FindBySubjectAndCounterpartAsync(string normalizedSubject, string counterpartEmail, CancellationToken cancellationToken = default);

    /// <summary>Denormalized inbox-list fields, applied atomically when a message is added.</summary>
    Task ApplyNewMessageAsync(string threadId, string snippet, DateTime at, string direction, bool hasAttachments, CancellationToken cancellationToken = default);

    Task MarkReadAsync(string threadId, CancellationToken cancellationToken = default);

    /// <summary>Flags the thread as having unread activity again (unread count set to 1).</summary>
    Task MarkUnreadAsync(string threadId, CancellationToken cancellationToken = default);

    Task<bool> ResolveAsync(string threadId, string agentId, CancellationToken cancellationToken = default);

    /// <summary>Sets status (open/archived) clearing resolution fields when reopening.</summary>
    Task SetStatusAsync(string threadId, string status, CancellationToken cancellationToken = default);

    /// <summary>Snoozes until the given time; null clears the snooze.</summary>
    Task SetSnoozeAsync(string threadId, DateTime? until, CancellationToken cancellationToken = default);

    Task SetStarredAsync(string threadId, bool starred, CancellationToken cancellationToken = default);
}
