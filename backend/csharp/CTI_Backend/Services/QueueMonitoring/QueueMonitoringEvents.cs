namespace CtiBackend.Services.QueueMonitoring;

public static class QueueMonitoringEvents
{
    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        // Snapshot
        "QueueParams","QueueMember","QueueEntry","QueueStatusComplete",
        "QueueSummary","QueueSummaryComplete",
        // Waiting callers
        "QueueCallerJoin","QueueCallerLeave","QueueCallerAbandon",
        "Join","Leave", // Older Asterisk aliases
        // Agent lifecycle
        "AgentCalled","AgentRingNoAnswer","AgentConnect","AgentComplete",
        // Membership
        "QueueMemberStatus","QueueMemberPause","QueueMemberAdded",
        "QueueMemberRemoved","QueueMemberPenalty","QueueMemberRinginuse",
    };

    public static readonly HashSet<string> Snapshot = new(StringComparer.OrdinalIgnoreCase)
    {
        "QueueParams","QueueMember","QueueEntry","QueueStatusComplete",
        "QueueSummary","QueueSummaryComplete",
    };

    public static bool IsRelevant(string? evt) => !string.IsNullOrEmpty(evt) && All.Contains(evt);
    public static bool IsSnapshot(string? evt) => !string.IsNullOrEmpty(evt) && Snapshot.Contains(evt);
}
