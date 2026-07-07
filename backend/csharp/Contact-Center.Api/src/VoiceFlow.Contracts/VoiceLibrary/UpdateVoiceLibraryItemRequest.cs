namespace VoiceFlow.Contracts.VoiceLibrary;

public sealed class UpdateVoiceLibraryItemRequest
{
    public string? Name { get; set; }
    public bool? Interruptible { get; set; }
}
