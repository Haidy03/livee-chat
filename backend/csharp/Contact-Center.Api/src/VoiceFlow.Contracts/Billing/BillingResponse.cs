namespace VoiceFlow.Contracts.Billing;

public sealed class BillingResponse
{
    public string Id { get; init; } = string.Empty;
    public string InvoiceName { get; init; } = string.Empty;
    public string BillingEmails { get; init; } = string.Empty;
    public string BillingAddress { get; init; } = string.Empty;
    public string BillingCountry { get; init; } = string.Empty;
    public string VatNumber { get; init; } = string.Empty;
    public string RegistrationNumber { get; init; } = string.Empty;
    public bool SendInvoicesToAdmins { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
