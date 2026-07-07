using System.ComponentModel.DataAnnotations;
using VoiceFlow.Core.Enums;

namespace VoiceFlow.Contracts.Invoices;

public sealed class CreateInvoiceRequest
{
    [Required]
    public string InvoiceNumber { get; set; } = string.Empty;
    [Required]
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
}
