using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Campaigns;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Application.Services;

public sealed class CampaignService : ICampaignService
{
    private readonly ICampaignRepository _campaigns;
    private readonly ICampaignTargetRepository _targets;
    private readonly ICampaignActivityRepository _activity;
    private readonly ICampaignReceivedCallRepository _receivedCalls;

    public CampaignService(
        ICampaignRepository campaigns,
        ICampaignTargetRepository targets,
        ICampaignActivityRepository activity,
        ICampaignReceivedCallRepository receivedCalls)
    {
        _campaigns = campaigns;
        _targets = targets;
        _activity = activity;
        _receivedCalls = receivedCalls;
    }

    public async Task<Result<IEnumerable<CampaignResponse>>> GetAllAsync(string tenantId, CancellationToken ct = default)
    {
        var items = await _campaigns.GetByTenantAsync(tenantId, ct);
        return Result.Success(items.Select(MapToResponse));
    }

    public async Task<Result<CampaignResponse>> GetByIdAsync(string id, string tenantId, CancellationToken ct = default)
    {
        var item = await _campaigns.GetByIdForTenantAsync(id, tenantId, ct);
        if (item is null) return Result.Failure<CampaignResponse>(Error.NotFound("Campaign", id));
        return MapToResponse(item);
    }

    public async Task<Result<CampaignResponse>> CreateAsync(string tenantId, CreateCampaignRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var isInbound = request.Type == "inbound_support";

        var entity = new Campaign
        {
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Type = request.Type,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "draft" : request.Status,
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? "medium" : request.Priority,
            Description = request.Description,
            Script = request.Script,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            AssignedMode = string.IsNullOrWhiteSpace(request.AssignedMode) ? "agents" : request.AssignedMode,
            AgentIds = (string.IsNullOrWhiteSpace(request.AssignedMode) || request.AssignedMode == "agents") ? (request.AgentIds ?? new()) : new(),
            QueueId = request.AssignedMode == "queue" ? request.QueueId : null,
            DialingMode = string.IsNullOrWhiteSpace(request.DialingMode) ? "progressive" : request.DialingMode,
            PowerRatio = request.PowerRatio ?? 1.0,
            InboundSettings = isInbound ? MapInbound(request.InboundSettings) : null,
            CreatedAt = now,
            UpdatedAt = now,
            LastActivityAt = now,
            Version = 1,
        };

        await _campaigns.InsertAsync(entity, ct);

        // Seed initial targets (outbound) or received calls (inbound) into their own collections.
        if (request.Contacts is { Count: > 0 })
        {
            var targets = request.Contacts.Select(c => MapDtoToTarget(c, tenantId, entity.Id, now)).ToList();
            await _targets.InsertManyAsync(targets, ct);
            ApplyCountsFromTargets(entity, targets);
            await _campaigns.UpdateAsync(entity, ct);
        }

        if (isInbound && request.ReceivedCalls is { Count: > 0 })
        {
            var items = request.ReceivedCalls.Select(r => MapDtoToReceivedCall(r, tenantId, entity.Id, now)).ToList();
            await _receivedCalls.InsertManyAsync(items, ct);
        }

        await _activity.InsertManyAsync(new[]
        {
            new CampaignActivityItem
            {
                TenantId = tenantId,
                CampaignId = entity.Id,
                At = now.ToString("O"),
                Type = "created",
                Message = "Campaign created",
                CreatedAt = now,
                UpdatedAt = now,
            }
        }, ct);

        return MapToResponse(entity);
    }

