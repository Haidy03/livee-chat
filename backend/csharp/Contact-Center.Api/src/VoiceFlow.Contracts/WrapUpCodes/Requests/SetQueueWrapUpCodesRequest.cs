using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.WrapUpCodes.Requests;

public sealed class SetQueueWrapUpCodesRequest
{
    [Required]
    public List<string> CodeIds { get; set; } = new();
}
