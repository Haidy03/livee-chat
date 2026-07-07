using CTI.Options;
using CtiBackend.Integrations.HubSpot.Services;
using CtiBackend.Integrations.HubSpot;
using CtiBackend.Middleware;
using CtiBackend.Options;
using CtiBackend.Services.Ami;
using CtiBackend.Services.CallerInfo;
using CtiBackend.Services.Events;
using CtiBackend.Services.Health;
using CtiBackend.Services.HubSpot;
using CtiBackend.Services.QueueMonitoring;
using CtiBackend.Services.Security;
using CtiBackend.Services.State;
using CtiBackend.Services.State.UsersMap;
using CtiBackend.Tenant;
using HelperLib;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using VoiceFlow.Repos;

var builder = WebApplication.CreateBuilder(args);

// ---- Options ---------------------------------------------------------------
builder.Services.Configure<AmiOptions>(builder.Configuration.GetSection(AmiOptions.SectionName));
builder.Services.Configure<CallerInfoOptions>(builder.Configuration.GetSection(CallerInfoOptions.SectionName));
builder.Services.Configure<ApiSecurityOptions>(builder.Configuration.GetSection(ApiSecurityOptions.SectionName));
builder.Services.Configure<RawEventStoreOptions>(builder.Configuration.GetSection(RawEventStoreOptions.SectionName));
builder.Services.Configure<SessionRetentionOptions>(builder.Configuration.GetSection(SessionRetentionOptions.SectionName));

builder.Services.Configure<CallTTlOptions>(builder.Configuration.GetSection(CallTTlOptions.SectionName));
builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection(MongoOptions.SectionName));
builder.Services.Configure<HubSpotOptions>(builder.Configuration.GetSection(HubSpotOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<HubSpotOptions>, HubSpotOptionsValidator>();
builder.Services.Configure<QueueMonitoringOptions>(builder.Configuration.GetSection(QueueMonitoringOptions.SectionName));

// ---- HTTP client factory (for future CRM integration) ---------------------
builder.Services.AddHttpClient();

// ---- Core services ---------------------------------------------------------
builder.Services.AddSingleton<AmiConnectionStatus>();
builder.Services.AddSingleton<IAmiMessageParser, AmiMessageParser>();
builder.Services.AddSingleton<IAmiRawEventStore, InMemoryAmiRawEventStore>();
builder.Services.AddSingleton<ICallSessionStateManager, CallSessionStateManager>();
builder.Services.AddSingleton<ICallerInfoResolver, CallerInfoResolver>();
builder.Services.AddSingleton<IAccountRepository, AccountRepository>();

// ---- Redis (StackExchange.Redis) ------------------------------------------
// AbortOnConnectFail=false so the service starts even if Redis is down; the
// first Redis call will throw and be logged by the dispatcher, matching the
// behaviour of ReportsService.
/*builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisOptions>>().Value;
    var cfg = ConfigurationOptions.Parse(opts.ConnectionString);
    cfg.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(cfg);
});*/

// ---- MongoDB ---------------------------------------------------------------
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoOptions>>().Value;
    return new MongoClient(opts.ConnectionString);
});

// UsersMap (mirrors ReportsService: Redis-backed live registry + Mongo-backed call records)
builder.Services.AddSingleton<ILiveCallRegistry, RedisLiveCallRegistry>();
builder.Services.AddSingleton<ICallRecordRepository, MongoCallRecordRepository>();
builder.Services.AddSingleton<UsersMapStateService>();
builder.Services.Configure<CtiBackend.Options.UsersMapOptions>(builder.Configuration.GetSection("UsersMap"));
builder.Services.AddSingleton<UsersMapSnapshotService>();

// ---- Startup health probe (Redis + MongoDB + LiveCall round-trip) ---------
builder.Services.AddSingleton<IBackendHealthState, BackendHealthState>();
builder.Services.AddSingleton<BackendStartupHealthCheck>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<BackendStartupHealthCheck>());

// Dispatcher is both an interface (for the listener) and a BackgroundService
// (its drain loop). Register it once and resolve via both shapes.
builder.Services.AddSingleton<AmiEventDispatcher>();
builder.Services.AddSingleton<IAmiEventDispatcher>(sp => sp.GetRequiredService<AmiEventDispatcher>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<AmiEventDispatcher>());

// AMI listener
builder.Services.AddSingleton<AmiActionSender>();
builder.Services.AddSingleton<IAmiActionSender>(sp => sp.GetRequiredService<AmiActionSender>());
builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<IOptions<QueueMonitoringOptions>>().Value;
    return new AmiConnectionContext
    {
        TenantId = opts.TenantId,
        ServerId = opts.ServerId,
        ConnectionName = "default",
    };
});
builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<IOptions<QueueMonitoringOptions>>().Value;
    return new QueueMonitoringKeys(opts);
});
builder.Services.AddSingleton<IAgentIdentityNormalizer, AgentIdentityNormalizer>();
builder.Services.AddSingleton<IQueueMonitoringRedisRepository, QueueMonitoringRedisRepository>();
builder.Services.AddSingleton<IQueueSnapshotService, QueueSnapshotService>();
builder.Services.AddSingleton<IQueueMonitoringEventHandler, QueueMonitoringEventHandler>();
builder.Services.AddSingleton<IQueueStateQueryService, QueueStateQueryService>();
builder.Services.AddHostedService<AmiListenerHostedService>();

