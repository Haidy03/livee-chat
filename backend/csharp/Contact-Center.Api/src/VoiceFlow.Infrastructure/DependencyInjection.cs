using CtiBackend.Services.HubSpot;
using ExportToSql.Application.Abstractions;
using ExportToSql.Infrastructure.Persistence;
using HelperLib.Messaging;
using HelperLib.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using StackExchange.Redis;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Application.Interfaces.Hubspot;
using VoiceFlow.Application.Interfaces.Messaging;
using VoiceFlow.Application.Interfaces.Reports;
using VoiceFlow.Application.Options;
using VoiceFlow.Application.Services.Hubspot;
using VoiceFlow.Application.Services.UserMap;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Core.Interfaces.Repositories.FreeSwitch;
using VoiceFlow.Core.Interfaces.Repositories.Hubspot;
using VoiceFlow.Core.Interfaces.Repositories.Reports;
using VoiceFlow.Core.Interfaces.Repositories.SkillCatalog;
using VoiceFlow.Core.Interfaces.Repositories.Surveys;
using VoiceFlow.Core.Interfaces.Repositories.WrapUpCodes;
using VoiceFlow.Core.Interfaces.Services;
using VoiceFlow.Infrastructure.Auth;
using VoiceFlow.Infrastructure.ExternalServices;
using VoiceFlow.Infrastructure.ExternalServices.Hubspot;
using VoiceFlow.Infrastructure.ExternalServices.Messaging;
using VoiceFlow.Infrastructure.Options;
using VoiceFlow.Infrastructure.Persistence;
using VoiceFlow.Infrastructure.Persistence.Repositories;
using VoiceFlow.Infrastructure.Persistence.Repositories.Hubspot;
using VoiceFlow.Infrastructure.Persistence.Repositories.Reports;
using VoiceFlow.Infrastructure.Persistence.Repositories.Surveys;
using VoiceFlow.Infrastructure.Persistence.Repositories.WrapUpCodes;
using VoiceFlow.Infrastructure.Repositories.SkillCatalog;
using VoiceFlow.Core.Interfaces.Reports;
using VoiceFlow.Infrastructure.Reports.Rendering;
using VoiceFlow.Reports.Infrastructure.Persistence.Repositories.FreeSwitch;
using VoiceFlow.Reports.Infrastructure.Repositories;
using VoiceFlow.Reports.Infrastructure.Telemetry;

