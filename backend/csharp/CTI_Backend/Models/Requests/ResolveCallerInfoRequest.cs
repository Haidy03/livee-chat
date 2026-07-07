namespace CtiBackend.Models.Requests;

public sealed class ResolveCallerInfoRequest
{
    public string? TenantId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? LinkedId { get; set; }
    public string? UniqueId { get; set; }
}
