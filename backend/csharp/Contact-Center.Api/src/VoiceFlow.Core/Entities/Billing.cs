using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.ValueObjects;

namespace VoiceFlow.Core.Entities;

[BsonIgnoreExtraElements]
public sealed class Billing : Entity
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    // General
    [BsonElement("invoiceName")]
    public string InvoiceName { get; set; } = "<default>";

    [BsonElement("billingEmails")]
    public string BillingEmails { get; set; } = string.Empty;

    [BsonElement("billingAddress")]
    public string BillingAddress { get; set; } = string.Empty;

    [BsonElement("billingCountry")]
    public string BillingCountry { get; set; } = string.Empty;

    [BsonElement("vatNumber")]
    public string VatNumber { get; set; } = string.Empty;

    [BsonElement("registrationNumber")]
    public string RegistrationNumber { get; set; } = string.Empty;

    [BsonElement("sendInvoicesToAdmins")]
    public bool SendInvoicesToAdmins { get; set; } = true;

    // Payment
    [BsonElement("paymentMethods")]
    public List<object> PaymentMethods { get; set; } = [];

    // Balance
    [BsonElement("availableBalance")]
    public decimal AvailableBalance { get; set; }

    [BsonElement("uninvoicedAmount")]
    public decimal UninvoicedAmount { get; set; }

    [BsonElement("rechargeTo")]
    public decimal RechargeTo { get; set; }

    [BsonElement("rechargeThreshold")]
    public decimal RechargeThreshold { get; set; }

    [BsonElement("usageAlertsEnabled")]
    public bool UsageAlertsEnabled { get; set; }

    [BsonElement("uninvoicedLimit")]
    public decimal? UninvoicedLimit { get; set; }

    [BsonElement("balanceCurrency")]
    public string BalanceCurrency { get; set; } = "USD";

    [BsonElement("balanceUpdatedAt")]
    public DateTime? BalanceUpdatedAt { get; set; }

    [BsonElement("unbilledBreakdown")]
    public List<UnbilledBreakdownItem> UnbilledBreakdown { get; set; } = [];
}
