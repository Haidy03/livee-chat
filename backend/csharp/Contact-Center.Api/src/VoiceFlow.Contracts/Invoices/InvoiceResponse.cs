using VoiceFlow.Core.Enums;

namespace VoiceFlow.Contracts.Invoices;

public sealed class InvoiceResponse
{
    public string Id { get; init; } = string.Empty;
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateTime IssueDate { get; init; }
    public DateTime? DueDate { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public InvoiceStatus Status { get; init; }
    public DateTime? PaidAt { get; init; }
    public string? PdfUrl { get; init; }
}
