using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IContactRepository : IRepository<Contact>
{
    Task<(IEnumerable<Contact> Items, long TotalCount)> SearchAsync(string tenantId, string? query, IEnumerable<string>? tagIds, int skip, int take, CancellationToken cancellationToken = default);
}