    public async Task<Result<CampaignResponse>> UpdateAsync(string id, string tenantId, UpdateCampaignRequest request, CancellationToken ct = default)
    {
        var entity = await _campaigns.GetByIdForTenantAsync(id, tenantId, ct);
        if (entity is null) return Result.Failure<CampaignResponse>(Error.NotFound("Campaign", id));

        if (request.Name is not null) entity.Name = request.Name.Trim();
        if (request.Type is not null) entity.Type = request.Type;
        if (request.Status is not null) entity.Status = request.Status;
        if (request.Priority is not null) entity.Priority = request.Priority;
        if (request.Description is not null) entity.Description = request.Description;
        if (request.Script is not null) entity.Script = request.Script;
        if (request.StartDate is not null) entity.StartDate = request.StartDate;
        if (request.EndDate is not null) entity.EndDate = request.EndDate;
        if (!string.IsNullOrWhiteSpace(request.AssignedMode)) entity.AssignedMode = request.AssignedMode;
        if (request.AgentIds is not null) entity.AgentIds = request.AgentIds;
        if (request.QueueId is not null) entity.QueueId = string.IsNullOrWhiteSpace(request.QueueId) ? null : request.QueueId;
        // Enforce mutual exclusivity based on the (possibly updated) AssignedMode.
        if (entity.AssignedMode == "queue") entity.AgentIds = new();
        else if (entity.AssignedMode == "agents") entity.QueueId = null;
        if (!string.IsNullOrWhiteSpace(request.DialingMode)) entity.DialingMode = request.DialingMode;
        if (request.PowerRatio is not null) entity.PowerRatio = request.PowerRatio.Value;
        if (request.InboundSettings is not null) entity.InboundSettings = MapInbound(request.InboundSettings);

        entity.Version += 1;
        entity.LastActivityAt = DateTime.UtcNow;
        await _campaigns.UpdateAsync(entity, ct);

        await _activity.InsertManyAsync(new[]
        {
            new CampaignActivityItem
            {
                TenantId = tenantId,
                CampaignId = entity.Id,
                At = DateTime.UtcNow.ToString("O"),
                Type = "edited",
                Message = "Campaign updated",
            }
        }, ct);

        return MapToResponse(entity);
    }

    public async Task<Result> DeleteAsync(string id, string tenantId, CancellationToken ct = default)
    {
        var entity = await _campaigns.GetByIdForTenantAsync(id, tenantId, ct);
        if (entity is null) return Result.Failure(Error.NotFound("Campaign", id));

        await _targets.DeleteAllForCampaignAsync(tenantId, id, ct);
        await _activity.DeleteAllForCampaignAsync(tenantId, id, ct);
        await _receivedCalls.DeleteAllForCampaignAsync(tenantId, id, ct);
        await _campaigns.DeleteAsync(id, ct);
        return Result.Success();
    }

    public async Task<Result<CampaignResponse>> SetStatusAsync(string id, string tenantId, SetCampaignStatusRequest request, CancellationToken ct = default)
    {
        var entity = await _campaigns.GetByIdForTenantAsync(id, tenantId, ct);
        if (entity is null) return Result.Failure<CampaignResponse>(Error.NotFound("Campaign", id));

        var previous = entity.Status;
        entity.Status = request.Status;
        entity.Version += 1;
        entity.LastActivityAt = DateTime.UtcNow;

        var activityType = request.Status switch
        {
            "active" => previous == "paused" ? "resumed" : "launched",
            "paused" => "paused",
            "completed" => "completed",
            "cancelled" => "cancelled",
            _ => null,
        };

        await _campaigns.UpdateAsync(entity, ct);

        if (activityType is not null)
        {
            await _activity.InsertManyAsync(new[]
            {
                new CampaignActivityItem
                {
                    TenantId = tenantId,
                    CampaignId = entity.Id,
                    At = DateTime.UtcNow.ToString("O"),
                    Type = activityType,
                    Message = $"Campaign {request.Status}",
                }
            }, ct);
        }

        return MapToResponse(entity);
    }

    // -------- Targets --------

