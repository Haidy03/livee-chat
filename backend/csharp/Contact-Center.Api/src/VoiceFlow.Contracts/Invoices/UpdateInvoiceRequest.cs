using VoiceFlow.Core.Enums;

namespace VoiceFlow.Contracts.Invoices;

public sealed class UpdateInvoiceRequest
{
    public InvoiceStatus? Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PdfUrl { get; set; }
}
