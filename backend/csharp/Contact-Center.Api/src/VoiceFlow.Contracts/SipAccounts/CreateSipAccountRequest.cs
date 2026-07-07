using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.SipAccounts;

public sealed class CreateSipAccountRequest
{
    [Required]
    public string DisplayName { get; set; } = string.Empty;
    [Required]
    public string SipUri { get; set; } = string.Empty;
    [Required]
    public string AuthId { get; set; } = string.Empty;
    [Required]
    public string WsUrl { get; set; } = string.Empty;
    public List<string> StunUrls { get; set; } = ["stun:stun.l.google.com:19302"];
    public string TurnUrl { get; set; } = string.Empty;
    public string TurnUsername { get; set; } = string.Empty;
}
