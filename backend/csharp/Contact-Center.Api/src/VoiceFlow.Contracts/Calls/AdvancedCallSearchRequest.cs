using VoiceFlow.Core.Enums;

namespace VoiceFlow.Contracts.Calls;

public sealed class AdvancedCallSearchRequest
{
    private int _page = 1;
    private int _pageSize = 25;

    public string DateRange { get; set; } = "all";
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public List<CallTypeFilter> Types { get; set; } = [];
    public List<string> InboundStates { get; set; } = [];
    public List<string> OutboundStates { get; set; } = [];
    public List<string> CampaignStates { get; set; } = [];
    public List<string> InternalStates { get; set; } = [];
    public List<string> Statuses { get; set; } = [];
    public List<CallPropertyFilter> Properties { get; set; } = [];
    public List<Sentiment> Sentiment { get; set; } = [];
    public bool HangUpByAgent { get; set; }
    public List<string> AbandonmentReasons { get; set; } = [];
    public List<string> AgentIds { get; set; } = [];
    public List<string> GroupIds { get; set; } = [];
    public List<string> TagIds { get; set; } = [];
    public string? Caller { get; set; }
    public string? Callee { get; set; }
    public HandledByFilter HandledBy { get; set; } = HandledByFilter.Any;
    public string? CallId { get; set; }
    public string? ReferenceId { get; set; }
    public string? Keyword { get; set; }
    public SearchOperatorFilter SearchOperator { get; set; } = SearchOperatorFilter.And;
    public DurationFilter Duration { get; set; } = new();
    public DurationFilter HandlingDuration { get; set; } = new();
    public DurationFilter WaitingDuration { get; set; } = new();
    public DurationFilter HoldingDuration { get; set; } = new();

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch { < 1 => 1, > 200 => 200, _ => value };
    }

    public string SortBy { get; set; } = "startedAt";
    public SortDirectionFilter SortDir { get; set; } = SortDirectionFilter.Desc;

    public int Skip => (Page - 1) * PageSize;
}
