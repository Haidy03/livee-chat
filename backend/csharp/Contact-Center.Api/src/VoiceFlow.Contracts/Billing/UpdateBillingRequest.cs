namespace VoiceFlow.Contracts.Billing;

public sealed class UpdateBillingRequest
{
    public string? InvoiceName { get; set; }
    public string? BillingEmails { get; set; }
    public string? BillingAddress { get; set; }
    public string? BillingCountry { get; set; }
    public string? VatNumber { get; set; }
    public string? RegistrationNumber { get; set; }
    public bool? SendInvoicesToAdmins { get; set; }
}