    public async Task<Result<PagedResponse<CampaignContactDto>>> ListTargetsAsync(string id, string tenantId, ListCampaignTargetsRequest request, CancellationToken ct = default)
    {
        var campaign = await _campaigns.GetByIdForTenantAsync(id, tenantId, ct);
        if (campaign is null) return Result.Failure<PagedResponse<CampaignContactDto>>(Error.NotFound("Campaign", id));

        var filter = new CampaignTargetListFilter
        {
            Status = request.Status,
            Search = request.Search,
            Page = request.Page,
            PageSize = request.PageSize,
        };
        var page = await _targets.ListAsync(tenantId, id, filter, ct);
        var items = page.Items.Select(MapTargetToDto).ToList();
        return PagedResponse<CampaignContactDto>.Create(items, request.Page, request.PageSize, page.TotalCount);
    }

    public async Task<Result<int>> AddTargetsAsync(string id, string tenantId, AddCampaignContactsRequest request, CancellationToken ct = default)
    {
        var campaign = await _campaigns.GetByIdForTenantAsync(id, tenantId, ct);
        if (campaign is null) return Result.Failure<int>(Error.NotFound("Campaign", id));

        if (request.Contacts.Count == 0) return 0;

        var now = DateTime.UtcNow;
        var targets = request.Contacts.Select(c => MapDtoToTarget(c, tenantId, id, now)).ToList();

        // Chunk to keep individual InsertMany calls bounded.
        const int chunkSize = 1000;
        var inserted = 0;
        for (var i = 0; i < targets.Count; i += chunkSize)
        {
            var chunk = targets.Skip(i).Take(chunkSize).ToList();
            inserted += (int)await _targets.InsertManyAsync(chunk, ct);
        }

        // Update counters atomically via $inc.
        var deltas = new Dictionary<string, long> { ["total"] = inserted };
        foreach (var t in targets)
        {
            deltas.TryGetValue(t.Status, out var current);
            deltas[t.Status] = current + 1;
        }
        await _campaigns.ApplyTargetCounterDeltasAsync(id, tenantId, deltas, ct);

        await _activity.InsertManyAsync(new[]
        {
            new CampaignActivityItem
            {
                TenantId = tenantId,
                CampaignId = id,
                At = now.ToString("O"),
                Type = "contacts_added",
                Message = $"Added {inserted} contact{(inserted == 1 ? string.Empty : "s")}",
            }
        }, ct);

        return inserted;
    }

    public async Task<Result> RemoveTargetAsync(string id, string tenantId, string targetId, CancellationToken ct = default)
    {
        var deleted = await _targets.DeleteForCampaignAsync(tenantId, id, targetId, ct);
        if (deleted is null) return Result.Failure(Error.NotFound("CampaignTarget", targetId));

        var deltas = new Dictionary<string, long>
        {
            ["total"] = -1,
            [deleted.Status] = -1,
        };
        await _campaigns.ApplyTargetCounterDeltasAsync(id, tenantId, deltas, ct);
        return Result.Success();
    }

    public async Task<Result<CampaignContactDto>> UpdateTargetStatusAsync(string id, string tenantId, string targetId, UpdateCampaignContactStatusRequest request, CancellationToken ct = default)
    {
        var nowIso = DateTime.UtcNow.ToString("O");
        var result = await _targets.UpdateStatusAsync(tenantId, id, targetId, request.Status, nowIso, ct);
        if (result is null) return Result.Failure<CampaignContactDto>(Error.NotFound("CampaignTarget", targetId));

        var (previous, next) = result.Value;
        if (previous != next)
        {
            var deltas = new Dictionary<string, long>
            {
                [previous] = -1,
                [next] = +1,
            };
            await _campaigns.ApplyTargetCounterDeltasAsync(id, tenantId, deltas, ct);
        }

        var updated = await _targets.GetForCampaignAsync(tenantId, id, targetId, ct);
        return MapTargetToDto(updated!);
    }

    // -------- Activity --------

