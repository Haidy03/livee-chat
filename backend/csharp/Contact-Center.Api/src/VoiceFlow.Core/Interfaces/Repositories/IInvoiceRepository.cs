using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Enums;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<(IEnumerable<Invoice> Items, long TotalCount)> SearchAsync(string tenantId, InvoiceStatus? status, DateTime? from, DateTime? to, int skip, int take, CancellationToken cancellationToken = default);
}
