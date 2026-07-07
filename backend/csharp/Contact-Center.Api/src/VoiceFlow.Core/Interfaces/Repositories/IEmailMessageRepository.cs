using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IEmailMessageRepository : IRepository<EmailMessage>
{
    Task<IReadOnlyList<EmailMessage>> ListByThreadAsync(string threadId, CancellationToken cancellationToken = default);

    /// <summary>Dedupe check — has this RFC message id already been ingested?</summary>
    Task<bool> ExistsByMessageIdAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>Thread containing any of the given RFC message ids (References/In-Reply-To matching).</summary>
    Task<string?> FindThreadIdByMessageIdsAsync(IReadOnlyCollection<string> messageIds, CancellationToken cancellationToken = default);

    /// <summary>Latest inbound message of a thread — supplies In-Reply-To/References for agent replies.</summary>
    Task<EmailMessage?> GetLatestInboundAsync(string threadId, CancellationToken cancellationToken = default);
}
