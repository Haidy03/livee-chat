namespace VoiceFlow.FreeSwitchXmlCurl.Settings;

public sealed class FreeSwitchSettings
{
    public string DefaultInboundDomain { get; set; } = "pstn.example.com";
    public string VoicemailCollectionName { get; set; } = "voicemail_messages";
}