// ---- HubSpot integration ---------------------------------------------------
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddSingleton<ITenantAdminPolicy, DefaultTenantAdminPolicy>();
builder.Services.AddSingleton<IHubSpotIntegrationRepository, MongoHubSpotIntegrationRepository>();
builder.Services.AddSingleton<IRefreshLock, RedisRefreshLock>();
builder.Services.AddSingleton<ITokenProtector, DataProtectionTokenProtector>();
builder.Services.AddScoped<IHubSpotOAuthService, HubSpotOAuthService>();
builder.Services.AddScoped<IHubSpotTokenProvider, HubSpotTokenProvider>();
builder.Services.AddScoped<HubSpotApiClient>();
builder.Services.AddHttpClient("hubspot", c => c.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddHostedService<HubSpotStateCleanupHostedService>();

// HubSpot caller lookup
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<HubSpotLookupCache>();
builder.Services.AddSingleton<IPhoneNumberNormalizer, PhoneNumberNormalizer>();
builder.Services.AddScoped<IHubSpotContactSearchClient, HubSpotContactSearchClient>();
builder.Services.AddScoped<IHubSpotCallerLookupService, HubSpotCallerLookupService>();


// Light CRM contact directory (Mongo-backed)
builder.Services.AddSingleton<CtiBackend.Services.Directory.IContactDirectoryService,
    CtiBackend.Services.Directory.ContactDirectoryService>();

// Data Protection keys persisted in MongoDB so encrypted tokens survive
// restarts and span multiple backend instances.
builder.Services.AddDataProtection()
    .SetApplicationName("CtiBackend.HubSpot")
    .AddKeyManagementOptions(o =>
    {
        // Resolve once at startup; provider holds singletons.
        var sp = builder.Services.BuildServiceProvider();
        var mongoClient = sp.GetRequiredService<IMongoClient>();
        var mongoOpts = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
        var hsOpts = sp.GetRequiredService<IOptions<HubSpotOptions>>().Value;
        var col = mongoClient.GetDatabase(mongoOpts.Database)
                             .GetCollection<BsonDocument>(hsOpts.MongoDataProtectionCollection);
        o.XmlRepository = new MongoXmlRepository(col);
    });


// ---- Web stack -------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "CtiBackend", Version = "v1" });
});

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                "http://localhost:8080",
                "http://localhost:5173",
                "http://localhost:5151",
                "https://localhost:7180",
                "https://*.lovable.app",
                "https://*.lovableproject.com")
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));


builder.Services.AddHelperServices(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

app.UseMiddleware<ApiKeyMiddleware>();
app.UseMiddleware<TenantContextMiddleware>();


app.UseCors("AllowAll");

app.MapControllers();

// Ensure HubSpot Mongo indexes exist at startup.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var repo = scope.ServiceProvider.GetRequiredService<IHubSpotIntegrationRepository>();
        await repo.EnsureIndexesAsync(default);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Failed to ensure HubSpot indexes at startup");
    }
}

// Queue monitoring: trigger a full snapshot whenever AMI connects/reconnects.
{
    var status = app.Services.GetRequiredService<AmiConnectionStatus>();
    var snapshot = app.Services.GetRequiredService<IQueueSnapshotService>();
    var amiCtx = app.Services.GetRequiredService<AmiConnectionContext>();
    var qmRepo = app.Services.GetRequiredService<IQueueMonitoringRedisRepository>();
    status.ConnectedEvent += () =>
    {
        _ = Task.Run(async () =>
        {
            try
            {
                // Give the listener a moment to attach the writer.
                await Task.Delay(500);
                await snapshot.RequestFullSnapshotAsync(amiCtx, CancellationToken.None);
            }
            catch (Exception ex) { app.Logger.LogError(ex, "Snapshot trigger failed"); }
        });
    };
    status.DisconnectedEvent += err =>
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await qmRepo.UpdateAmiStatusAsync(new CtiBackend.Services.QueueMonitoring.Models.AmiServerStatus
                {
                    TenantId = amiCtx.TenantId,
                    ServerId = amiCtx.ServerId,
                    Connected = false,
                    ConnectionStatus = "Disconnected",
                    LastDisconnectedUtc = DateTime.UtcNow,
                    SnapshotStatus = "Stale",
                    IsStateStale = true,
                    LastError = err,
                }, CancellationToken.None);
            }
            catch (Exception ex) { app.Logger.LogDebug(ex, "AMI stale-state update failed"); }
        });
    };
}

app.Run();
