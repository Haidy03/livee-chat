using System.Collections.Generic;
using System.Linq;

namespace VoiceFlow.Core.Reports.Catalog;

/// <summary>Metric aggregation kinds owned by the backend. The frontend selects a key; the backend builds the Mongo expression.</summary>
public enum ReportMetricKind
{
    Count,
    SumDuration,
    AvgDuration,
    MinDuration,
    MaxDuration,
    AnsweredCount,
    UnansweredCount,
    AbandonedCount,
    AnswerRate,
    InboundCount,
    OutboundCount,
    InternalCount,
    HoldTime,

    // Extended kinds (surfaced in metadata; executor falls back to Count for unknown accumulators).
    ActiveCount,
    AvgHandleTime,
    TotalTalkTime,
    TotalWaiting,
    AvgWaitTime,
    ServiceLevel,
    TargetsCount,
    ContactedCount,
    SuccessCount,
    SuccessRate,
    ContactRate,
    OpenCount,
    ClosedCount,
    ResolvedCount,
    AvgResolutionTime,
    AvgResponseTime,
    TotalMessages,
    AvgScore,
    CsatPercentage,
    Nps,

    // Calls (wrap-up / voicemail).
    VoicemailCount,
    SumAcw,
    AvgSpeedOfAnswer,       // avg ringSeconds over answered calls (ASA)
    AnsweredWithinSlaCount, // answered within the SLA threshold (service-level numerator)
    LongestWait,            // max ringSeconds
    RecordedCount,          // hasRecording == true
    CallbackCount,          // wrapUp.callbackScheduled == true
    HeldCount,              // holdSeconds > 0
    NegativeSentimentCount, // sentiment == Negative

    // Generic post-group ratio marker: no accumulator, computed as numerator/denominator
    // in MetricReportBuilder.RatioMetrics. Use for any new percentage metric.
    PostGroupRatio,

    // Outbound attempts (call_attempts ledger).
    ConnectedCount,      // dialStatus == ANSWER
    RightPartyCount,     // amdStatus == HUMAN
    AnsweringMachineCount,     // amdStatus == MACHINE (AMD breakdown)
    AbandonedDispositionCount, // disposition == "abandoned" (predictive over-dial drop)
    AvgAttemptsToContact,      // avg attemptNumber of the connecting attempt

    // Post-group ratio metrics (numerator/denominator handled by the executor).
    AbandonmentRate,
    ConnectionRate,
    RightPartyRate,
    ConversionRate,

    // Customers (contacts) / campaign targets.
    RepeatContactCount,  // totalCalls > 1
}

public sealed record ReportFieldDefinition(
    string Key,
    string LabelEn,
    string LabelAr,
    string MongoField,
    string DataType,
    bool CanUseInDetail = true,
    bool CanUseAsDimension = false,
    bool CanFilter = true,
    bool CanSort = true,
    string? CanonicalKey = null,
    // Optional display lookup for a dimension: after grouping by MongoField (an id),
    // the executor swaps the id for LookupDisplayField from LookupCollection, matched
    // on LookupForeignField. Lets a report group by a stable id but show a name.
    string? LookupCollection = null,
    string? LookupForeignField = null,
    string? LookupDisplayField = null)
{
    public string ResolvedKey => string.IsNullOrWhiteSpace(CanonicalKey) ? Key : CanonicalKey;

    public bool HasDisplayLookup =>
        !string.IsNullOrWhiteSpace(LookupCollection)
        && !string.IsNullOrWhiteSpace(LookupForeignField)
        && !string.IsNullOrWhiteSpace(LookupDisplayField);
}

public sealed record ReportMetricDefinition(
    string Key,
    string LabelEn,
    string LabelAr,
    string DataType,
    ReportMetricKind Kind,
    string? CanonicalKey = null)
{
    public string ResolvedKey => string.IsNullOrWhiteSpace(CanonicalKey) ? Key : CanonicalKey;
}

public sealed class ReportDataSourceDefinition
{
    public string Key { get; init; } = null!;
    public string CollectionName { get; init; } = null!;
    public string DateField { get; init; } = "createdAt";

    // Display metadata surfaced by the /reports/data-sources API so the frontend picker is
    // fully catalog-driven (no hardcoded source list).
    public string LabelEn { get; init; } = "";
    public string LabelAr { get; init; } = "";
    public string Icon { get; init; } = "Database";
    public string DescriptionEn { get; init; } = "";
    public string DescriptionAr { get; init; } = "";

    /// <summary>False for planned sources with no data yet — returned by the API, shown disabled.</summary>
    public bool Ready { get; init; } = true;

    /// <summary>
    /// When set, metric-mode reports on this source aggregate the named source's collection
    /// instead (e.g. agents → calls: the roster lives in profiles, but per-agent performance
    /// metrics live in calls grouped by agent). Detail mode still uses this source's own fields.
    /// </summary>
    public string? MetricDelegateKey { get; init; }

    /// <summary>
    /// When set, metric-mode reports run a bespoke cross-collection rollup instead of the generic
    /// group aggregation (e.g. campaigns → "campaign": one row per campaign joining
    /// campaign_targets + call_attempts). Detail mode still uses this source's own collection.
    /// </summary>
    public string? MetricSummaryBuilder { get; init; }

