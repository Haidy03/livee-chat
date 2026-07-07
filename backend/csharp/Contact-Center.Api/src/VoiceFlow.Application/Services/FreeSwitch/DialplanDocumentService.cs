using VoiceFlow.Application.Interfaces.FreeSwitch;
using VoiceFlow.Contracts.FreeSwitch;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities.FreeSwitch;
using VoiceFlow.Core.Interfaces.Repositories.FreeSwitch;
using VoiceFlow.Domain.FreeSwitch;

namespace VoiceFlow.Reports.Application.Services.FreeSwitch;

public sealed class DialplanDocumentService : IDialplanDocumentService
{
    private readonly IDialplanDocumentRepository _repo;

    public DialplanDocumentService(IDialplanDocumentRepository repo) => _repo = repo;

    public async Task<Result<PushDialplanDocumentsResponse>> PushAsync(PushDialplanDocumentsRequest request, CancellationToken ct)
    {
        var records = request?.Records ?? new();
        if (records.Count == 0)
            return Result.Success(new PushDialplanDocumentsResponse { Inserted = 0 });

        List<DialplanDocument> entities = new();

        for (var i = 0; i < records.Count; i++)
        {
            var r = records[i];
           /* if (string.IsNullOrWhiteSpace(r.Id))
                return Result.Failure<PushDialplanDocumentsResponse>(Error.Validation("dialplan.invalid_id", $"records[{i}].id is required."));
           */ if (string.IsNullOrWhiteSpace(r.Domain))
                return Result.Failure<PushDialplanDocumentsResponse>(Error.Validation("dialplan.invalid_domain", $"records[{i}].domain is required."));
            if (string.IsNullOrWhiteSpace(r.Context))
                return Result.Failure<PushDialplanDocumentsResponse>(Error.Validation("dialplan.invalid_context", $"records[{i}].context is required."));
            if (string.IsNullOrWhiteSpace(r.Name))
                return Result.Failure<PushDialplanDocumentsResponse>(Error.Validation("dialplan.invalid_name", $"records[{i}].name is required."));

            entities.Add(ToEntity(r));

        }

        var inserted = await _repo.UpsertManyAsync(entities, ct);
        return Result.Success(new PushDialplanDocumentsResponse { Inserted = inserted });
    }


    private static DialplanDocument ToEntity(DialplanDocumentDto r)
    {
        return new DialplanDocument
        {
            TenantId = r.TenantId ?? string.Empty,
            Domain = r.Domain ?? string.Empty,
            Context = r.Context ?? string.Empty,
            Name = r.Name ?? string.Empty,
            Enabled = r.Enabled,
            Priority = r.Priority,
            RenderMode = r.RenderMode ?? "structured",

            Entries = (r.Entries ?? [])
                .Select(ToEntity)
                .ToList()
        };
    }

    private static DialplanEntry ToEntity(DialplanEntryDto e)
    {
        return new DialplanEntry
        {
            Name = e.Name ?? string.Empty,
            RouteType = e.RouteType ?? "default",
            Priority = e.Priority,

            Match = new DialplanMatch
            {
                Field = e.Match?.Field ?? "destination_number",
                Type = e.Match?.Type ?? "regex",
                Pattern = e.Match?.Pattern ?? string.Empty
            },

            Validation = e.Validation is null
                ? null
                : new DialplanValidation
                {
                    Field = e.Validation.Field ?? string.Empty,
                    Type = e.Validation.Type ?? string.Empty,
                    Pattern = e.Validation.Pattern ?? string.Empty
                },

            Actions = (e.Actions ?? [])
                .Select(a => new DialplanAction
                {
                    Application = a.Application ?? string.Empty,
                    Data = a.Data?.ToString()
                })
                .ToList()
        };
    }


}
