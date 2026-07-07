using System.Collections.Concurrent;
using VoiceFlow.Api.UserMaps.Requests;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.UserMaps.Responses;

namespace VoiceFlow.Application.Services.UserMap;

/// <summary>
/// In-memory per-tenant simulator that powers the "Users Map" screen.
/// Phase 1: synthetic data driven by a weighted state-transition table that
/// mirrors the original client-side simulator. Phase 2 swaps this for real
/// telemetry from the call platform without changing the HTTP contract.
/// </summary>
public sealed class SimulatorUsersMapService : IUsersMapService
{
    private const int MinDwellSec = 6;
    private const int MaxCallers = 34;
    private const string DefaultTenant = "default";

    private static readonly string[] StateOrder = { "ivr", "ai", "agent", "queue", "vm", "survey" };

    private static readonly Dictionary<string, int> StateCapacity = new()
    {
        ["ivr"] = 20, ["ai"] = 8, ["agent"] = 12, ["queue"] = 15, ["vm"] = 6, ["survey"] = 6,
    };

    private static readonly (string To, double Weight)[] T_Ivr =
    {
        ("ai", 0.30), ("queue", 0.30), ("agent", 0.12), ("vm", 0.10), ("survey", 0.03), ("end", 0.15),
    };
    private static readonly (string, double)[] T_Ai = { ("agent", 0.35), ("survey", 0.20), ("end", 0.45) };
    private static readonly (string, double)[] T_Agent = { ("survey", 0.40), ("end", 0.60) };
    private static readonly (string, double)[] T_Queue = { ("agent", 0.60), ("vm", 0.12), ("end", 0.28) };
    private static readonly (string, double)[] T_Vm = { ("end", 1.0) };
    private static readonly (string, double)[] T_Survey = { ("end", 1.0) };

    private static readonly Dictionary<string, (string, double)[]> Transitions = new()
    {
        ["ivr"] = T_Ivr, ["ai"] = T_Ai, ["agent"] = T_Agent,
        ["queue"] = T_Queue, ["vm"] = T_Vm, ["survey"] = T_Survey,
    };

    private static readonly Dictionary<string, (string Key, string Label)> NodeForState = new()
    {
        ["ivr"]   = ("menu_main", "menu_main"),
        ["ai"]    = ("ai_agent", "ai_agent"),
        ["agent"] = ("transfer", "transfer"),
        ["queue"] = ("queue_main", "queue_main"),
        ["vm"]    = ("voicemail", "voicemail"),
        ["survey"]= ("survey", "survey"),
    };

    private static readonly string[] ArabicNames =
    {
        "محمد العتيبي","سارة الدوسري","خالد القحطاني","نورة الشمري",
        "عبدالله الغامدي","ريم الحربي","فهد المطيري","لمى الزهراني",
        "ماجد السبيعي","جواهر العنزي","تركي الرشيدي","هند البقمي",
        "سلطان الشهري","العنود الفيفي","بدر الحارثي","وعد القرني",
    };

    private static readonly string[] AvatarPalette =
    {
        "#1f9d6b","#4f63d2","#7c5cf6","#e09322","#0fa39f","#647590",
        "#c0436b","#5d8f3a","#b9772a","#3a8aab",
    };

    private static readonly string[] IntentKeys = { "billing", "tech", "inquiry", "sales", "complaints" };
    private static readonly string[] IvrChoiceKeys = { "mainMenu", "sales", "stores", "support", "agent" };

    private static readonly (string Id, string Name)[] AgentPool =
    {
        ("u_1","أحمد سالم"),("u_2","منى فيصل"),("u_3","وليد ناصر"),
        ("u_4","ليان عبدالله"),("u_5","سعد حمد"),("u_6","ريما خالد"),
    };

    private static readonly FlowSummaryResponse SampleFlowTemplate = new()
    {
        Id = "flow_8821",
        Name = "Test (copy)",
        Nodes = new()
        {
            new() { Key = "start",      Label = "start",      Type = "start" },
            new() { Key = "menu_main",  Label = "menu_main",  Type = "menu" },
            new() { Key = "ai_agent",   Label = "ai_agent",   Type = "ai" },
            new() { Key = "queue_main", Label = "queue_main", Type = "queue" },
            new() { Key = "transfer",   Label = "transfer",   Type = "transfer" },
            new() { Key = "survey",     Label = "survey",     Type = "survey" },
            new() { Key = "voicemail",  Label = "voicemail",  Type = "voicemail" },
        },
    };

    private readonly ConcurrentDictionary<string, TenantState> _tenants = new();

    public Task<UsersMapSnapshotResponse> GetSnapshotAsync(string tenantId, CancellationToken ct)
    {
        var key = string.IsNullOrWhiteSpace(tenantId) ? DefaultTenant : tenantId;
        var state = _tenants.GetOrAdd(key, _ => TenantState.Seed());
        lock (state.Sync)
        {
            state.Advance();
            return Task.FromResult(state.ToSnapshot());
        }
    }

