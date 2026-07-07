using ExportToSql.Application.Abstractions;
using ExportToSql.Application.Services;
using FluentValidation;
using HelperLib;
using HelperLib.Services;
using HelperLib.SharedMiddlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using VoiceFlow.Api.Middleware;
using VoiceFlow.Api.Services;
using VoiceFlow.Application;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Exporters;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Application.Interfaces.Surveys;
using VoiceFlow.Application.Services;
using VoiceFlow.Application.Validators;
using VoiceFlow.Application.Validators.Serveys;
using VoiceFlow.Infrastructure;
using VoiceFlow.Infrastructure.Options;
using VoiceFlow.Infrastructure.Persistence;
using VoiceFlow.Surveys.Application;
using VoiceFlow.Application.dependencyInjection;
using VoiceFlow.Api.Backgrounds;
using VoiceFlow.Core.Interfaces.Repositories.Hubspot;
using VoiceFlow.Api.LiveChat.Config;
using VoiceFlow.Api.LiveChat.Infrastructure.Logging;
using VoiceFlow.Api.LiveChat.Infrastructure.Mongo;
// Must be called once before any MongoDB driver usage — registers BSON serializers,
// camelCase conventions, and all SaasApi entity class maps.
SaasApi.Infrastructure.Persistence.MongoSerializationRegistry.Register();

// Canonical lowercase persistence for CallDirection ("inbound" / "outbound" / "internal").
MongoDB.Bson.Serialization.BsonSerializer.RegisterSerializer(
    typeof(VoiceFlow.Core.Enums.CallDirection),
    new VoiceFlow.Infrastructure.Persistence.Serializers.CallDirectionSerializer());

var builder = WebApplication.CreateBuilder(args);
 
builder.Services.AddHelperServices(builder.Configuration);
// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddVoiceFlowApplication(builder.Configuration);

// SaasApi Application layer — AutoMapper, FluentValidation, application services
SaasApi.Application.DependencyInjection.AddApplication(builder.Services);

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<ISurveyService, SurveyService>();
builder.Services.AddValidatorsFromAssemblyContaining<SurveyCreateRequestValidator>();

// SaasApi Infrastructure layer — IMongoClient, IMongoDatabaseAccessor, IMongoDatabase,
// repositories, JWT/password services, Google OAuth HTTP client.
// Reads MongoDB connection and database from the shared MongoDB config section.
var mongoSection = builder.Configuration.GetSection("MongoDB");


SaasApi.Infrastructure.DependencyInjection.AddInfrastructure(
    builder.Services,
    builder.Configuration,
    mongoConnectionString: mongoSection["ConnectionString"],
    mongoDatabaseName: mongoSection["voicebotdb"]);

// HttpContextAccessor for CurrentUser
builder.Services.AddHttpContextAccessor();

// Scoped context services
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
var publicKeyPath = Path.Combine(builder.Environment.ContentRootPath, jwtSettings.PublicKeyPath);

TokenValidationParameters tokenValidationParameters;

if (File.Exists(publicKeyPath))
{
    var rsa = RSA.Create();
    rsa.ImportFromPem(File.ReadAllText(publicKeyPath));
    var rsaKey = new RsaSecurityKey(rsa);

    tokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = rsaKey,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
}
else
{
    // Development fallback — no public key file yet
    tokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = false,
        SignatureValidator = (token, _) =>
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            return handler.ReadJwtToken(token);
        }
    };
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = tokenValidationParameters;
        // SignalR over WebSocket: browsers can't set headers on the WS handshake,
        // so accept the JWT via ?access_token=... on /hubs/* only.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

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

// Rate Limiting
var rateLimitSection = builder.Configuration.GetSection("RateLimit");
var permitLimit = rateLimitSection.GetValue<int>("PermitLimit", 100);
var windowSeconds = rateLimitSection.GetValue<int>("WindowSeconds", 60);

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = permitLimit;
        limiterOptions.Window = TimeSpan.FromSeconds(windowSeconds);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Application services (US1)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();

// Application services (US2)
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IRbacService, RbacService>();

// Application services (US3-US9)
builder.Services.AddScoped<ICallService, CallService>();
builder.Services.AddScoped<ILiveSnapshotService, LiveSnapshotService>();
builder.Services.AddScoped<IFlowService, FlowService>();
builder.Services.AddScoped<IVoiceLibraryService, VoiceLibraryService>();
builder.Services.AddScoped<IVoicemailService, VoicemailService>();
builder.Services.AddScoped<IEmailChannelService, EmailChannelService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IAutoTagService, AutoTagService>();
builder.Services.AddScoped<ISipAccountService, SipAccountService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IExportToSqlService, ExportToSqlService>();
builder.Services.AddScoped<IEditLogService, EditLogService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IQueueService, QueueService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<FlowValidator>();
builder.Services.AddScoped<AsteriskExporter>();