    /// <summary>
    /// Event sources (calls, attempts) are naturally bounded by the report's date range.
    /// Entity sources (agents, queues, campaigns, contacts) are a current-state list — filtering
    /// them by <see cref="DateField"/> would wrongly drop rows created outside the window, so the
    /// executor omits the date clause when this is false.
    /// </summary>
    public bool DateFiltered { get; init; } = true;
    public IReadOnlyList<ReportFieldDefinition> Fields { get; init; } = System.Array.Empty<ReportFieldDefinition>();
    public IReadOnlyList<ReportMetricDefinition> Metrics { get; init; } = System.Array.Empty<ReportMetricDefinition>();

    public ReportFieldDefinition? FindField(string key)
        => Fields.FirstOrDefault(f => string.Equals(f.Key, key, System.StringComparison.OrdinalIgnoreCase));

    public ReportMetricDefinition? FindMetric(string key)
        => Metrics.FirstOrDefault(m => string.Equals(m.Key, key, System.StringComparison.OrdinalIgnoreCase));
}

public static class ReportDataSourceCatalog
{
    // Small builders to keep the dictionary readable.
    private static ReportFieldDefinition F(string key, string en, string ar, string mongo, string type,
        bool detail = true, bool dim = false, string? canonical = null)
        => new(key, en, ar, mongo, type, CanUseInDetail: detail, CanUseAsDimension: dim, CanonicalKey: canonical);

    // Reference dimension: groups by an id field but displays a looked-up name.
    private static ReportFieldDefinition Ref(string key, string en, string ar, string groupField,
        string collection, string foreignField, string display, string? canonical = null)
        => new(key, en, ar, groupField, "string", CanUseInDetail: false, CanUseAsDimension: true,
            CanonicalKey: canonical,
            LookupCollection: collection, LookupForeignField: foreignField, LookupDisplayField: display);

    private static ReportMetricDefinition M(string key, string en, string ar, ReportMetricKind kind, string type = "number", string? canonical = null)
        => new(key, en, ar, type, kind, canonical);

    // Planned source with no data yet: surfaced in the API (Ready=false) so the UI can list it disabled.
    private static ReportDataSourceDefinition Stub(string key, string en, string ar, string icon, string descEn, string descAr)
        => new() { Key = key, CollectionName = key, Ready = false, Icon = icon,
            LabelEn = en, LabelAr = ar, DescriptionEn = descEn, DescriptionAr = descAr };