    public Task<bool> ActionAsync(string tenantId, string callId, string kind, TransferRequest? transfer, CancellationToken ct)
    {
        var key = string.IsNullOrWhiteSpace(tenantId) ? DefaultTenant : tenantId;
        if (!_tenants.TryGetValue(key, out var state)) return Task.FromResult(true);
        lock (state.Sync)
        {
            var caller = state.Callers.FirstOrDefault(c => c.CallId == callId || c.Id == callId);
            if (caller == null) return Task.FromResult(true);

            switch (kind.ToLowerInvariant())
            {
                case "hangup":
                    state.Callers.Remove(caller);
                    if (caller.State == "queue") state.AbandonedToday++;
                    break;
                case "transfer":
                    caller.State = "agent";
                    caller.EnteredStateAt = DateTimeOffset.UtcNow.ToString("O");
                    caller.Agent = new AgentRefResponse
                    {
                        Id = transfer?.Target.Id ?? AgentPool[0].Id,
                        Name = AgentPool[0].Name,
                    };
                    break;
                case "listen":
                case "whisper":
                case "barge":
                    // No state change — supervisor session is established elsewhere.
                    break;
            }
        }
        return Task.FromResult(true);
    }

    // ----- internal state -----

    private sealed class TenantState
    {
        public readonly object Sync = new();
        public List<CallerResponse> Callers = new();
        public DateTimeOffset LastTickAt = DateTimeOffset.UtcNow;
        public DateTimeOffset LastSpawnAt = DateTimeOffset.UtcNow;
        public int AbandonedToday = 14;
        public int AvgHandleSec = 214;
        public int SeedInt;

        public static TenantState Seed()
        {
            var s = new TenantState();
            var rng = new Random(Environment.TickCount);
            var dist = new Dictionary<string, int>
            {
                ["ivr"] = 6, ["ai"] = 4, ["agent"] = 5, ["queue"] = 4, ["vm"] = 2, ["survey"] = 2,
            };
            int idx = 0;
            foreach (var st in StateOrder)
            {
                for (int i = 0; i < dist[st]; i++)
                {
                    var age = 8 + rng.Next(180);
                    s.Callers.Add(BuildCaller(st, age, idx++, rng, st == "queue" ? i + 1 : (int?)null));
                }
            }
            return s;
        }

        public void Advance()
        {
            var rng = new Random();
            var now = DateTimeOffset.UtcNow;

            // Spawn cadence: ~ every 3s when under cap.
            if (Callers.Count < MaxCallers && (now - LastSpawnAt).TotalSeconds >= 3 && rng.NextDouble() < 0.7)
            {
                Callers.Add(BuildCaller("ivr", 1, ++SeedInt + 1000, rng));
                LastSpawnAt = now;
            }

            // Attempt one transition per tick window (~ every 2s).
            if ((now - LastTickAt).TotalSeconds < 2) return;
            LastTickAt = now;

            var eligible = Callers
                .Where(c => (now - DateTimeOffset.Parse(c.EnteredStateAt)).TotalSeconds >= MinDwellSec)
                .ToList();
            if (eligible.Count == 0 || rng.NextDouble() >= 0.7) return;

            var target = eligible[rng.Next(eligible.Count)];
            var dest = WeightedPick(Transitions[target.State], rng);

            if (dest == "end")
            {
                if (target.State == "queue") AbandonedToday++;
                Callers.Remove(target);
                return;
            }

            var node = NodeForState[target.State];
            target.History.Add(new JourneyStepResponse
            {
                At = target.EnteredStateAt,
                NodeKey = node.Key,
                Label = node.Label,
                Sub = target.Detail ?? target.IvrChoice,
            });
            target.State = dest;
            target.EnteredStateAt = now.ToString("O");
            target.Detail = dest == "ai" ? IntentKeys[rng.Next(IntentKeys.Length)]
                          : dest == "vm" ? "leavingMessage"
                          : null;
            if (dest == "ai") target.Intent = IntentKeys[rng.Next(IntentKeys.Length)];
            target.Agent = dest == "agent"
                ? new AgentRefResponse { Id = AgentPool[rng.Next(AgentPool.Length)].Id, Name = AgentPool[rng.Next(AgentPool.Length)].Name }
                : null;
            target.QueuePosition = dest == "queue" ? 0 : null;
            if (dest == "survey")
            {
                target.SurveyStep = 1;
                target.SurveyTotal = 5;
            }
        }

