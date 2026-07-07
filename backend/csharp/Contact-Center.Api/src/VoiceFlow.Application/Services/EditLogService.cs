using MongoDB.Bson;
using System.Text.Json;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.EditLogs;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Application.Services;

public sealed class EditLogService : IEditLogService
{
    private readonly IEditLogRepository _repository;

    public EditLogService(IEditLogRepository repository) => _repository = repository;

    public async Task<Result<PagedResponse<EditLogResponse>>> SearchAsync(string tenantId, EditLogSearchRequest request, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _repository.SearchAsync(
            tenantId,
            request.EntityType,
            request.EntityId,
            request.UserId,
            request.Action,
            request.From,
            request.To,
            request.SummarySearch,
            request.Skip,
            request.PageSize,
            cancellationToken);
        return PagedResponse<EditLogResponse>.Create(items.Select(MapToResponse).ToList().AsReadOnly(), request.Page, request.PageSize, total);
    }

    public async Task<Result<EditLogResponse>> CreateAsync(string tenantId, string userId, CreateEditLogRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.EntityType))
            return Result.Failure<EditLogResponse>(Error.Validation(nameof(request.EntityType), "Entity type is required."));
        if (string.IsNullOrWhiteSpace(request.EntityId))
            return Result.Failure<EditLogResponse>(Error.Validation(nameof(request.EntityId), "Entity id is required."));
        if (string.IsNullOrWhiteSpace(request.Action))
            return Result.Failure<EditLogResponse>(Error.Validation(nameof(request.Action), "Action is required."));

        var inserted = await InsertLogAsync(
            tenantId,
            userId,
            request.EntityType.Trim(),
            request.EntityId.Trim(),
            request.Action.Trim(),
            request.Field,
            request.OldValue,
            request.NewValue,
            request.Summary,
            cancellationToken);
        return MapToResponse(inserted);
    }

    public async Task LogAsync(string tenantId, string userId, string entityType, string entityId, string action, string? field = null, JsonElement? oldValue = null, JsonElement? newValue = null, string? summary = null, CancellationToken cancellationToken = default)
    {
        await InsertLogAsync(tenantId, userId, entityType, entityId, action, field, oldValue, newValue, summary, cancellationToken);
    }

    private async Task<EditLog> InsertLogAsync(
        string tenantId,
        string userId,
        string entityType,
        string entityId,
        string action,
        string? field,
        JsonElement? oldValue,
        JsonElement? newValue,
        string? summary,
        CancellationToken cancellationToken)
    {
        var log = new EditLog
        {
            TenantId = tenantId,
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Field = field,
            OldValue = OptionalJsonElementToBson(oldValue),
            NewValue = OptionalJsonElementToBson(newValue),
            Summary = summary
        };
        return await _repository.InsertAsync(log, cancellationToken);
    }

    private static BsonValue? OptionalJsonElementToBson(JsonElement? element)
    {
        if (!element.HasValue) return null;
        var e = element.Value;
        return e.ValueKind == JsonValueKind.Undefined ? null : JsonElementToBson(e);
    }

    private static BsonValue JsonElementToBson(JsonElement e) =>
        e.ValueKind switch
        {
            JsonValueKind.Null => BsonNull.Value,
            JsonValueKind.String => e.GetString() ?? string.Empty,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => e.TryGetInt64(out var l)
                ? l
                : e.TryGetDecimal(out var d) ? Convert.ToDouble(d) : e.GetDouble(),
            JsonValueKind.Object => ObjectToBsonDocument(e),
            JsonValueKind.Array => ArrayToBsonArray(e),
            _ => BsonNull.Value
        };

    private static BsonDocument ObjectToBsonDocument(JsonElement e)
    {
        var doc = new BsonDocument();
        foreach (var prop in e.EnumerateObject())
            doc[prop.Name] = JsonElementToBson(prop.Value);
        return doc;
    }

    private static BsonArray ArrayToBsonArray(JsonElement e)
    {
        var arr = new BsonArray();
        foreach (var item in e.EnumerateArray())
            arr.Add(JsonElementToBson(item));
        return arr;
    }

    private static object? BsonToApiObject(BsonValue? value)
    {
        if (value is null || value.IsBsonNull) return null;
        return BsonTypeMapper.MapToDotNetValue(value);
    }

    private static EditLogResponse MapToResponse(EditLog e) => new()
    {
        Id = e.Id,
        UserId = e.UserId,
        EntityType = e.EntityType,
        EntityId = e.EntityId,
        Action = e.Action,
        Field = e.Field,
        OldValue = BsonToApiObject(e.OldValue),
        NewValue = BsonToApiObject(e.NewValue),
        Summary = e.Summary,
        CreatedAt = e.CreatedAt
    };
}