    private static readonly Dictionary<string, ReportDataSourceDefinition> _sources =
        new(System.StringComparer.OrdinalIgnoreCase)
        {
            ["calls"] = new()
            {
                Key = "calls",
                LabelEn = "Calls / Voice", LabelAr = "المكالمات / الصوت", Icon = "Phone",
                DescriptionEn = "Inbound and outbound voice interactions", DescriptionAr = "تفاعلات صوتية واردة وصادرة",
                CollectionName = "calls",
                DateField = "startedAt",
                Fields = new[]
                {
                    // Detail-mode fields — every entry maps to a real Call BSON element.
                    F("callId",           "Call ID",          "معرف المكالمة",   "callId",           "string"),
                    F("callerNumber",     "Caller Number",    "رقم المتصل",      "callerId",         "string"),
                    F("callerName",       "Caller Name",      "اسم المتصل",      "callerName",       "string"),
                    F("callerExtension",  "Caller Extension", "امتداد المتصل",   "callerExtension",  "string"),
                    F("calleeNumber",     "Callee Number",    "رقم المستقبل",    "calledId",         "string"),
                    F("calleeName",       "Callee Name",      "اسم المستقبل",    "calledName",       "string"),
                    F("calleeExtension",  "Callee Extension", "امتداد المستقبل", "calledExtension",  "string"),
                    F("direction",        "Direction",        "الاتجاه",         "direction",        "string", dim: true),
                    F("status",           "Status",           "الحالة",          "status",           "string", dim: true),
                    F("startedAt",        "Started At",       "وقت البدء",       "startedAt",        "date"),
                    F("answeredAt",       "Answered At",      "وقت الرد",        "answeredAt",       "date"),
                    F("endedAt",          "Ended At",         "وقت الانتهاء",    "endedAt",          "date"),
                    F("durationSec",      "Duration (s)",     "المدة (ث)",       "totalSeconds",     "number"),
                    F("ringSeconds",      "Ring (s)",         "زمن الرنين",      "ringSeconds",      "number"),
                    F("holdSeconds",      "Hold (s)",         "زمن الانتظار",    "totalHoldSeconds", "number"),
                    F("activeSeconds",    "Talk (s)",         "زمن المحادثة",    "activeSeconds",    "number"),
                    F("hangupCause",      "Hangup Cause",     "سبب الإنهاء",     "hangupCause",      "string"),
                    F("hasRecording",     "Has Recording",    "به تسجيل",        "hasRecording",     "boolean"),
                    F("notes",            "Notes",            "ملاحظات",         "notes",            "string"),
                    F("summary",          "Summary",          "الملخص",          "summary",          "string"),
                    F("sentiment",        "Sentiment",        "المشاعر",         "sentiment",        "string", dim: true),

                    // Dimension-only aliases (hidden from Detail-mode picker; used for grouping in Metric mode).
                    // Agent: canonical key is "agent" (nice column). Groups by the always-present userId,
                    // then swaps it for the profile's displayName via a post-group $lookup. (Top-level
                    // agentId is only set at wrap-up, so userId is the reliable key.)
                    Ref("agent",     "Agent",              "الموظف",          "userId", "profiles", "userId", "displayName"),
                    Ref("agentName", "Agent",              "الموظف",          "userId", "profiles", "userId", "displayName", canonical: "agent"),
                    Ref("agentId",   "Agent",              "الموظف",          "userId", "profiles", "userId", "displayName", canonical: "agent"),
                    // queue*/campaign* map to groupId, which is NOT populated on the call write path yet —
                    // these group under null until queue/campaign linkage is written. Kept for forward compat.
                    F("queueId",     "Queue ID",           "معرف قائمة الانتظار", "groupId", "string", detail: false, dim: true, canonical: "groupId"),
                    F("queueName",   "Queue",              "قائمة الانتظار",  "groupId",   "string", detail: false, dim: true, canonical: "groupId"),
                    F("campaignId",  "Campaign",           "الحملة",          "groupId",   "string", detail: false, dim: true, canonical: "groupId"),
                    F("queue",       "Queue",              "قائمة الانتظار",  "groupId",   "string", detail: false, dim: true, canonical: "groupId"),
                    F("channel",     "Channel",            "القناة",          "direction", "string", detail: false, dim: true, canonical: "direction"),
                    // Real disposition lives on the nested wrap-up; only set for wrapped-up calls.
                    F("disposition", "Disposition",        "التصنيف",         "wrapUp.disposition", "string", detail: false, dim: true),
                    F("campaign",    "Campaign",           "الحملة",          "groupId",   "string", detail: false, dim: true, canonical: "groupId"),
                },

                Metrics = new[]
                {
                    M("count", "Call Count", "عدد المكالمات", ReportMetricKind.Count),
                    M("total_duration", "Total Duration", "إجمالي المدة", ReportMetricKind.SumDuration),
                    M("avg_duration", "Average Duration", "متوسط المدة", ReportMetricKind.AvgDuration),
                    M("min_duration", "Min Duration", "أقل مدة", ReportMetricKind.MinDuration),
                    M("max_duration", "Max Duration", "أطول مدة", ReportMetricKind.MaxDuration),
                    M("answered_count", "Answered", "المُجابة", ReportMetricKind.AnsweredCount),
                    M("unanswered_count", "Unanswered", "غير المُجابة", ReportMetricKind.UnansweredCount),
                    M("abandoned_count", "Abandoned", "المهجورة", ReportMetricKind.AbandonedCount),
                    M("answer_rate", "Answer Rate %", "نسبة الرد %", ReportMetricKind.AnswerRate),

                    // UI-facing metric aliases.
                    M("total_calls",     "Total Calls",      "إجمالي المكالمات",   ReportMetricKind.Count, canonical: "count"),
                    M("inbound_calls",   "Inbound Calls",    "المكالمات الواردة",  ReportMetricKind.InboundCount),
                    M("outbound_calls",  "Outbound Calls",   "المكالمات الصادرة",  ReportMetricKind.OutboundCount),
                    M("internal_calls",  "Internal Calls",   "المكالمات الداخلية", ReportMetricKind.InternalCount),
                    M("calls_offered",   "Calls Offered",    "المكالمات المعروضة", ReportMetricKind.Count, canonical: "count"),
                    M("calls_answered",  "Calls Answered",   "المكالمات المُجابة", ReportMetricKind.AnsweredCount, canonical: "answered_count"),
                    M("answered_calls",  "Answered Calls",   "المكالمات المُجابة", ReportMetricKind.AnsweredCount, canonical: "answered_count"),
                    M("calls_abandoned", "Calls Abandoned",  "المكالمات المهجورة", ReportMetricKind.AbandonedCount, canonical: "abandoned_count"),
                    M("abandoned_calls", "Abandoned Calls",  "المكالمات المهجورة", ReportMetricKind.AbandonedCount, canonical: "abandoned_count"),
                    M("talk_time",       "Talk Time",        "زمن المحادثة",       ReportMetricKind.SumDuration, canonical: "total_duration"),
                    M("hold_time",       "Hold Time",        "زمن الانتظار",       ReportMetricKind.HoldTime),
                    M("avg_talk_time",   "Avg Talk Time",    "متوسط المحادثة",     ReportMetricKind.AvgDuration, canonical: "avg_duration"),

                    // Wrap-up derived (keys match the frontend Calls metric ids).
                    M("average_handle_time_aht", "Avg Handle Time (AHT)", "متوسط زمن المعالجة", ReportMetricKind.AvgHandleTime),
                    M("after_call_work_acw",     "After-Call Work (ACW)", "العمل بعد المكالمة", ReportMetricKind.SumAcw),
                    M("voicemails_left",         "Voicemails Left",       "البريد الصوتي المتروك", ReportMetricKind.VoicemailCount),
                    M("abandonment_rate",        "Abandonment Rate %",    "معدل الهجر %",       ReportMetricKind.AbandonmentRate, type: "percent"),

                    // Speed-of-answer / service level (from ringSeconds + answeredAt).
                    M("asa",                     "Avg Speed of Answer",   "متوسط سرعة الرد",    ReportMetricKind.AvgSpeedOfAnswer),
                    M("answered_within_sla_count","Answered in SLA",      "المُجابة ضمن الهدف",  ReportMetricKind.AnsweredWithinSlaCount),
                    M("service_level",           "Service Level %",       "مستوى الخدمة %",     ReportMetricKind.ServiceLevel, type: "percent"),
                    M("longest_wait",            "Longest Wait",          "أطول انتظار",        ReportMetricKind.LongestWait),

                    // Recording coverage, callback rate, hold frequency, negative sentiment —
                    // each a count numerator plus a post-group percentage over total calls.
                    M("recorded_count",          "Calls Recorded",        "المكالمات المسجّلة", ReportMetricKind.RecordedCount),
                    M("recording_coverage",      "Recording Coverage %",  "تغطية التسجيل %",    ReportMetricKind.PostGroupRatio, type: "percent"),
                    M("callback_count",          "Callbacks Scheduled",   "المعاودات المجدولة", ReportMetricKind.CallbackCount),
                    M("callback_rate",           "Callback Rate %",       "معدل المعاودة %",    ReportMetricKind.PostGroupRatio, type: "percent"),
                    M("held_count",              "Calls Held",            "المكالمات المعلّقة", ReportMetricKind.HeldCount),
                    M("hold_rate",               "Hold Rate %",           "معدل التعليق %",     ReportMetricKind.PostGroupRatio, type: "percent"),
                    M("negative_sentiment_count","Negative Sentiment",    "المشاعر السلبية",    ReportMetricKind.NegativeSentimentCount),
                    M("negative_sentiment_rate", "Negative Sentiment %",  "نسبة المشاعر السلبية %", ReportMetricKind.PostGroupRatio, type: "percent"),
                },

            },

            // Agents are the platform users in the `profiles` collection (current-state list),
            // so this is not date-filtered. Per-agent CALL metrics live on the `calls` source
            // grouped by agent — profiles only backs the agent roster itself.
            ["agents"] = new()
            {
                Key = "agents",
                LabelEn = "Agents", LabelAr = "الموظفون", Icon = "Headphones",
                DescriptionEn = "Roster and per-agent activity", DescriptionAr = "قائمة الموظفين والنشاط لكل موظف",
                CollectionName = "profiles",
                DateField = "createdAt",
                DateFiltered = false,
                // Detail = the profiles roster; metric mode aggregates calls grouped by agent.
                MetricDelegateKey = "calls",
                Fields = new[]
                {
                    F("agentId",   "Agent ID",   "معرف الموظف",       "userId",          "string"),
                    F("agentName", "Agent Name", "اسم الموظف",        "displayName",     "string", dim: true),
                    F("firstName", "First Name", "الاسم الأول",       "firstName",       "string"),
                    F("lastName",  "Last Name",  "اسم العائلة",       "lastName",        "string"),
                    F("email",     "Email",      "البريد الإلكتروني", "email",           "string"),
                    F("extension", "Extension",  "الرقم الفرعي",      "extensionNumber", "number"),
                    F("role",      "Role",       "الدور",             "role",            "string", dim: true),
                    F("status",    "Status",     "الحالة",            "status",          "string", dim: true),
                    // Array of skill objects; the executor collapses it to a name list ("English, Arabic").
                    F("skills",    "Skills",     "المهارات",          "skills",          "string"),
                    F("createdAt", "Created At", "تاريخ الإنشاء",     "createdAt",       "date"),
                },
                Metrics = new[]
                {
                    // Only the roster count is backed by `profiles`; per-agent call metrics
                    // (AHT, answered, …) come from the `calls` source grouped by agent.
                    M("count", "Agents Count", "عدد الموظفين", ReportMetricKind.Count),
                },
            },

            ["queues"] = new()
            {
                Key = "queues",
                LabelEn = "Queues", LabelAr = "قوائم الانتظار", Icon = "Inbox",
                DescriptionEn = "Queue configuration and roster", DescriptionAr = "إعدادات قوائم الانتظار",
                CollectionName = "queues",
                DateField = "createdAt",
                DateFiltered = false,
                Fields = new[]
                {
                    F("queueId", "Queue ID", "معرف قائمة الانتظار", "_id", "string"),
                    F("queueName", "Queue Name", "اسم قائمة الانتظار", "name", "string", dim: true),
                    F("code", "Code", "الرمز", "code", "string"),
                    F("strategy", "Strategy", "الاستراتيجية", "strategy", "string", dim: true),
                    F("status", "Status", "الحالة", "status", "string", dim: true),
                    F("agentsCount", "Agents Count", "عدد الموظفين", "agentsCount", "number"),
                    F("createdAt", "Created At", "تاريخ الإنشاء", "createdAt", "date"),
                },
                Metrics = new[]
                {
                    M("count", "Queues Count", "عدد القوائم", ReportMetricKind.Count),
                    M("total_waiting", "Total Waiting", "إجمالي الانتظار", ReportMetricKind.TotalWaiting),
                    M("avg_wait_time", "Avg Wait Time", "متوسط زمن الانتظار", ReportMetricKind.AvgWaitTime),
                    M("answered_count", "Answered", "المُجابة", ReportMetricKind.AnsweredCount),
                    M("abandoned_count", "Abandoned", "المهجورة", ReportMetricKind.AbandonedCount),
                    M("service_level", "Service Level %", "مستوى الخدمة %", ReportMetricKind.ServiceLevel),
                },
            },

            ["campaigns"] = new()
            {
                Key = "campaigns",
                LabelEn = "Campaigns", LabelAr = "الحملات", Icon = "Megaphone",
                DescriptionEn = "Per-campaign rollup: targets, attempts, agents, outcomes", DescriptionAr = "ملخّص لكل حملة: الأهداف والمحاولات والموظفون والنتائج",
                CollectionName = "campaigns",
                DateField = "createdAt",
                DateFiltered = false,
                // Detail = the campaign list; metric mode runs the cross-collection rollup
                // (campaigns + campaign_targets + call_attempts) — one row per campaign.
                MetricSummaryBuilder = "campaign",
                Fields = new[]
                {
                    F("campaignId", "Campaign ID", "معرف الحملة", "_id", "string"),
                    F("name", "Name", "الاسم", "name", "string"),
                    F("dialingMode", "Dialing Mode", "وضع الاتصال", "dialingMode", "string"),
                    F("status", "Status", "الحالة", "status", "string"),
                    F("assignedMode", "Assigned Mode", "وضع التعيين", "assignedMode", "string"),
                    F("startDate", "Start Date", "تاريخ البداية", "startDate", "date"),
                    F("endDate", "End Date", "تاريخ النهاية", "endDate", "date"),
                    F("createdAt", "Created At", "تاريخ الإنشاء", "createdAt", "date"),
                },
                // Metric keys map to fields the CampaignSummaryBuilder projects (one row per campaign).
                Metrics = new[]
                {
                    M("targets",          "Targets",          "الأهداف",          ReportMetricKind.TargetsCount),
                    M("contacted",        "Contacted",        "تم الاتصال",       ReportMetricKind.ContactedCount),
                    M("succeeded",        "Succeeded",        "الناجحة",          ReportMetricKind.SuccessCount),
                    M("failed",           "Failed",           "الفاشلة",          ReportMetricKind.Count),
                    M("list_penetration", "List Penetration %","اختراق القائمة %", ReportMetricKind.ContactRate, type: "percent"),
                    M("success_rate",     "Success Rate %",   "نسبة النجاح %",    ReportMetricKind.SuccessRate, type: "percent"),
                    M("attempts",         "Attempts",         "المحاولات",        ReportMetricKind.Count),
                    M("connected",        "Connected",        "المتصلة",          ReportMetricKind.ConnectedCount),
                    M("connection_rate",  "Connection Rate %","معدل الاتصال %",   ReportMetricKind.ConnectionRate, type: "percent"),
                    M("rpc",              "Right-Party",      "الطرف الصحيح",     ReportMetricKind.RightPartyCount),
                    M("rpc_rate",         "Right-Party %",    "الطرف الصحيح %",   ReportMetricKind.RightPartyRate, type: "percent"),
                    M("machine",          "Answering Machine","المجيب الآلي",     ReportMetricKind.AnsweringMachineCount),
                    M("abandoned",        "Abandoned",        "المهجورة",         ReportMetricKind.AbandonedDispositionCount),
                    M("abandonment_rate", "Abandonment Rate %","معدل الهجر %",    ReportMetricKind.AbandonmentRate, type: "percent"),
                    M("agents",           "Agents",           "الموظفون",         ReportMetricKind.Count),
                    M("avg_duration",     "Avg Duration",     "متوسط المدة",      ReportMetricKind.AvgDuration),
                    M("avg_queue_wait",   "Avg Queue Wait",   "متوسط الانتظار",   ReportMetricKind.AvgWaitTime),
                },
            },

            ["tickets"] = new()
            {
                Key = "tickets",
                LabelEn = "Tickets / Cases", LabelAr = "التذاكر / الحالات", Icon = "ClipboardList",
                DescriptionEn = "Cases, dispositions, and resolution", DescriptionAr = "الحالات والمآلات والحل",
                Ready = false,
                CollectionName = "tickets",
                DateField = "createdAt",
                Fields = new[]
                {
                    F("ticketId", "Ticket ID", "معرف التذكرة", "_id", "string"),
                    F("subject", "Subject", "الموضوع", "subject", "string"),
                    F("status", "Status", "الحالة", "status", "string", dim: true),
                    F("priority", "Priority", "الأولوية", "priority", "string", dim: true),
                    F("channel", "Channel", "القناة", "channel", "string", dim: true),
                    F("assignedAgent", "Assigned Agent", "الموظف المعين", "assignedAgent", "string", dim: true),
                    F("createdAt", "Created At", "تاريخ الإنشاء", "createdAt", "date"),
                    F("closedAt", "Closed At", "تاريخ الإغلاق", "closedAt", "date"),
                },
                Metrics = new[]
                {
                    M("count", "Tickets Count", "عدد التذاكر", ReportMetricKind.Count),
                    M("open_count", "Open", "المفتوحة", ReportMetricKind.OpenCount),
                    M("closed_count", "Closed", "المغلقة", ReportMetricKind.ClosedCount),
                    M("resolved_count", "Resolved", "المحلولة", ReportMetricKind.ResolvedCount),
                    M("avg_resolution_time", "Avg Resolution Time", "متوسط زمن الحل", ReportMetricKind.AvgResolutionTime),
                    M("avg_response_time", "Avg Response Time", "متوسط زمن الاستجابة", ReportMetricKind.AvgResponseTime),
                },
            },

            ["chat"] = new()
            {
                Key = "chat",
                LabelEn = "Chat / Messaging", LabelAr = "الدردشة / المراسلة", Icon = "MessageSquare",
                DescriptionEn = "Live chat and async messaging", DescriptionAr = "الدردشة الحية والمراسلة",
                Ready = false,
                CollectionName = "chatSessions",
                DateField = "startedAt",
                Fields = new[]
                {
                    F("sessionId", "Session ID", "معرف الجلسة", "_id", "string"),
                    F("agentName", "Agent", "الموظف", "agentName", "string", dim: true),
                    F("channel", "Channel", "القناة", "channel", "string", dim: true),
                    F("status", "Status", "الحالة", "status", "string", dim: true),
                    F("sentiment", "Sentiment", "المشاعر", "sentiment", "string", dim: true),
                    F("startedAt", "Started At", "وقت البدء", "startedAt", "date"),
                    F("endedAt", "Ended At", "وقت الانتهاء", "endedAt", "date"),
                    F("durationSec", "Duration (s)", "المدة (ث)", "durationSec", "number"),
                    F("messageCount", "Messages", "الرسائل", "messageCount", "number"),
                },
                Metrics = new[]
                {
                    M("count", "Sessions Count", "عدد الجلسات", ReportMetricKind.Count),
                    M("avg_duration", "Avg Duration", "متوسط المدة", ReportMetricKind.AvgDuration),
                    M("total_messages", "Total Messages", "إجمالي الرسائل", ReportMetricKind.TotalMessages),
                    M("answered_count", "Answered", "المُجابة", ReportMetricKind.AnsweredCount),
                    M("abandoned_count", "Abandoned", "المهجورة", ReportMetricKind.AbandonedCount),
                    M("avg_response_time", "Avg Response Time", "متوسط زمن الاستجابة", ReportMetricKind.AvgResponseTime),
                },
            },

            ["email"] = new()
            {
                Key = "email",
                LabelEn = "Email", LabelAr = "البريد الإلكتروني", Icon = "Mail",
                DescriptionEn = "Asynchronous email interactions", DescriptionAr = "تفاعلات بريدية غير متزامنة",
                Ready = false,
                CollectionName = "emailThreads",
                DateField = "receivedAt",
                Fields = new[]
                {
                    F("threadId", "Thread ID", "معرف المحادثة", "_id", "string"),
                    F("subject", "Subject", "الموضوع", "subject", "string"),
                    F("fromAddress", "From", "من", "fromAddress", "string"),
                    F("assignedAgent", "Assigned Agent", "الموظف المعين", "assignedAgent", "string", dim: true),
                    F("status", "Status", "الحالة", "status", "string", dim: true),
                    F("priority", "Priority", "الأولوية", "priority", "string", dim: true),
                    F("receivedAt", "Received At", "تاريخ الاستلام", "receivedAt", "date"),
                    F("resolvedAt", "Resolved At", "تاريخ الحل", "resolvedAt", "date"),
                },
                Metrics = new[]
                {
                    M("count", "Emails Count", "عدد الرسائل", ReportMetricKind.Count),
                    M("resolved_count", "Resolved", "المحلولة", ReportMetricKind.ResolvedCount),
                    M("open_count", "Open", "المفتوحة", ReportMetricKind.OpenCount),
                    M("avg_response_time", "Avg Response Time", "متوسط زمن الاستجابة", ReportMetricKind.AvgResponseTime),
                    M("avg_resolution_time", "Avg Resolution Time", "متوسط زمن الحل", ReportMetricKind.AvgResolutionTime),
                },
            },

            ["surveys"] = new()
            {
                Key = "surveys",
                LabelEn = "Surveys — CSAT / NPS / CES", LabelAr = "الاستبيانات", Icon = "Star",
                DescriptionEn = "Post-contact feedback", DescriptionAr = "ملاحظات بعد التواصل",
                Ready = false,
                CollectionName = "surveyResponses",
                DateField = "submittedAt",
                Fields = new[]
                {
                    F("responseId", "Response ID", "معرف الرد", "_id", "string"),
                    F("surveyId", "Survey ID", "معرف الاستبيان", "surveyId", "string"),
                    F("surveyName", "Survey", "الاستبيان", "surveyName", "string", dim: true),
                    F("agentName", "Agent", "الموظف", "agentName", "string", dim: true),
                    F("channel", "Channel", "القناة", "channel", "string", dim: true),
                    F("score", "Score", "الدرجة", "score", "number"),
                    F("submittedAt", "Submitted At", "وقت الإرسال", "submittedAt", "date"),
                },
                Metrics = new[]
                {
                    M("count", "Responses Count", "عدد الردود", ReportMetricKind.Count),
                    M("avg_score", "Avg Score", "متوسط الدرجة", ReportMetricKind.AvgScore),
                    M("csat_percentage", "CSAT %", "رضا العملاء %", ReportMetricKind.CsatPercentage),
                    M("nps", "NPS", "صافي المروجين", ReportMetricKind.Nps),
                },
            },

            // Outbound campaigns report over the per-dial ledger written by the Outbound engine
            // (call_attempts). This is the source of truth for attempts/outcomes — the old
            // "outboundAttempts" collection never existed.
            ["outbound"] = new()
            {
                Key = "outbound",
                LabelEn = "Outbound Campaigns", LabelAr = "الحملات الصادرة", Icon = "PhoneOutgoing",
                DescriptionEn = "Dialer attempts and outcomes", DescriptionAr = "محاولات الاتصال والنتائج",
                CollectionName = "call_attempts",
                DateField = "startedAt",
                Fields = new[]
                {
                    // Detail-mode fields — every entry is a real CallAttempt element.
                    F("attemptId",   "Attempt ID",   "معرف المحاولة",  "_id",         "string"),
                    F("targetId",    "Target ID",    "معرف الهدف",     "targetId",    "string"),
                    F("attemptNumber","Attempt #",   "رقم المحاولة",   "attemptNumber","number"),
                    F("startedAt",   "Started At",   "وقت البدء",      "startedAt",   "date"),
                    F("answeredAt",  "Answered At",  "وقت الرد",       "answeredAt",  "date"),
                    F("endedAt",     "Ended At",     "وقت الانتهاء",   "endedAt",     "date"),
                    F("durationSec", "Duration (s)", "المدة (ث)",      "durationSec", "number"),
                    F("queueWaitSec","Queue Wait (s)","انتظار (ث)",    "queueWaitSec","number"),
                    F("dialStatus",  "Dial Status",  "حالة الاتصال",   "dialStatus",  "string", dim: true),
                    F("amdStatus",   "AMD Status",   "كشف المجيب",     "amdStatus",   "string", dim: true),
                    F("hangupCause", "Hangup Cause", "سبب الإنهاء",    "hangupCause", "string"),

                    // Dimensions (keys match the frontend dimension ids).
                    Ref("campaign", "Campaign", "الحملة", "campaignId", "campaigns", "_id", "name"),
                    Ref("agent",    "Agent",    "الموظف", "agentId",    "profiles",  "userId", "displayName"),
                    F("disposition","Disposition","المآل", "disposition", "string", detail: false, dim: true),
                    F("status",     "Dial Status","حالة الاتصال", "dialStatus", "string", detail: false, dim: true, canonical: "dialStatus"),
                    F("campaignId", "Campaign ID","معرف الحملة", "campaignId", "string", detail: false, dim: true, canonical: "campaign"),
                },
                Metrics = new[]
                {
                    // Base counts (also usable as ratio numerators).
                    M("count",           "Attempts",       "المحاولات",        ReportMetricKind.Count),
                    M("connected_count", "Connected",      "المتصلة",          ReportMetricKind.ConnectedCount),
                    M("rpc_count",       "Right-Party",    "الطرف الصحيح",     ReportMetricKind.RightPartyCount),
                    M("machine_count",   "Answering Machine","المجيب الآلي",    ReportMetricKind.AnsweringMachineCount),
                    M("abandoned_count", "Abandoned",      "المهجورة",         ReportMetricKind.AbandonedDispositionCount),
                    M("success_count",   "Successful",     "الناجحة",          ReportMetricKind.SuccessCount),
                    M("total_duration",  "Total Duration", "إجمالي المدة",     ReportMetricKind.SumDuration),
                    M("avg_duration",    "Avg Duration",   "متوسط المدة",      ReportMetricKind.AvgDuration),
                    M("avg_queue_wait",  "Avg Queue Wait", "متوسط الانتظار",   ReportMetricKind.AvgWaitTime),
                    M("avg_attempts_to_contact", "Avg Attempts to Contact", "متوسط المحاولات للاتصال", ReportMetricKind.AvgAttemptsToContact),

                    // Frontend-facing metric ids.
                    M("calls_attempted",         "Calls Attempted",         "محاولات الاتصال",      ReportMetricKind.Count, canonical: "count"),
                    M("time_to_disposition",     "Time to Disposition",     "زمن المآل",            ReportMetricKind.AvgDuration, canonical: "avg_duration"),
                    M("connection_rate",         "Connection Rate %",       "معدل الاتصال %",       ReportMetricKind.ConnectionRate, type: "percent"),
                    M("right_party_contact_rate","Right-Party Contact %",   "الوصول للطرف الصحيح %", ReportMetricKind.RightPartyRate, type: "percent"),
                    M("ob_conv",                 "Conversion Rate %",       "معدل التحويل %",       ReportMetricKind.ConversionRate, type: "percent"),
                    // Abandoned (predictive over-dial drop) over answered calls. Denominator is
                    // connected_count (answered) so the ratio can never exceed 100%; for strict
                    // TCPA live-answer basis, swap the denominator to rpc_count once AMD tagging
                    // on abandoned attempts is confirmed. See MetricReportBuilder.RatioMetrics.
                    M("abandonment_rate_tcpa",   "Abandonment Rate (TCPA) %","معدل الهجر (TCPA) %",  ReportMetricKind.AbandonmentRate, type: "percent"),
                },
            },

            // Customers — the `contacts` roster (current-state, not date-filtered). Frontend id "crm".
            ["crm"] = new()
            {
                Key = "crm",
                LabelEn = "Customer / CRM", LabelAr = "العملاء / CRM", Icon = "UsersRound",
                DescriptionEn = "Customer roster and repeat contact", DescriptionAr = "قائمة العملاء والتواصل المتكرر",
                CollectionName = "contacts",
                DateField = "createdAt",
                DateFiltered = false,
                Fields = new[]
                {
                    F("contactId",  "Contact ID",  "معرف العميل",      "_id",        "string"),
                    F("name",       "Name",        "الاسم",            "name",       "string"),
                    F("phone",      "Phone",       "الهاتف",           "phone",      "string"),
                    F("email",      "Email",       "البريد الإلكتروني", "email",      "string"),
                    F("company",    "Company",     "الشركة",           "company",    "string", dim: true),
                    F("tags",       "Tags",        "الوسوم",           "tagIds",     "string"),
                    F("totalCalls", "Total Calls", "إجمالي المكالمات", "totalCalls", "number"),
                    F("lastCallAt", "Last Call",   "آخر مكالمة",       "lastCallAt", "date"),
                    F("createdAt",  "Created At",  "تاريخ الإنشاء",    "createdAt",  "date"),
                },
                Metrics = new[]
                {
                    M("count",               "Customers",           "العملاء",              ReportMetricKind.Count),
                    M("active_customers",    "Active Customers",    "العملاء النشطون",      ReportMetricKind.Count, canonical: "count"),
                    M("repeat_count",        "Repeat Customers",    "العملاء المتكررون",    ReportMetricKind.RepeatContactCount),
                    M("repeat_contact_rate", "Repeat Contact Rate %","معدل التواصل المتكرر %", ReportMetricKind.ContactRate, type: "percent"),
                },
            },

            // ---- Planned sources (no data captured yet) — listed by the API as not ready. ----
            ["channels"]   = Stub("channels",   "Channels — Cross-channel",   "القنوات — متعدّدة",       "Globe",       "Cross-channel mix and effectiveness", "مزيج القنوات وفعاليتها"),
            ["social"]     = Stub("social",     "Social Media",               "وسائل التواصل",           "Share2",      "Public mentions and direct messages", "الإشارات العامة والرسائل"),
            ["sms"]        = Stub("sms",        "SMS / Text Messaging",       "الرسائل النصية",          "Send",        "SMS and short-form messaging", "رسائل SMS والرسائل القصيرة"),
            ["ivr"]        = Stub("ivr",        "IVR / Self-Service",         "الرد الآلي",              "Workflow",    "Voice menu and self-service paths", "قوائم الصوت والخدمة الذاتية"),
            ["wfm"]        = Stub("wfm",        "Workforce Management — WFM",  "إدارة القوى العاملة",     "Users",       "Forecasting, scheduling, adherence", "التنبؤ والجدولة والالتزام"),
            ["qm"]         = Stub("qm",         "Quality Management — QM",     "إدارة الجودة",            "Target",      "Evaluations, coaching, calibration", "التقييمات والتدريب والمعايرة"),
            ["kb"]         = Stub("kb",         "Knowledge Base",             "قاعدة المعرفة",           "BookOpen",    "Article views, search, helpfulness", "المقالات والبحث والفائدة"),
            ["bot"]        = Stub("bot",        "Bot / AI Conversations",     "البوت / محادثات الذكاء",  "Sparkles",    "Chatbot and voicebot interactions", "تفاعلات البوت"),
            ["speech"]     = Stub("speech",     "Speech & Text Analytics",    "تحليلات الكلام والنص",    "Activity",    "Sentiment, topics, compliance phrases", "المشاعر والمواضيع والامتثال"),
            ["callback"]   = Stub("callback",   "Callbacks / Virtual Hold",   "إعادة الاتصال",           "Repeat",      "Scheduled callbacks and virtual queueing", "إعادة اتصال مجدولة"),
            ["cost"]       = Stub("cost",       "Cost / Operations",          "التكلفة / العمليات",      "TrendingUp",  "Cost per contact, channel economics", "التكلفة واقتصاد القنوات"),
            ["compliance"] = Stub("compliance", "Compliance",                 "الامتثال",                "CheckCircle2","Recording, retention, regulatory", "التسجيل والاحتفاظ والامتثال"),
        };

    public static IReadOnlyCollection<ReportDataSourceDefinition> All => _sources.Values;

    public static ReportDataSourceDefinition? Find(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        return _sources.TryGetValue(key, out var s) ? s : null;
    }
}