        public UsersMapSnapshotResponse ToSnapshot()
        {
            var now = DateTimeOffset.UtcNow;

            var queue = Callers.Where(c => c.State == "queue")
                .OrderBy(c => DateTimeOffset.Parse(c.EnteredStateAt))
                .ToList();
            for (int i = 0; i < queue.Count; i++) queue[i].QueuePosition = i + 1;
            var longestWait = queue.Count > 0
                ? (int)(now - DateTimeOffset.Parse(queue[0].EnteredStateAt)).TotalSeconds
                : 0;

            var states = StateOrder.ToDictionary(
                s => s,
                s => new StateBucketResponse
                {
                    Count = Callers.Count(c => c.State == s),
                    Capacity = StateCapacity[s],
                });

            var flow = new FlowSummaryResponse
            {
                Id = SampleFlowTemplate.Id,
                Name = SampleFlowTemplate.Name,
                Nodes = SampleFlowTemplate.Nodes.Select(n =>
                {
                    int count = n.Key switch
                    {
                        "start" => Callers.Count,
                        "menu_main" => Callers.Count(c => c.State == "ivr"),
                        "ai_agent" => Callers.Count(c => c.State == "ai"),
                        "transfer" => Callers.Count(c => c.State == "agent"),
                        "queue_main" => Callers.Count(c => c.State == "queue"),
                        "voicemail" => Callers.Count(c => c.State == "vm"),
                        "survey" => Callers.Count(c => c.State == "survey"),
                        _ => 0,
                    };
                    return new FlowNodeResponse { Key = n.Key, Label = n.Label, Type = n.Type, Count = count };
                }).ToList(),
            };

            return new UsersMapSnapshotResponse
            {
                GeneratedAt = now.ToString("O"),
                Metrics = new UsersMapMetricsResponse
                {
                    ActiveNow = Callers.Count,
                    InQueue = queue.Count,
                    LongestWaitSec = longestWait,
                    AvgHandleSec = AvgHandleSec,
                    AbandonedToday = AbandonedToday,
                    SlaPercent = 93,
                    SlaTargetPercent = 90,
                },
                States = states,
                Callers = Callers.Select(Clone).ToList(),
                Flow = flow,
            };
        }
    }

    // ----- helpers -----

    private static string WeightedPick((string Value, double Weight)[] options, Random rng)
    {
        var total = options.Sum(o => o.Weight);
        var r = rng.NextDouble() * total;
        double acc = 0;
        foreach (var o in options)
        {
            acc += o.Weight;
            if (r <= acc) return o.Value;
        }
        return options[^1].Value;
    }

    private static CallerResponse BuildCaller(string state, int ageSec, int seed, Random rng, int? queuePos = null)
    {
        var nowMs = DateTimeOffset.UtcNow;
        var startedAt = nowMs.AddSeconds(-(ageSec + rng.Next(120)));
        var enteredAt = nowMs.AddSeconds(-ageSec);
        var ivrChoice = IvrChoiceKeys[rng.Next(IvrChoiceKeys.Length)];

        var history = new List<JourneyStepResponse>
        {
            new() { At = startedAt.ToString("O"), NodeKey = "start", Label = "start" },
            new() { At = startedAt.AddSeconds(5).ToString("O"), NodeKey = "hours", Label = "hours" },
        };
        if (state != "ivr")
        {
            history.Add(new JourneyStepResponse
            {
                At = startedAt.AddSeconds(12).ToString("O"),
                NodeKey = "menu_main",
                Label = "menu_main",
                Sub = ivrChoice,
            });
        }

        var caller = new CallerResponse
        {
            Id = Guid.NewGuid().ToString("N")[..10],
            CallId = $"ch_PJSIP_{Guid.NewGuid().ToString("N")[..12]}",
            Name = ArabicNames[seed % ArabicNames.Length],
            MaskedNumber = GenMaskedSaudi(rng),
            Color = AvatarPalette[seed % AvatarPalette.Length],
            State = state,
            EnteredStateAt = enteredAt.ToString("O"),
            CallStartedAt = startedAt.ToString("O"),
            IvrChoice = ivrChoice,
            Channel = "voice",
            Direction = "inbound",
            Tags = rng.NextDouble() < 0.4 ? new() { "vip", "lang" } : new() { "lang" },
            History = history,
        };

        if (state == "ai") caller.Intent = IntentKeys[rng.Next(IntentKeys.Length)];
        if (state == "agent")
        {
            var a = AgentPool[rng.Next(AgentPool.Length)];
            caller.Agent = new AgentRefResponse { Id = a.Id, Name = a.Name };
        }
        if (state == "queue") caller.QueuePosition = queuePos ?? 1;
        if (state == "vm") caller.Detail = "leavingMessage";
        if (state == "survey") { caller.SurveyTotal = 5; caller.SurveyStep = 1 + rng.Next(5); }
        return caller;
    }

    private static string GenMaskedSaudi(Random rng)
    {
        var last = rng.Next(0, 10000).ToString("D4");
        return $"+9665••••{last}";
    }

    private static CallerResponse Clone(CallerResponse c) => new()
    {
        Id = c.Id, CallId = c.CallId, Name = c.Name, MaskedNumber = c.MaskedNumber,
        Color = c.Color, State = c.State, EnteredStateAt = c.EnteredStateAt,
        CallStartedAt = c.CallStartedAt, IvrChoice = c.IvrChoice, Intent = c.Intent,
        Detail = c.Detail,
        Agent = c.Agent == null ? null : new AgentRefResponse { Id = c.Agent.Id, Name = c.Agent.Name },
        QueuePosition = c.QueuePosition, SurveyStep = c.SurveyStep, SurveyTotal = c.SurveyTotal,
        Channel = c.Channel, Direction = c.Direction,
        Tags = c.Tags?.ToList(),
        History = c.History.Select(h => new JourneyStepResponse
        {
            At = h.At, NodeKey = h.NodeKey, Label = h.Label, Sub = h.Sub,
        }).ToList(),
    };
}
