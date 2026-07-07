namespace Outbound.Event.Campaign.Processing;

/// <summary>How a disposition maps onto the target lifecycle once an action succeeds.</summary>
public enum TerminalKind { Successful, Callback, Failed }

/// <summary>
/// Maps call dispositions to terminal target states. <c>machine</c> and <c>abandoned</c> are
/// separated from generic callback so reporting can distinguish AMD hits from agent-side drops.
/// </summary>
public static class DispositionMapper
{
    public static TerminalKind ToTerminal(string? disposition) => (disposition ?? "").ToLowerInvariant() switch
    {
        "answered" or "delivered" or "sale" or "success" => TerminalKind.Successful,
        "callback"
            or "no_answer" or "busy"
            or "voicemail" or "machine" or "amd_hangup"
            or "abandoned" => TerminalKind.Callback,
        "invalid" or "opt_out" or "dnc" or "rejected" => TerminalKind.Failed,
        _ => TerminalKind.Successful
    };

    public static string ToStatus(TerminalKind kind) => kind switch
    {
        TerminalKind.Successful => "successful",
        TerminalKind.Callback => "callback",
        _ => "failed"
    };
}
