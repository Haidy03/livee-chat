using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Enums;

namespace VoiceFlow.Core.Entities;

[BsonIgnoreExtraElements]
public sealed class Invoice : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [BsonElement("issueDate")]
    public DateTime IssueDate { get; set; }

    [BsonElement("dueDate")]
    public DateTime? DueDate { get; set; }

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "SAR";

    [BsonElement("status")]
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;

    [BsonElement("paidAt")]
    public DateTime? PaidAt { get; set; }

    [BsonElement("pdfUrl")]
    public string? PdfUrl { get; set; }
}