    public async Task<Result<PagedResponse<CampaignActivityEntryDto>>> ListActivityAsync(string id, string tenantId, int page, int pageSize, CancellationToken ct = default)
    {
        var campaign = await _campaigns.GetByIdForTenantAsync(id, tenantId, ct);
        if (campaign is null) return Result.Failure<PagedResponse<CampaignActivityEntryDto>>(Error.NotFound("Campaign", id));

        var result = await _activity.ListAsync(tenantId, id, page, pageSize, ct);
        var items = result.Items.Select(MapActivityToDto).ToList();
        return PagedResponse<CampaignActivityEntryDto>.Create(items, page, pageSize, result.TotalCount);
    }

    public async Task<Result<CampaignActivityEntryDto>> AddActivityAsync(string id, string tenantId, AddCampaignActivityRequest request, CancellationToken ct = default)
    {
        var campaign = await _campaigns.GetByIdForTenantAsync(id, tenantId, ct);
        if (campaign is null) return Result.Failure<CampaignActivityEntryDto>(Error.NotFound("Campaign", id));

        var entry = new CampaignActivityItem
        {
            TenantId = tenantId,
            CampaignId = id,
            At = DateTime.UtcNow.ToString("O"),
            Type = request.Type,
            Message = request.Message,
        };
        await _activity.InsertManyAsync(new[] { entry }, ct);
        return MapActivityToDto(entry);
    }

    // -------- Received calls --------

    public async Task<Result<PagedResponse<CampaignReceivedCallDto>>> ListReceivedCallsAsync(string id, string tenantId, int page, int pageSize, CancellationToken ct = default)
    {
        var campaign = await _campaigns.GetByIdForTenantAsync(id, tenantId, ct);
        if (campaign is null) return Result.Failure<PagedResponse<CampaignReceivedCallDto>>(Error.NotFound("Campaign", id));

        var result = await _receivedCalls.ListAsync(tenantId, id, page, pageSize, ct);
        var items = result.Items.Select(MapReceivedCallToDto).ToList();
        return PagedResponse<CampaignReceivedCallDto>.Create(items, page, pageSize, result.TotalCount);
    }

    public async Task<Result<CampaignReceivedCallDto>> AddReceivedCallAsync(string id, string tenantId, AddCampaignReceivedCallRequest request, CancellationToken ct = default)
    {
        var campaign = await _campaigns.GetByIdForTenantAsync(id, tenantId, ct);
        if (campaign is null) return Result.Failure<CampaignReceivedCallDto>(Error.NotFound("Campaign", id));

        var entry = MapDtoToReceivedCall(request.Call, tenantId, id, DateTime.UtcNow);
        await _receivedCalls.InsertManyAsync(new[] { entry }, ct);
        return MapReceivedCallToDto(entry);
    }

    // -------- Mapping helpers --------

    private static CampaignTarget MapDtoToTarget(NewCampaignContactDto dto, string tenantId, string campaignId, DateTime now) => new()
    {
        // Leave Id empty so the MongoDB driver's StringObjectIdGenerator
        // assigns a fresh ObjectId on insert.
        Id = string.Empty,
        TenantId = tenantId,
        CampaignId = campaignId,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Phone = dto.Phone,
        Email = dto.Email,
        Notes = dto.Notes,
        Status = string.IsNullOrWhiteSpace(dto.Status) ? "pending" : dto.Status,
        LastCallAt = dto.LastCallAt,
        Source = string.IsNullOrWhiteSpace(dto.Source) ? "manual" : dto.Source,
        CreatedAt = now,
        UpdatedAt = now,
    };

    private static CampaignReceivedCallItem MapDtoToReceivedCall(CampaignReceivedCallDto dto, string tenantId, string campaignId, DateTime now) => new()
    {
        // Same rationale as targets: empty Id => Mongo assigns ObjectId.
        Id = string.IsNullOrWhiteSpace(dto.Id) ? string.Empty : dto.Id,
        TenantId = tenantId,
        CampaignId = campaignId,
        CallerName = dto.CallerName,
        Phone = dto.Phone,
        At = dto.At,
        DurationSec = dto.DurationSec,
        WaitSec = dto.WaitSec,
        AgentId = dto.AgentId,
        Status = dto.Status,
        Notes = dto.Notes,
        CreatedAt = now,
        UpdatedAt = now,
    };

