namespace VoiceFlow.Api.UserMaps.Requests;

public sealed class TransferRequest
{
    public TransferTarget Target { get; set; } = new();
}

public sealed class TransferTarget
{
    public string Type { get; set; } = string.Empty; // agent | queue | number
    public string? Id { get; set; }
    public string? Number { get; set; }
}
