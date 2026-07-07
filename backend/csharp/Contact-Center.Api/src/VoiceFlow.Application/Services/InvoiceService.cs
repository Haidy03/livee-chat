using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Invoices;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Enums;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Application.Services;

public sealed class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _repository;

    public InvoiceService(IInvoiceRepository repository) => _repository = repository;

    public async Task<Result<PagedResponse<InvoiceResponse>>> GetInvoicesAsync(string tenantId, InvoiceStatus? status, DateTime? from, DateTime? to, PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _repository.SearchAsync(tenantId, status, from, to, pagination.Skip, pagination.PageSize, cancellationToken);
        return PagedResponse<InvoiceResponse>.Create(items.Select(MapToResponse).ToList().AsReadOnly(), pagination.Page, pagination.PageSize, total);
    }

    public async Task<Result<InvoiceResponse>> GetInvoiceAsync(string invoiceId, string tenantId, CancellationToken cancellationToken = default)
    {
        var invoice = await _repository.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice is null || invoice.TenantId != tenantId)
            return Result.Failure<InvoiceResponse>(Error.NotFound("Invoice", invoiceId));
        return MapToResponse(invoice);
    }

    public async Task<Result<InvoiceResponse>> CreateInvoiceAsync(string tenantId, string userId, CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = new Invoice { TenantId = tenantId, UserId = userId, InvoiceNumber = request.InvoiceNumber, IssueDate = request.IssueDate, DueDate = request.DueDate, Amount = request.Amount, Currency = request.Currency, Status = request.Status };
        await _repository.InsertAsync(invoice, cancellationToken);
        return MapToResponse(invoice);
    }

    public async Task<Result<InvoiceResponse>> UpdateInvoiceAsync(string invoiceId, string tenantId, UpdateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await _repository.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice is null || invoice.TenantId != tenantId)
            return Result.Failure<InvoiceResponse>(Error.NotFound("Invoice", invoiceId));

        if (request.Status.HasValue) invoice.Status = request.Status.Value;
        if (request.PaidAt.HasValue) invoice.PaidAt = request.PaidAt;
        if (request.PdfUrl is not null) invoice.PdfUrl = request.PdfUrl;

        await _repository.UpdateAsync(invoice, cancellationToken);
        return MapToResponse(invoice);
    }

    public async Task<Result> DeleteInvoiceAsync(string invoiceId, string tenantId, CancellationToken cancellationToken = default)
    {
        var invoice = await _repository.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice is null || invoice.TenantId != tenantId)
            return Result.Failure(Error.NotFound("Invoice", invoiceId));
        await _repository.DeleteAsync(invoiceId, cancellationToken);
        return Result.Success();
    }

    private static InvoiceResponse MapToResponse(Invoice i) => new() { Id = i.Id, InvoiceNumber = i.InvoiceNumber, IssueDate = i.IssueDate, DueDate = i.DueDate, Amount = i.Amount, Currency = i.Currency, Status = i.Status, PaidAt = i.PaidAt, PdfUrl = i.PdfUrl };
}
