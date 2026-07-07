using FluentValidation;
using System.Text;
using System.Text.RegularExpressions;
using VoiceFlow.Core.Interfaces.Reports;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Helpers;
using VoiceFlow.Application.Interfaces.Messaging;
using VoiceFlow.Application.Interfaces.Reports;
using VoiceFlow.Core.Reports.Catalog;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Events;
using VoiceFlow.Contracts.Reports;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Constatnts.RoutingKeys;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Enums.Reports;
using VoiceFlow.Core.Exceptions.Reports;
using VoiceFlow.Core.Interfaces.Repositories.Reports;
namespace VoiceFlow.Application.Services.Reports;

public sealed class ReportService : IReportService
{
    private readonly IReportRepository _repo;
    private readonly IReportRunRepository _runRepo;
    private readonly IReportResultRepository _results;
    private readonly IReportPublisher _reportPublisher;
    private readonly IRbacAuthorizer _rbac;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IReportMapper _mapper;
    private readonly IValidator<CreateReportRequest> _createValidator;
    private readonly IValidator<UpdateReportRequest> _updateValidator;
    private readonly IValidator<BulkStatusRequest> _bulkValidator;
    private readonly IReportRenderer _renderer;

    public ReportService(
        IReportRepository repo,
        IReportRunRepository runRepo,
        IRbacAuthorizer rbac,
        ITenantContext tenant,
        ICurrentUser user,
        IClock clock,
        IReportResultRepository results,
        IReportMapper mapper,
        IReportPublisher reportPublisher,
        IReportRenderer renderer,
        IValidator<CreateReportRequest> createValidator,
        IValidator<UpdateReportRequest> updateValidator,
        IValidator<BulkStatusRequest> bulkValidator)
    {
        _repo = repo; _runRepo = runRepo; _rbac = rbac; _tenant = tenant; _user = user; _clock = clock;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _bulkValidator = bulkValidator;
        _results = results;
        _reportPublisher = reportPublisher;
        _renderer = renderer;
    }

