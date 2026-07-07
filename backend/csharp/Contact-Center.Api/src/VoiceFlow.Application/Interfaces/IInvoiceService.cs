using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Invoices;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Enums;

namespace VoiceFlow.Application.Interfaces;

public interface IInvoiceService
{
    Task<Result<PagedResponse<InvoiceResponse>>> GetInvoicesAsync(string tenantId, InvoiceStatus? status, DateTime? from, DateTime? to, PaginationRequest pagination, CancellationToken cancellationToken = default);
    Task<Result<InvoiceResponse>> GetInvoiceAsync(string invoiceId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<InvoiceResponse>> CreateInvoiceAsync(string tenantId, string userId, CreateInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<Result<InvoiceResponse>> UpdateInvoiceAsync(string invoiceId, string tenantId, UpdateInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteInvoiceAsync(string invoiceId, string tenantId, CancellationToken cancellationToken = default);
}
