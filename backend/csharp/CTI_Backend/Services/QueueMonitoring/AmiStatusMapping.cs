namespace CtiBackend.Services.QueueMonitoring;

public static class AmiStatusMapping
{
    /// <summary>Maps AMI device-status code to human label.</summary>
    public static string FromCode(int code) => code switch
    {
        0 => "Unknown",
        1 => "Available",
        2 => "InUse",
        3 => "Busy",
        4 => "Invalid",
        5 => "Unavailable",
        6 => "Ringing",
        7 => "RingInUse",
        8 => "OnHold",
        _ => "Unknown",
    };

    /// <summary>
    /// Computes the effective agent status. Pause and InCall override the raw code.
    /// </summary>
    public static string Compute(int code, bool paused, bool inCall, bool ringing)
    {
        if (paused && inCall) return "TalkingWhilePaused";
        if (paused) return "Paused";
        if (inCall) return "Talking";
        if (ringing) return "Ringing";
        return FromCode(code);
    }
}