builder.Services.AddHostedService<HubSpotStateCleanupHostedService>();

// LiveChat module — SignalR hubs, routing engine, presence, channel adapters, workers.
builder.Services.AddLiveChat(builder.Configuration);

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
    });

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "VoiceFlow Studio API",
        Version = "v1",
        Description = "Call center and IVR platform API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT access token"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });
});

var app = builder.Build();

// Seed MongoDB indexes on startup
using (var scope = app.Services.CreateScope())
{
    var bootstrap = scope.ServiceProvider.GetRequiredService<CollectionBootstrap>();
    await bootstrap.InitializeAsync();

    try
    {
        var liveChatBootstrap = scope.ServiceProvider.GetRequiredService<LiveChatCollectionBootstrap>();
        await liveChatBootstrap.InitializeAsync();

        var liveChatLogWriter = scope.ServiceProvider.GetRequiredService<AgentHubLogWriter>();
        await liveChatLogWriter.TryWriteAsync(
            "Information",
            "AgentHubStartupDiagnostics",
            "AgentHub startup diagnostics initialized",
            new Dictionary<string, object?>
            {
                ["environment"] = builder.Environment.EnvironmentName,
                ["machineName"] = Environment.MachineName,
            });
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "LiveChat MongoDB bootstrap failed — AgentHub live chat collections may be unavailable.");
    }

    // AgentHub dependency smoke-test — every scoped/singleton dep the hub constructor
    // needs must resolve, otherwise SignalR closes with 1011 before any hub code runs.
    try
    {
        var liveChatLogWriter = scope.ServiceProvider.GetRequiredService<AgentHubLogWriter>();
        var depTypes = new (string Name, Type Type)[]
        {
            ("IProfileRepository", typeof(VoiceFlow.Core.Interfaces.Repositories.IProfileRepository)),
            ("IRoomRepository", typeof(VoiceFlow.Api.LiveChat.Application.Abstractions.IRoomRepository)),
            ("IPresenceStore", typeof(VoiceFlow.Api.LiveChat.Application.Abstractions.IPresenceStore)),
            ("RoutingEngine", typeof(VoiceFlow.Api.LiveChat.Application.RoutingEngine)),
            ("RoomService", typeof(VoiceFlow.Api.LiveChat.Application.RoomService)),
            ("ILogger<AgentHub>", typeof(ILogger<VoiceFlow.Api.LiveChat.Hubs.AgentHub>)),
        };

        foreach (var (name, type) in depTypes)
        {
            try
            {
                _ = scope.ServiceProvider.GetRequiredService(type);
                await liveChatLogWriter.TryWriteAsync(
                    "Information",
                    "AgentHubDependencySmokeTest",
                    $"Resolved {name}",
                    new Dictionary<string, object?> { ["dependency"] = name });
            }
            catch (Exception depEx)
            {
                await liveChatLogWriter.TryWriteAsync(
                    "Error",
                    "AgentHubDependencySmokeTest",
                    $"Failed to resolve {name}: {depEx.GetType().FullName}: {depEx.Message}",
                    new Dictionary<string, object?>
                    {
                        ["dependency"] = name,
                        ["exceptionType"] = depEx.GetType().FullName,
                    },
                    depEx);
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(depEx, "AgentHub dependency {Dep} failed to resolve at startup", name);
            }
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "AgentHub dependency smoke-test could not run.");
    }


    // SaasApi indexes and provider catalog seed
    var saasDb = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
    try
    {
        await SaasApi.Infrastructure.Persistence.CollectionBootstrap.EnsureIndexesAsync(saasDb);
        await SaasApi.Infrastructure.Persistence.ProviderCatalogSeeder.SeedIfEmptyAsync(saasDb);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "SaasApi MongoDB bootstrap or provider catalog seed skipped — ensure MongoDB is reachable and MongoDB:ConnectionString is configured.");
    }
}

// Middleware pipeline
//app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<HelperLib.SharedMiddlewares.ExceptionMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();


app.UseMiddleware<LocalizationMiddleware>();

app.UseDefaultFiles();
app.UseStaticFiles();

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VoiceFlow Studio API v1");
        c.RoutePrefix = "swagger";
    });
//}

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AgentHubHandshakeDiagnosticsMiddleware>();
app.UseMiddleware<TenantContextMiddleware>();

app.MapControllers();
app.MapLiveChat();
app.MapFallbackToFile("index.html");



try
{
    LoggingHelper.LogStartup(app.Services.GetRequiredService<IConfiguration>());
    await app.RunAsync();

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

}
catch (Exception ex)
{
    LoggingHelper.LogStartupFailure(ex);
}
finally
{
    LoggingHelper.EnsureFlush();
}




