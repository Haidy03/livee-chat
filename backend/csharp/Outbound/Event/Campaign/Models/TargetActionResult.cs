namespace Outbound.Event.Campaign.Models;

public enum TargetActionStatus
{
    Success,          // delivered – move target to a terminal state
    RetryLater,       // transient failure – back off and retry
    PermanentFailure  // non-retryable – dead-letter and fail
}

/// <summary>
/// Outcome returned by an <c>ITargetAction</c>. The processor routes on <see cref="Status"/>;
/// <see cref="Disposition"/> further refines the terminal/retry decision (busy, no_answer, …).
/// </summary>
public sealed class TargetActionResult
{
    public TargetActionStatus Status { get; private init; }
    public string? Disposition { get; private init; }
    public string? Reason { get; private init; }
    public TimeSpan? RetryAfter { get; private init; }

    /// <summary>True for transient failures that should NOT count against the attempt cap
    /// (rate-limit / network blips — not the contact's fault).</summary>
    public bool IsInfrastructure { get; private init; }

    public static TargetActionResult Success(string? disposition = null) =>
        new() { Status = TargetActionStatus.Success, Disposition = disposition };

    public static TargetActionResult Retry(string? disposition = null, string? reason = null,
        TimeSpan? retryAfter = null, bool isInfrastructure = false) =>
        new()
        {
            Status = TargetActionStatus.RetryLater,
            Disposition = disposition,
            Reason = reason,
            RetryAfter = retryAfter,
            IsInfrastructure = isInfrastructure
        };

    public static TargetActionResult Permanent(string? disposition = null, string? reason = null) =>
        new() { Status = TargetActionStatus.PermanentFailure, Disposition = disposition, Reason = reason };
}
