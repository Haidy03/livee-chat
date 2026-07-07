namespace Contact_Center.Worker.Events.Reports.Services.Execution;

/// <summary>
/// Turns a relative range token (e.g. <c>last_7d</c>) from the report filters into a
/// concrete UTC <c>[from, to)</c> window. Unknown tokens fall back to the last 30 days.
/// </summary>
internal static class DateRangeResolver
{
    public static (DateTimeOffset from, DateTimeOffset to) Resolve(string? range)
    {
        var now = DateTimeOffset.UtcNow;
        var to = new DateTimeOffset(now.Date.AddDays(1), TimeSpan.Zero);
        var days = range switch
        {
            "today" => 1,
            "last_7d" or "last_7_days" => 7,
            "last_14d" or "last_14_days" => 14,
            "last_30d" or "last_30_days" => 30,
            "last_90d" or "last_90_days" => 90,
            _ => 30,
        };
        return (to.AddDays(-days), to);
    }
}
