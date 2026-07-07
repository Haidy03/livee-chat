namespace CtiBackend.Models.Cti;

public sealed class CallerInfoModel
{
    public string? PhoneNumber { get; set; }
    public string? CustomerId { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public bool IsVip { get; set; }
    public string? Segment { get; set; }
    public string? NationalId { get; set; }
    public string? AccountNumber { get; set; }
    public Dictionary<string, string> Extra { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
