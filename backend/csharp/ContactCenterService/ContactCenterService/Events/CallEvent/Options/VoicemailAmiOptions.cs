namespace Contact_Center.Worker.Events.CallEvent.Options;

/// <summary>
/// Connection settings for the AMI manager account the worker uses to receive
/// UserEvent(VoicemailRecorded) events emitted by the Asterisk dialplan.
/// </summary>
public sealed class VoicemailAmiOptions
{
    public const string SectionName = "VoicemailAmi";

    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 5038;
    public string Username { get; init; } = "voicemail-worker";
    public string Password { get; init; } = "";
    public int ReconnectDelaySeconds { get; init; } = 5;
}