namespace VoiceFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(
            configuration.GetSection(MongoDbSettings.SectionName));

        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));

        services.AddSingleton<MongoDbContext>();
        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            var connectionString = settings.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = configuration.GetSection(MongoDbSettings.SectionName)[nameof(MongoDbSettings.ConnectionString)];

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("MongoDB:ConnectionString is not configured.");

            return new MongoClient(connectionString);
        });

        services.AddSingleton<CollectionBootstrap>();

        // Auth
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services
            .AddOptions<EmailOptions>()
            .BindConfiguration(EmailOptions.SectionName)
            .Validate(o => !o.Enabled || (!string.IsNullOrWhiteSpace(o.Host) && !string.IsNullOrWhiteSpace(o.FromAddress) && !string.IsNullOrWhiteSpace(o.FrontendBaseUrl)),
                "Email is enabled but Host, FromAddress, or FrontendBaseUrl is missing.")
            .Validate(o => !o.Enabled || !string.IsNullOrWhiteSpace(o.Password) || o.Host.Contains("smtp-relay", StringComparison.OrdinalIgnoreCase),
                "Email is enabled but Username/Password are missing (only smtp-relay.gmail.com with IP allowlisting may omit credentials).")
            .ValidateOnStart();
        services.AddScoped<IEmailService, SmtpEmailService>();

        // Digital-workspace email channel (agent inbox): IMAP ingest + SMTP replies.
        services.AddScoped<IEmailThreadRepository, EmailThreadRepository>();
        services.AddScoped<IEmailMessageRepository, EmailMessageRepository>();
        services.AddScoped<IEmailAgentSettingsRepository, EmailAgentSettingsRepository>();
        services.AddScoped<IEmailChannelSender, SmtpEmailChannelSender>();
        services.AddScoped<IEmailAttachmentFetcher, ImapEmailAttachmentFetcher>();
        services.AddHostedService<EmailInboundWorker>();
        services.AddSingleton<IRefreshTokenStore, RefreshTokenStore>();
        services.AddSingleton<IWebHostEnvironmentAccessor, WebHostEnvironmentAccessor>();

        // External services
        services.AddScoped<IStorageService, AzureBlobStorageService>();
        services.AddScoped<IAiGatewayService, AiGatewayService>();
        services.AddScoped<ITtsService, TtsService>();

        // Repositories
        services.AddScoped<IAuthUserRepository, AuthUserRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IBillingRepository, BillingRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IRbacRoleRepository, RbacRoleRepository>();
        services.AddScoped<IRbacUserRoleRepository, RbacUserRoleRepository>();
        services.AddScoped<ICallRepository, CallRepository>();
        services.AddScoped<IVoicemailRepository, VoicemailRepository>();
        services.AddScoped<IFlowRepository, FlowRepository>();
        services.AddScoped<IVoiceLibraryRepository, VoiceLibraryRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IAutoTagRepository, AutoTagRepository>();
        services.AddScoped<ISipAccountRepository, SipAccountRepository>();
        services.AddScoped<ISoftphoneCallLogRepository, SoftphoneCallLogRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IEditLogRepository, EditLogRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IQueueRepository, QueueRepository>();
        services.AddScoped<ILiveCallRecordRepository, LiveCallRecordRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<ICampaignTargetRepository, CampaignTargetRepository>();
        services.AddScoped<ICampaignActivityRepository, CampaignActivityRepository>();
        services.AddScoped<ICampaignReceivedCallRepository, CampaignReceivedCallRepository>();

        services
            .AddOptions<CallPublisherOptions>()
            .BindConfiguration(CallPublisherOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
          .AddOptions<ReportPublisherOptions>()
          .BindConfiguration(ReportPublisherOptions.SectionName)
          .ValidateDataAnnotations()
          .ValidateOnStart();

        
        services.AddSingleton<CallPublisher>();
        services.AddSingleton<ICallPublisher>(sp => sp.GetRequiredService<CallPublisher>());
        services.AddSingleton<ReportPublisher>();
        services.AddSingleton<IReportPublisher>(sp => sp.GetRequiredService<ReportPublisher>());

        services.AddSingleton<IMessageBus>(sp => sp.GetRequiredService<CallPublisher>());

        services
            .AddOptions<MariaDbOptions>()
            .BindConfiguration(MariaDbOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<ISqlScriptExecutor, MySqlScriptExecutor>();
        services.AddScoped<IVoiceLibraryObjectStorage, S3VoiceLibraryObjectStorage>();


        services.AddMemoryCache();
        services.AddSurveysInfrastructure();
        services.AddReportsInfrastructure();

        services.AddScoped<IDialplanDocumentRepository, DialplanDocumentRepository>();
        services.AddRedisConfig(configuration);

        services.AddScoped<ISkillCatalogRepository, SkillCatalogRepository>();
        services.AddScoped<IWrapUpCodeRepository, WrapUpCodeRepository>();
        services.AddScoped<IQueueWrapUpCodeRepository, QueueWrapUpCodeRepository>();



        services.AddHubspotServices(configuration);
        services.AddQueueMonitoringReadServices();
        return services;
    }

    private static IServiceCollection AddQueueMonitoringReadServices(this IServiceCollection services)
    {
        services.AddSingleton<VoiceFlow.Infrastructure.ExternalServices.QueueMonitoring.QueueMonitoringKeys>(sp =>
            new VoiceFlow.Infrastructure.ExternalServices.QueueMonitoring.QueueMonitoringKeys(
                sp.GetRequiredService<IOptions<VoiceFlow.Application.Options.QueueMonitoringOptions>>().Value));
        services.AddScoped<VoiceFlow.Application.Interfaces.QueueMonitoring.IQueueMonitoringReadRepository,
            VoiceFlow.Infrastructure.ExternalServices.QueueMonitoring.RedisQueueMonitoringReadRepository>();
        return services;
    }

    private static IServiceCollection AddRedisConfig(this IServiceCollection services , IConfiguration config)
    {
        // ----- Users Map (live caller tracking) -----
        services.Configure<UsersMapOptions>(config.GetSection("UsersMap"));
        services.Configure<CallTTlOptions>(config.GetSection("CallTTlOptions"));

        var source = config.GetValue<string>("UsersMap:Source") ?? "simulator";
        if (string.Equals(source, "redis", StringComparison.OrdinalIgnoreCase))
        {
           /* var redisCs = config.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisCs));*/
            services.AddScoped<ILiveCallRegistry, RedisLiveCallRegistry>();
            services.AddSingleton<ICallControlClient, NoopCallControlClient>();
            services.AddScoped<IUsersMapService, UsersMapService>();
        }
        else
        {
            services.AddScoped<IUsersMapService, SimulatorUsersMapService>();
        }

        return services;
    }

    private static IServiceCollection AddSurveysInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ISurveyRepository, SurveyRepository>();
        services.AddScoped<ISurveyResponseRepository, SurveyResponseRepository>();
        return services;
    }

    private static IServiceCollection AddReportsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportRunRepository, ReportRunRepository>();
        services.AddScoped<IRbacAuthorizer, RbacAuthorizer>();
        services.AddScoped<IReportResultRepository, ReportResultRepository>();

        // Report export rendering (shared by the API download path and the scheduled worker).
        services.AddSingleton<ReportHtmlBuilder>();
        services.AddSingleton<ChromiumPageRenderer>();
        services.AddSingleton<IReportFormatRenderer, CsvReportRenderer>();
        services.AddSingleton<IReportFormatRenderer, XlsxReportRenderer>();
        services.AddSingleton<IReportFormatRenderer, HtmlReportRenderer>();
        services.AddSingleton<IReportFormatRenderer, PdfReportRenderer>();
        services.AddSingleton<IReportRenderer, ReportRenderer>();
        return services;
    }

    private static IServiceCollection AddHubspotServices(this IServiceCollection services , IConfiguration configuration)
    {

        services.Configure<HubSpotOptions>(configuration.GetSection(HubSpotOptions.SectionName));
        services.AddSingleton<IValidateOptions<HubSpotOptions>, HubSpotOptionsValidator>();

        // ---- HubSpot integration ---------------------------------------------------
        services.AddSingleton<IHubSpotIntegrationRepository, MongoHubSpotIntegrationRepository>();
        services.AddSingleton<IRefreshLock, RedisRefreshLock>();
        services.AddSingleton<ITokenProtector, DataProtectionTokenProtector>();
        services.AddScoped<IHubSpotOAuthService, HubSpotOAuthService>();
        services.AddScoped<IHubSpotTokenProvider, HubSpotTokenProvider>();
        services.AddScoped<HubSpotApiClient>();
        services.AddHttpClient("hubspot", c => c.Timeout = TimeSpan.FromSeconds(30));


        // Data Protection keys persisted in MongoDB so encrypted tokens survive
        // restarts and span multiple backend instances.
        services.AddDataProtection()
            .SetApplicationName("CtiBackend.HubSpot")
            .AddKeyManagementOptions(o =>
            {
                // Resolve once at startup; provider holds singletons.
                var sp = services.BuildServiceProvider();
                var mongoClient = sp.GetRequiredService<IMongoClient>();
                var mongoOpts = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
                var hsOpts = sp.GetRequiredService<IOptions<HubSpotOptions>>().Value;
                var col = mongoClient.GetDatabase(mongoOpts.DatabaseName)
                                     .GetCollection<BsonDocument>(hsOpts.MongoDataProtectionCollection);
                o.XmlRepository = new MongoXmlRepository(col);
            });


        return services;
    }
}