    private async Task<Error?> RequireAsync(ReportsAction action, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_tenant.TenantId))
            return Error.Forbidden("tenant_missing");
        var ok = await _rbac.CanAsync(_tenant.TenantId, _user.UserId, action, ct);
        return ok ? null : Error.Forbidden(action.ToString().ToLowerInvariant());
    }

    public async Task<Result<PagedResponse<ReportResponse>>> ListAsync(string? search, string? category, string? status, bool? starred, string? ownerId, int page, int pageSize, string? sort, CancellationToken ct)
    {
        var err = await RequireAsync(ReportsAction.View, ct);
        if (err is not null) return Result.Failure<PagedResponse<ReportResponse>>(err);

        ReportCategory? cat = null;
        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<ReportCategory>(category, true, out var c)) cat = c;
        ReportStatus? st = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReportStatus>(status, true, out var s)) st = s;

        page = page <= 0 ? 1 : page;
        pageSize = pageSize is <= 0 or > 200 ? 50 : pageSize;

        var paged = await _repo.ListAsync(_tenant.TenantId,
            new ReportListQuery(search, cat, st, starred, ownerId, page, pageSize, sort), ct);

        return Result.Success(new PagedResponse<ReportResponse>
        {
            Items = _mapper.ToResponse(paged.Items),
            TotalCount = paged.Total,
            Page = paged.Page,
            PageSize = paged.PageSize,
        });
    }

    public async Task<Result<ReportResponse>> GetAsync(string id, CancellationToken ct)
    {
        var err = await RequireAsync(ReportsAction.View, ct);
        if (err is not null) return Result.Failure<ReportResponse>(err);

        var found = await _repo.GetAsync(_tenant.TenantId, id, ct);
        return found is null
            ? Result.Failure<ReportResponse>(Error.NotFound("report",id))
            : Result.Success(_mapper.ToResponse(found));
    }

    public async Task<Result<ReportResponse>> CreateAsync(CreateReportRequest request, CancellationToken ct)
    {
        var err = await RequireAsync(ReportsAction.Create, ct);
        if (err is not null) return Result.Failure<ReportResponse>(err);

        var v = await _createValidator.ValidateAsync(request, ct);
        if (!v.IsValid)
            return Result.Failure<ReportResponse>(Error.Validation("report.invalid", string.Join("; ", v.Errors.Select(e => e.ErrorMessage))));

        var entity = _mapper.ToEntity(request);
        if (string.IsNullOrWhiteSpace(_user.UserId))
            return Result.Failure<ReportResponse>(Error.Forbidden("owner_missing"));
        entity.OwnerId = _user.UserId;
        entity.TenantId = _tenant.TenantId;
        entity.CreatedAt = _clock.UtcNow.Date;
        entity.UpdatedAt = _clock.UtcNow.Date;
        entity.RecipientsCount = entity.Schedule.Recipients.Count;
        entity.NextRunAt = ReportScheduleCalculator.CalculateNextRun(entity.Schedule, _clock.UtcNow);

        var (ok, error) = ReportDefinitionNormalizer.Normalize(entity.Definition);
        if (!ok)
            return Result.Failure<ReportResponse>(Error.Validation("report.invalid", error ?? "invalid_definition"));

        await _repo.AddAsync(entity, ct);
        return Result.Success(_mapper.ToResponse(entity));
    }

    public async Task<Result<ReportResponse>> UpdateAsync(string id, UpdateReportRequest request, CancellationToken ct)
    {
        var err = await RequireAsync(ReportsAction.Edit, ct);
        if (err is not null) return Result.Failure<ReportResponse>(err);

        var v = await _updateValidator.ValidateAsync(request, ct);
        if (!v.IsValid)
            return Result.Failure<ReportResponse>(Error.Validation("report.invalid", string.Join("; ", v.Errors.Select(e => e.ErrorMessage))));

        var existing = await _repo.GetAsync(_tenant.TenantId, id, ct);
        if (existing is null) return Result.Failure<ReportResponse>(Error.NotFound("report",id));

        _mapper.Apply(request, existing);
        existing.UpdatedAt = _clock.UtcNow.Date;
        existing.RecipientsCount = existing.Schedule.Recipients.Count;

        var (nOk, nErr) = ReportDefinitionNormalizer.Normalize(existing.Definition);
        if (!nOk)
            return Result.Failure<ReportResponse>(Error.Validation("report.invalid", nErr ?? "invalid_definition"));

        var ok = await _repo.UpdateReportAsync(existing, ct);
        return ok
            ? Result.Success(_mapper.ToResponse(existing))
            : Result.Failure<ReportResponse>(Error.NotFound("report",id));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct)
    {
        var err = await RequireAsync(ReportsAction.Delete, ct);
        if (err is not null) return Result.Failure<bool>(err);

        var ok = await _repo.DeleteAsync(_tenant.TenantId, id, ct);
        return ok ? Result.Success(true) : Result.Failure<bool>(Error.NotFound("report",id));
    }

    public async Task<Result<IReadOnlyList<ReportResponse>>> BulkSetStatusAsync(BulkStatusRequest request, CancellationToken ct)
    {
        var err = await RequireAsync(ReportsAction.Edit, ct);
        if (err is not null) return Result.Failure<IReadOnlyList<ReportResponse>>(err);

        var v = await _bulkValidator.ValidateAsync(request, ct);
        if (!v.IsValid)
            return Result.Failure<IReadOnlyList<ReportResponse>>(Error.Validation("report.invalid", string.Join("; ", v.Errors.Select(e => e.ErrorMessage))));

        if (!Enum.TryParse<ReportStatus>(request.Status, true, out var status))
            return Result.Failure<IReadOnlyList<ReportResponse>>(Error.Validation("report.bad_status", $"Unknown status '{request.Status}'."));

        var updated = await _repo.BulkSetStatusAsync(_tenant.TenantId, request.Ids, status, DateTime.UtcNow, ct);
        return Result.Success(_mapper.ToResponse(updated));
    }

    public async Task<Result<ReportResponse>> RunAsync(string id, CancellationToken ct)
    {
        var err = await RequireAsync(ReportsAction.Export, ct);
        if (err is not null) return Result.Failure<ReportResponse>(err);

        var existing = await _repo.GetAsync(_tenant.TenantId, id, ct);
        if (existing is null) return Result.Failure<ReportResponse>(Error.NotFound("report.not_found", id));

        var now = _clock.UtcNow;

        // 1. Create the ReportRun row in Queued state so it shows immediately in history.
        var run = new ReportRun
        {
            TenantId = _tenant.TenantId,
            ReportId = existing.Id,
            StartedAt = now,
            Status = ReportRunStatus.Queued,
            TriggeredBy = _user.UserId,
            Trigger = ReportRunTrigger.Manual,
        };
        await _runRepo.AddAsync(run, ct);

        // 2. Hand off execution to the worker via RabbitMQ.
        await _reportPublisher.PublishAsync(new ReportRunRequested
        {
            RunId = run.Id,
            ReportId = existing.Id,
            TenantId = _tenant.TenantId,
            TriggeredBy = _user.UserId,
            Trigger = ReportRunTrigger.Manual,
            RequestedAt = now,
            CorrelationId = Guid.NewGuid().ToString("N"),
        },ReportRoutingKeys.Report, ct);

        // 3. Mark on the report itself.
        existing.LastRunAt = now;
        existing.RunsCount += 1;
        existing.UpdatedAt = now.DateTime;
        await _repo.UpdateReportAsync(existing, ct);

        return Result.Success(_mapper.ToResponse(existing));
    }

    public async Task<Result<PagedResponse<ReportRunResponse>>> ListRunsAsync(string reportId, int page, int pageSize, CancellationToken ct)
    {
        var err = await RequireAsync(ReportsAction.View, ct);
        if (err is not null) return Result.Failure<PagedResponse<ReportRunResponse>>(err);

        var report = await _repo.GetAsync(_tenant.TenantId, reportId, ct);
        if (report is null) return Result.Failure<PagedResponse<ReportRunResponse>>(Error.NotFound("report.not_found", reportId));

        page = page <= 0 ? 1 : page;
        pageSize = pageSize is <= 0 or > 200 ? 20 : pageSize;

        var paged = await _runRepo.ListAsync(_tenant.TenantId, reportId, page, pageSize, ct);
        return Result.Success(new PagedResponse<ReportRunResponse>
        {
            Items = _mapper.ToRunResponse(paged.Items),
            TotalCount = paged.Total,
            Page = paged.Page,
            PageSize = paged.PageSize,
        });
    }

    public async Task<Result<ReportResultResponse>> GetRunResultAsync(string reportId, string runId, CancellationToken ct)
    {
        var err = await RequireAsync(ReportsAction.View, ct);
        if (err is not null) return Result.Failure<ReportResultResponse>(err);

        var report = await _repo.GetAsync(_tenant.TenantId, reportId, ct);
        if (report is null) return Result.Failure<ReportResultResponse>(Error.NotFound("report.not_found", reportId));

        var result = await _results.GetByRunIdAsync(_tenant.TenantId, runId, ct);
        if (result is null || result.ReportId != reportId)
            return Result.Failure<ReportResultResponse>(Error.NotFound("report.not_found", reportId));

        return Result.Success(_mapper.ToResponse(result));
    }

    public async Task<Result<ReportResultResponse>> GetLatestResultAsync(string reportId, CancellationToken ct)
    {
        var err = await RequireAsync(ReportsAction.View, ct);
        if (err is not null) return Result.Failure<ReportResultResponse>(err);

        var report = await _repo.GetAsync(_tenant.TenantId, reportId, ct);
        if (report is null) return Result.Failure<ReportResultResponse>(Error.NotFound("report.not_found", reportId));

        var result = await _results.GetLatestForReportAsync(_tenant.TenantId, reportId, ct);
        if (result is null) return Result.Failure<ReportResultResponse>(Error.NotFound("report.not_found", reportId));

        return Result.Success(_mapper.ToResponse(result));
    }

    public async Task<Result<RenderedReport>> ExportRunResultAsync(string reportId, string runId, ExportFormat format, string lang, CancellationToken ct)
    {
        var err = await RequireAsync(ReportsAction.View, ct);
        if (err is not null) return Result.Failure<RenderedReport>(err);

        if (!_renderer.CanRender(format))
            return Result.Failure<RenderedReport>(
                Error.Validation("report.export.format", $"Export format '{format}' is not supported yet."));

        var report = await _repo.GetAsync(_tenant.TenantId, reportId, ct);
        if (report is null) return Result.Failure<RenderedReport>(Error.NotFound("report.not_found", reportId));

        var result = await _results.GetByRunIdAsync(_tenant.TenantId, runId, ct);
        if (result is null || result.ReportId != reportId)
            return Result.Failure<RenderedReport>(Error.NotFound("report.not_found", reportId));

        var normLang = lang == "ar" ? "ar" : "en";
        var options = new ReportRenderOptions
        {
            FileStem = BuildFileStem(report),
            Title = ReportTitle(report, normLang),
            Viz = ResolveViz(report),
            Lang = normLang,
            GeneratedAt = result.GeneratedAt.ToString("o"),
            RowCount = result.RowCount,
        };
        var rendered = await _renderer.RenderAsync(result, format, options, ct);
        return Result.Success(rendered);
    }

    /// <summary>The visualization the frontend would show: detail reports are always tabular,
    /// otherwise the report's chosen viz. Lowercased to match the frontend VizId strings.</summary>
    private static string ResolveViz(Report report) =>
        report.Definition.Mode == ReportMode.Detail
            ? "table"
            : report.Definition.Visualization.ToString().ToLowerInvariant();

    private static string ReportTitle(Report report, string lang)
    {
        var name = lang == "ar"
            ? (!string.IsNullOrWhiteSpace(report.Name.Ar) ? report.Name.Ar : report.Name.En)
            : (!string.IsNullOrWhiteSpace(report.Name.En) ? report.Name.En : report.Name.Ar);
        return string.IsNullOrWhiteSpace(name) ? "Report" : name;
    }

    /// <summary>Turn a report name into a safe, dated file stem — mirrors the frontend safeStem().</summary>
    private static string BuildFileStem(Report report)
    {
        var name = !string.IsNullOrWhiteSpace(report.Name.En) ? report.Name.En
                 : !string.IsNullOrWhiteSpace(report.Name.Ar) ? report.Name.Ar
                 : "report";

        var cleaned = Regex.Replace(name.Trim(), "[^a-zA-Z0-9-_]+", "-").Trim('-');
        if (cleaned.Length == 0) cleaned = "report";

        return $"{cleaned}-{DateTime.UtcNow:yyyy-MM-dd}";
    }

    public Result<IReadOnlyList<DataSourceMetadataResponse>> ListDataSources()
    {
        var list = ReportDataSourceCatalog.All.Select(BuildMetadata).ToList();
        return Result.Success<IReadOnlyList<DataSourceMetadataResponse>>(list);
    }

    public Result<DataSourceMetadataResponse> GetDataSourceMetadata(string key)
    {
        var s = ReportDataSourceCatalog.Find(key);
        if (s is null) return Result.Failure<DataSourceMetadataResponse>(Error.NotFound("data_source", key));
        return Result.Success(BuildMetadata(s));
    }

    private static DataSourceMetadataResponse BuildMetadata(ReportDataSourceDefinition s)
    {
        // A delegating source (e.g. agents → calls) keeps its own DETAIL fields but takes its
        // dimensions and metrics from the delegate, since metric mode aggregates that collection.
        var metricSource = string.IsNullOrWhiteSpace(s.MetricDelegateKey)
            ? s
            : ReportDataSourceCatalog.Find(s.MetricDelegateKey) ?? s;
        var delegating = !ReferenceEquals(metricSource, s);

        // (field, usableInDetail, usableAsDimension) — when delegating, detail comes from self only
        // and dimensions from the delegate only; otherwise the field's own flags stand.
        var entries = delegating
            ? s.Fields.Select(f => (f, detail: f.CanUseInDetail, dim: false))
                .Concat(metricSource.Fields.Where(f => f.CanUseAsDimension).Select(f => (f, detail: false, dim: true)))
            : s.Fields.Select(f => (f, detail: f.CanUseInDetail, dim: f.CanUseAsDimension));

        // Collapse alias fields (agent/agentName/agentId all resolve to one dimension) so the
        // picker shows a single canonical entry; OR the detail/dimension capability across aliases.
        var fields = entries
            .GroupBy(e => e.f.ResolvedKey, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var rep = (g.FirstOrDefault(e => string.Equals(e.f.Key, e.f.ResolvedKey, StringComparison.OrdinalIgnoreCase)).f) ?? g.First().f;
                return new DataSourceFieldDto
                {
                    Key = rep.Key,
                    Label = new BiDto { En = rep.LabelEn, Ar = rep.LabelAr },
                    DataType = rep.DataType,
                    CanUseInDetail = g.Any(e => e.detail),
                    CanUseAsDimension = g.Any(e => e.dim),
                    CanFilter = rep.CanFilter,
                    CanSort = rep.CanSort,
                };
            }).ToList();

        return new DataSourceMetadataResponse
        {
            DataSource = s.Key,
            Label = new BiDto { En = s.LabelEn, Ar = s.LabelAr },
            Description = new BiDto { En = s.DescriptionEn, Ar = s.DescriptionAr },
            Icon = s.Icon,
            Ready = s.Ready,
            SupportedModes = new List<string> { "detail", "metricAndDimension" },
            Fields = fields,
            Metrics = metricSource.Metrics.Select(m => new DataSourceMetricDto
            {
                Key = m.Key,
                Label = new BiDto { En = m.LabelEn, Ar = m.LabelAr },
                DataType = m.DataType,
                Kind = m.Kind.ToString(),
            }).ToList(),
        };
    }
}
