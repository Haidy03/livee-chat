using MongoDB.Bson;
using System.Text.Json;
using VoiceFlow.Application.Exporters;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Application.Validators;
using VoiceFlow.Contracts.Flows;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Enums;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Core.ValueObjects;

namespace VoiceFlow.Application.Services;

public sealed class FlowService : IFlowService
{
    private readonly IFlowRepository _flowRepository;
    private readonly FlowValidator _validator;
    private readonly AsteriskExporter _exporter;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IProfileRepository _profileRepository;

    public FlowService(
        IFlowRepository flowRepository,
        FlowValidator validator,
        AsteriskExporter exporter,
        ICampaignRepository campaignRepository,
        IProfileRepository profileRepository)
    {
        _flowRepository = flowRepository;
        _validator = validator;
        _exporter = exporter;
        _campaignRepository = campaignRepository;
        _profileRepository = profileRepository;
    }

    public async Task<Result<IEnumerable<FlowResponse>>> GetFlowsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var flows = await _flowRepository.GetByTenantAsync(tenantId, cancellationToken);
        return Result.Success(flows.Select(MapToResponse));
    }

    public async Task<Result<FlowResponse>> GetFlowAsync(string flowId, string tenantId, CancellationToken cancellationToken = default)
    {
        var flow = await _flowRepository.GetByIdAsync(flowId, cancellationToken);
        if (flow is null || flow.TenantId != tenantId)
            return Result.Failure<FlowResponse>(Error.NotFound("Flow", flowId));

        return MapToResponse(flow);
    }

    public async Task<Result<FlowResponse>> CreateFlowAsync(string tenantId, string userId, CreateFlowRequest request, CancellationToken cancellationToken = default)
    {
        var flow = new Flow
        {
            TenantId = tenantId,
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            Nodes = request.Nodes.Select(MapNodeFromDto).ToList(),
            Edges = request.Edges.Select(MapEdgeFromDto).ToList()
        };

        await _flowRepository.InsertAsync(flow, cancellationToken);
        return MapToResponse(flow);
    }

    public async Task<Result<FlowResponse>> UpdateFlowAsync(string flowId, string tenantId, UpdateFlowRequest request, CancellationToken cancellationToken = default)
    {
        var flow = await _flowRepository.GetByIdAsync(flowId, cancellationToken);
        if (flow is null || flow.TenantId != tenantId)
            return Result.Failure<FlowResponse>(Error.NotFound("Flow", flowId));

        if (request.Name is not null) flow.Name = request.Name;
        if (request.Description is not null) flow.Description = request.Description;
        if (request.Nodes is not null) flow.Nodes = request.Nodes.Select(MapNodeFromDto).ToList();
        if (request.Edges is not null) flow.Edges = request.Edges.Select(MapEdgeFromDto).ToList();
        if (request.Status.HasValue) flow.Status = request.Status.Value;
        if (request.AssignedExtension is not null) flow.AssignedExtension = request.AssignedExtension;

        await _flowRepository.UpdateAsync(flow, cancellationToken);
        return MapToResponse(flow);
    }

    public async Task<Result> DeleteFlowAsync(string flowId, string tenantId, CancellationToken cancellationToken = default)
    {
        var flow = await _flowRepository.GetByIdAsync(flowId, cancellationToken);
        if (flow is null || flow.TenantId != tenantId)
            return Result.Failure(Error.NotFound("Flow", flowId));

        await _flowRepository.DeleteAsync(flowId, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<FlowValidationResponse>> ValidateFlowAsync(string flowId, string tenantId, CancellationToken cancellationToken = default)
    {
        var flow = await _flowRepository.GetByIdAsync(flowId, cancellationToken);
        if (flow is null || flow.TenantId != tenantId)
            return Result.Failure<FlowValidationResponse>(Error.NotFound("Flow", flowId));

        return _validator.Validate(flow);
    }

    public async Task<Result<FlowResponse>> PublishFlowAsync(string flowId, string tenantId, PublishFlowRequest request, CancellationToken cancellationToken = default)
    {
        var flow = await _flowRepository.GetByIdAsync(flowId, cancellationToken);
        if (flow is null || flow.TenantId != tenantId)
            return Result.Failure<FlowResponse>(Error.NotFound("Flow", flowId));

        var validation = _validator.Validate(flow);
        if (!validation.IsValid)
            return Result.Failure<FlowResponse>(Error.Validation("Flow", string.Join("; ", validation.Errors)));

        flow.Status = FlowStatus.Published;
        if (request.AssignedExtension is not null) flow.AssignedExtension = request.AssignedExtension;

        await _flowRepository.UpdateAsync(flow, cancellationToken);
        return MapToResponse(flow);
    }

    public async Task<Result<FlowExportResponse>> ExportFlowAsync(string flowId, string tenantId, CancellationToken cancellationToken = default)
    {
        var flow = await _flowRepository.GetByIdAsync(flowId, cancellationToken);
        if (flow is null || flow.TenantId != tenantId)
            return Result.Failure<FlowExportResponse>(Error.NotFound("Flow", flowId));

        var campaigns = await _campaignRepository.GetByTenantAsync(tenantId, cancellationToken);
        var profiles = await _profileRepository.GetByTenantAsync(tenantId, cancellationToken);
        var agentsById = profiles
            .Where(p => !string.IsNullOrEmpty(p.UserId))
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        return _exporter.Export(flow, campaigns, agentsById);
    }

    private static FlowNode MapNodeFromDto(FlowNodeDto dto) {
        return new()
        {


            Id = dto.Id,
            Type = dto.Type,
            Position = new NodePosition { X = dto.X, Y = dto.Y },
            Data = new NodeData { Label = dto.Label, Config = dto.Config.HasValue? BsonDocument.Parse(dto.Config.Value.GetRawText()): new BsonDocument()
            }
        };


    }

    private static FlowEdge MapEdgeFromDto(FlowEdgeDto dto) => new()
    {
        Id = dto.Id,
        Source = dto.Source,
        SourceHandle = dto.SourceHandle,
        Target = dto.Target,
        TargetHandle = dto.TargetHandle,
        Label = dto.Label,
        Tone = dto.Tone
    };

    private static FlowResponse MapToResponse(Flow flow) => new()
    {
        Id = flow.Id,
        TenantId = flow.TenantId,
        Name = flow.Name,
        Description = flow.Description,
        Status = flow.Status,
        AssignedExtension = flow.AssignedExtension,
        Nodes = flow.Nodes.Select(n => new FlowNodeDto
        {
            Id = n.Id, Type = n.Type,
            X = n.Position.X, Y = n.Position.Y,
            Label = n.Data.Label, Config = ConvertBsonToJsonElement(n.Data.Config)
        }).ToList(),
        Edges = flow.Edges.Select(e => new FlowEdgeDto
        {
            Id = e.Id, Source = e.Source, SourceHandle = e.SourceHandle,
            Target = e.Target, TargetHandle = e.TargetHandle,
            Label = e.Label, Tone = e.Tone
        }).ToList(),
        UpdatedAt = flow.UpdatedAt,
        CreatedAt = flow.CreatedAt
    };

    private static JsonElement ConvertBsonToJsonElement(BsonDocument? bson)
    {
        using var document = JsonDocument.Parse(bson.ToJson());
        return document.RootElement.Clone();
    }
}
