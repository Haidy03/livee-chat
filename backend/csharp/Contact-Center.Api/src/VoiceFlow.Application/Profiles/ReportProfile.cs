using System.Collections.Generic;
using AutoMapper;
using VoiceFlow.Application.Interfaces.Reports;
using VoiceFlow.Contracts.Reports;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Enums.Reports;

namespace VoiceFlow.Application.Profiles;

public sealed class ReportProfile: Profile
{
    public ReportProfile()
    {
        CreateMap<BiDto, Bi>().ReverseMap();
        CreateMap<ReportFiltersDto, ReportFilters>().ReverseMap();

        CreateMap<ReportSortDto, ReportSortDefinition>()
            .ForMember(d => d.Direction, o => o.MapFrom(s => ParseEnum<SortDirection>(s.Direction, SortDirection.Desc)));
        CreateMap<ReportSortDefinition, ReportSortDto>()
            .ForMember(d => d.Direction, o => o.MapFrom(s => s.Direction.ToString().ToLowerInvariant()));

        CreateMap<ReportDefinitionDto, ReportDefinition>()
            .ForMember(d => d.Visualization, o => o.MapFrom(s => ParseEnum<VizId>(s.Visualization, VizId.Table)))
            .ForMember(d => d.Mode, o => o.MapFrom(s => ParseEnum<ReportMode>(s.Mode, ReportMode.MetricAndDimension)))
            .ForMember(d => d.Sort, o => o.MapFrom(s => s.Sort))
            .ForMember(d => d.SelectedFields, o => o.MapFrom(s => s.SelectedFields ?? new List<string>()))
            .ForMember(d => d.SchemaVersion, o => o.MapFrom(s => s.SchemaVersion <= 0 ? 2 : s.SchemaVersion));
        CreateMap<ReportDefinition, ReportDefinitionDto>()
            .ForMember(d => d.Visualization, o => o.MapFrom(s => s.Visualization.ToString().ToLowerInvariant()))
            .ForMember(d => d.Mode, o => o.MapFrom(s => s.Mode == ReportMode.Detail ? "detail" : "metricAndDimension"))
            .ForMember(d => d.Sort, o => o.MapFrom(s => s.Sort))
            .ForMember(d => d.SelectedFields, o => o.MapFrom(s => s.SelectedFields ?? new List<string>()))
            .ForMember(d => d.SchemaVersion, o => o.MapFrom(s => s.SchemaVersion <= 0 ? 1 : s.SchemaVersion));

        CreateMap<ScheduleDto, Schedule>()
            .ForMember(d => d.Frequency, o => o.MapFrom(s => ParseEnum<ScheduleFrequency>(s.Frequency, ScheduleFrequency.Daily)))
            .ForMember(d => d.Formats, o => o.MapFrom(s => s.Formats.Select(f => ParseEnum<ExportFormat>(f, ExportFormat.Pdf)).ToList()));
        CreateMap<Schedule, ScheduleDto>()
            .ForMember(d => d.Frequency, o => o.MapFrom(s => s.Frequency.ToString().ToLowerInvariant()))
            .ForMember(d => d.Formats, o => o.MapFrom(s => s.Formats.Select(f => f.ToString().ToLowerInvariant()).ToList()));

        CreateMap<CreateReportRequest, Report>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.LastRunAt, o => o.Ignore())
            .ForMember(d => d.RunsCount, o => o.Ignore())
            .ForMember(d => d.RecipientsCount, o => o.Ignore())
            .ForMember(d => d.Category, o => o.MapFrom(s => ParseEnum<ReportCategory>(s.Category, ReportCategory.Operations)))
            .ForMember(d => d.Type, o => o.MapFrom(s => ParseEnum<ReportType>(s.Type, ReportType.Operational)))
            .ForMember(d => d.Status, o => o.MapFrom(s => ParseEnum<ReportStatus>(s.Status, ReportStatus.Draft)));

        CreateMap<Report, ReportResponse>()
            .ForMember(d => d.Category, o => o.MapFrom(s => s.Category.ToString()))
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt.ToString("O")))
            .ForMember(d => d.LastRunAt, o => o.MapFrom(s => s.LastRunAt.HasValue ? s.LastRunAt.Value.ToString("O") : null));

        CreateMap<ReportRun, ReportRunResponse>()
             .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
             .ForMember(d => d.Trigger, o => o.MapFrom(s => s.Trigger.ToString()))
             .ForMember(d => d.StartedAt, o => o.MapFrom(s => s.StartedAt.ToString("O")))
             .ForMember(d => d.FinishedAt, o => o.MapFrom(s => s.FinishedAt.HasValue ? s.FinishedAt.Value.ToString("O") : null));

        CreateMap<ReportResultColumn, ReportResultColumnDto>();
        CreateMap<ReportResult, ReportResultResponse>()
            .ForMember(d => d.GeneratedAt, o => o.MapFrom(s => s.GeneratedAt.ToString("O")));
    }

    private static T ParseEnum<T>(string? value, T fallback) where T : struct, Enum =>
        !string.IsNullOrWhiteSpace(value) && Enum.TryParse<T>(value, true, out var v) ? v : fallback;
}

public sealed class ReportMapper : IReportMapper
{
    private readonly IMapper _mapper;
    public ReportMapper(IMapper mapper) => _mapper = mapper;

    public Report ToEntity(CreateReportRequest request) => _mapper.Map<Report>(request);

    public void Apply(UpdateReportRequest request, Report target)
    {
        if (request.Name is not null) target.Name = _mapper.Map<Bi>(request.Name);
        if (request.Description is not null) target.Description = _mapper.Map<Bi>(request.Description);
        if (request.Category is not null && Enum.TryParse<ReportCategory>(request.Category, true, out var c)) target.Category = c;
        if (request.Type is not null && Enum.TryParse<ReportType>(request.Type, true, out var t)) target.Type = t;
        if (request.Status is not null && Enum.TryParse<ReportStatus>(request.Status, true, out var s)) target.Status = s;
        if (request.OwnerId is not null) target.OwnerId = request.OwnerId;
        if (request.Definition is not null) target.Definition = _mapper.Map<ReportDefinition>(request.Definition);
        if (request.Schedule is not null) target.Schedule = _mapper.Map<Schedule>(request.Schedule);
        if (request.Starred is not null) target.Starred = request.Starred.Value;
    }

    public ReportResponse ToResponse(Report entity) => _mapper.Map<ReportResponse>(entity);

    public IReadOnlyList<ReportResponse> ToResponse(IEnumerable<Report> entities) =>
        entities.Select(_mapper.Map<ReportResponse>).ToList();

    public ReportRunResponse ToRunResponse(ReportRun entity) => _mapper.Map<ReportRunResponse>(entity);

    public IReadOnlyList<ReportRunResponse> ToRunResponse(IEnumerable<ReportRun> entities) =>
        entities.Select(_mapper.Map<ReportRunResponse>).ToList();

    public ReportResultResponse ToResponse(ReportResult result)
    {
        return _mapper.Map<ReportResultResponse>(result);
    }
}