    private static CampaignContactDto MapTargetToDto(CampaignTarget t) => new()
    {
        Id = t.Id,
        FirstName = t.FirstName,
        LastName = t.LastName,
        Phone = t.Phone,
        Email = t.Email,
        Notes = t.Notes,
        Status = t.Status,
        LastCallAt = t.LastCallAt,
        Source = t.Source,
    };

    private static CampaignActivityEntryDto MapActivityToDto(CampaignActivityItem a) => new()
    {
        Id = a.Id,
        At = a.At,
        Type = a.Type,
        Message = a.Message,
    };

    private static CampaignReceivedCallDto MapReceivedCallToDto(CampaignReceivedCallItem r) => new()
    {
        Id = r.Id,
        CallerName = r.CallerName,
        Phone = r.Phone,
        At = r.At,
        DurationSec = r.DurationSec,
        WaitSec = r.WaitSec,
        AgentId = r.AgentId,
        Status = r.Status,
        Notes = r.Notes,
    };

    private static CampaignInboundSettings? MapInbound(CampaignInboundSettingsDto? dto)
    {
        if (dto is null) return null;
        return new CampaignInboundSettings
        {
            QueueName = dto.QueueName,
            ExpectedVolume = dto.ExpectedVolume,
            HoursFrom = dto.HoursFrom,
            HoursTo = dto.HoursTo,
            IvrMessage = dto.IvrMessage,
            OverflowAction = dto.OverflowAction,
        };
    }

    private static void ApplyCountsFromTargets(Campaign campaign, IReadOnlyList<CampaignTarget> seeded)
    {
        campaign.TargetsTotal += seeded.Count;
        foreach (var t in seeded)
        {
            switch (t.Status)
            {
                case "pending": campaign.TargetsPending += 1; break;
                case "called": campaign.TargetsCalled += 1; break;
                case "successful": campaign.TargetsSuccessful += 1; break;
                case "failed": campaign.TargetsFailed += 1; break;
                case "callback": campaign.TargetsCallback += 1; break;
            }
        }
    }

    private static CampaignResponse MapToResponse(Campaign c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Type = c.Type,
        Status = c.Status,
        Priority = c.Priority,
        Description = c.Description,
        Script = c.Script,
        StartDate = c.StartDate,
        EndDate = c.EndDate,
        AgentIds = c.AgentIds ?? new(),
        QueueId = c.QueueId,
        AssignedMode = string.IsNullOrWhiteSpace(c.AssignedMode) ? (string.IsNullOrEmpty(c.QueueId) ? "agents" : "queue") : c.AssignedMode,
        DialingMode = string.IsNullOrWhiteSpace(c.DialingMode) ? "progressive" : c.DialingMode,
        PowerRatio = c.PowerRatio,
        InboundSettings = c.InboundSettings is null ? null : new CampaignInboundSettingsDto
        {
            QueueName = c.InboundSettings.QueueName,
            ExpectedVolume = c.InboundSettings.ExpectedVolume,
            HoursFrom = c.InboundSettings.HoursFrom,
            HoursTo = c.InboundSettings.HoursTo,
            IvrMessage = c.InboundSettings.IvrMessage,
            OverflowAction = c.InboundSettings.OverflowAction,
        },
        Targets = new CampaignTargetCountersDto
        {
            Total = c.TargetsTotal,
            Pending = c.TargetsPending,
            Called = c.TargetsCalled,
            Successful = c.TargetsSuccessful,
            Failed = c.TargetsFailed,
            Callback = c.TargetsCallback,
        },
        LastActivityAt = c.LastActivityAt?.ToString("O"),
        Version = c.Version,
        CreatedAt = c.CreatedAt.ToString("O"),
        UpdatedAt = c.UpdatedAt.ToString("O"),
    };
}
