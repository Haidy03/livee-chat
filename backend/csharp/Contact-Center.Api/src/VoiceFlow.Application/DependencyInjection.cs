using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoiceFlow.Application.Interfaces.FreeSwitch;
using VoiceFlow.Application.Interfaces.QueueMonitoring;
using VoiceFlow.Application.Interfaces.Reports;
using VoiceFlow.Application.Interfaces.SkillCatalog;
using VoiceFlow.Application.Interfaces.WrapUpCodes;
using VoiceFlow.Application.Options;
using VoiceFlow.Application.Profiles;
using VoiceFlow.Application.Services.QueueMonitoring;
using VoiceFlow.Application.Services.Reports;
using VoiceFlow.Application.Services.SkillCatalog;
using VoiceFlow.Application.Services.WrapUpCodes;
using VoiceFlow.Application.Services.Visitors;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Core.Interfaces.Repositories.SkillCatalog;
using VoiceFlow.Reports.Application.Services.FreeSwitch;
namespace VoiceFlow.Application.dependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddVoiceFlowApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAutoMapper(cfg => cfg.AddMaps(typeof(ReportMapper).Assembly));
            services.AddScoped<IDialplanDocumentService, DialplanDocumentService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddSingleton<IReportMapper, ReportMapper>();
            services.AddScoped<ISkillCatalogService, SkillCatalogService>();
            services.AddScoped<IWrapUpCodeService, WrapUpCodeService>();

            services.Configure<QueueMonitoringOptions>(
                configuration.GetSection(QueueMonitoringOptions.SectionName));
            services.AddScoped<IQueueStateQueryService, QueueStateQueryService>();
            services.AddScoped<IVisitorsService, VisitorsService>();

            return services;
        }
    }

}
