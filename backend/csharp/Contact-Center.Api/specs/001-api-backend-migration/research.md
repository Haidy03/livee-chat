# Research: API Backend Migration

**Feature**: 001-api-backend-migration  
**Date**: 2026-05-11

## JWT Authentication (RS256)

### Decision
Use `System.IdentityModel.Tokens.Jwt` with RSA key pair for RS256 signing.

### Rationale
- RS256 is asymmetric: private key signs, public key validates
- Allows token validation without exposing signing key
- Industry standard for multi-service architectures
- Built into .NET ecosystem

### Alternatives Considered
- **HS256 (HMAC)**: Simpler but requires shared secret; rejected per constitution mandate for RS256
- **Third-party identity providers**: Adds external dependency; rejected for self-contained auth requirement

### Implementation Notes
```csharp
// Token generation (Infrastructure layer)
var credentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);
var token = new JwtSecurityToken(
    issuer: "voiceflow-api",
    claims: new[] {
        new Claim("sub", userId),
        new Claim("tenant_id", tenantId),
        new Claim("roles", string.Join(",", roles))
    },
    expires: DateTime.UtcNow.AddHours(1),
    signingCredentials: credentials
);
```

---

## Password Hashing

### Decision
Use `BCrypt.Net-Next` NuGet package for password hashing.

### Rationale
- BCrypt is designed for password hashing (adaptive work factor)
- Includes salt automatically
- Well-maintained .NET implementation
- Resistant to rainbow table and brute-force attacks

### Alternatives Considered
- **Argon2**: More modern but less .NET ecosystem support
- **PBKDF2**: Built into .NET but requires more configuration
- **SHA256 + salt**: Too fast for password hashing; vulnerable to brute force

### Implementation Notes
```csharp
// Hash password
string hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

// Verify password
bool valid = BCrypt.Net.BCrypt.Verify(password, hash);
```

---

## MongoDB Driver & Serialization

### Decision
Use official `MongoDB.Driver` 2.x with BSON class maps for serialization.

### Rationale
- Official driver with full feature support
- LINQ provider for type-safe queries
- Class maps allow custom serialization without attributes
- Supports all MongoDB features (indexes, aggregation, projections)

### Alternatives Considered
- **MongoFramework**: Higher-level abstraction but adds complexity
- **Entity Framework Core + MongoDB Provider**: Immature, limited support

### Implementation Notes
```csharp
// Configuration class pattern
BsonClassMap.RegisterClassMap<Account>(cm =>
{
    cm.AutoMap();
    cm.SetIgnoreExtraElements(true);
    cm.MapIdMember(c => c.Id)
        .SetSerializer(new StringSerializer(BsonType.ObjectId));
    cm.MapMember(c => c.TenantId); // Implicit index candidate
});
```

---

## Cloud Storage Abstraction

### Decision
Define `IStorageService` interface in Core; implement with Azure Blob Storage initially (swappable to S3/GCS).

### Rationale
- Constitution requires storage-agnostic design
- Signed URLs required for all media access
- Private containers for recordings and voice-library
- Easy to swap implementations via DI

### Alternatives Considered
- **Direct Azure SDK usage**: Violates Core layer no-dependency rule
- **Local filesystem**: Not suitable for production; cloud-native requirement

### Interface Design
```csharp
// Core/Interfaces/Services/IStorageService.cs
public interface IStorageService
{
    Task<string> UploadAsync(string container, string path, Stream content, string contentType);
    Task<string> GetSignedUrlAsync(string container, string path, TimeSpan expiry);
    Task DeleteAsync(string container, string path);
    Task<Stream> DownloadAsync(string container, string path);
}
```

---

## Rate Limiting

### Decision
Use ASP.NET Core built-in rate limiting middleware (`Microsoft.AspNetCore.RateLimiting`).

### Rationale
- Built into .NET 8; no external dependencies
- Supports fixed window, sliding window, token bucket, concurrency limiters
- Global middleware + per-endpoint overrides
- Configuration via appsettings.json

### Alternatives Considered
- **AspNetCoreRateLimit**: Popular but now redundant with built-in support
- **Custom middleware**: Reinventing the wheel

### Implementation Notes
```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.FindFirst("tenant_id")?.Value ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

---

## Circuit Breaker & Resilience

### Decision
Use Polly via `Microsoft.Extensions.Http.Polly` for HTTP client resilience.

### Rationale
- Polly is the de-facto standard for .NET resilience
- Integrates with `IHttpClientFactory`
- Supports circuit breaker, retry, timeout, bulkhead
- Constitution mandates circuit breakers for external services

### Alternatives Considered
- **Microsoft.Extensions.Http.Resilience**: Newer but less documented
- **Custom retry logic**: Error-prone, misses edge cases

### Implementation Notes
```csharp
// DI registration
services.AddHttpClient<IAiGatewayService, AiGatewayService>()
    .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30)))
    .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));
```

---

## Localization

### Decision
Use ASP.NET Core localization with JSON resource files loaded at startup.

### Rationale
- Constitution requires Arabic (ar) and English (en) support
- JSON files are easier to edit than RESX
- In-memory caching for performance
- `Accept-Language` header determines response language

### Alternatives Considered
- **RESX files**: More IDE support but harder for non-developers to edit
- **Database-backed**: Overkill for two languages with static messages

### Implementation Notes
```csharp
// Startup
services.AddLocalization();
services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
});
```

---

## AI Gateway Integration

### Decision
Create `IAiGatewayService` interface with HTTP client implementation using Polly resilience.

### Rationale
- External service requires circuit breaker (constitution mandate)
- Timeout of 30 seconds per operation (spec success criteria)
- Retry with exponential backoff for transient failures
- Abstract interface allows testing with mocks

### API Contract (assumed)
```
POST /v1/transcribe
  Body: { "audio_url": "...", "language": "ar|en" }
  Response: { "transcript": "..." }

POST /v1/summarize
  Body: { "text": "...", "language": "ar|en" }
  Response: { "summary": "...", "sentiment": "positive|neutral|negative" }

POST /v1/translate
  Body: { "text": "...", "source": "ar", "target": "en" }
  Response: { "translated": "..." }
```

---

## TTS Integration

### Decision
Define `ITtsService` interface; specific provider TBD (Azure Cognitive Services, Google Cloud TTS, or Amazon Polly).

### Rationale
- Spec assumption: "TTS generation will use a third-party service"
- Interface allows provider swapping
- Must support Arabic and English voices
- Output format: MP3 or WAV

### Interface Design
```csharp
public interface ITtsService
{
    Task<Stream> GenerateAsync(string text, string language, string voice);
}
```

---

## Email Service

### Decision
Define `IEmailService` interface; implement with SMTP or SendGrid/Mailgun.

### Rationale
- Required for password recovery (FR-003)
- Interface allows provider flexibility
- Must support HTML email templates

### Interface Design
```csharp
public interface IEmailService
{
    Task SendPasswordResetAsync(string email, string resetLink, string language);
}
```

---

## Result\<T\> Pattern Implementation

### Decision
Create custom `Result<T>` type in Core layer following functional programming patterns.

### Rationale
- Constitution mandate (Principle VI)
- Makes success/failure explicit
- Typed errors enable pattern matching
- No exceptions for business logic failures

### Implementation
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);
}

public record Error(string Code, string Message);
```

---

## Refresh Token Strategy

### Decision
Store refresh tokens in MongoDB with user binding and expiration.

### Rationale
- Access tokens are short-lived (1 hour)
- Refresh tokens enable session extension without re-login
- Must be revocable (logout invalidates refresh token)
- One refresh token per user session

### Storage
```csharp
public class RefreshToken
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
}
```

---

## IVR Flow Validation

### Decision
Implement `FlowValidator` in Application layer with graph traversal algorithms.

### Rationale
- Constitution requires validation before publishing (Principle XV)
- Must detect: orphan nodes, invalid node types, missing required fields
- Must verify voice library references exist
- Validation errors returned via Result<T>

### Validation Rules
1. Flow must have exactly one start node
2. All nodes must be reachable from start node
3. No orphan nodes (nodes with no incoming/outgoing edges except start)
4. All referenced voice library IDs must exist in tenant's library
5. Required fields per node type must be present

---

## Asterisk Export

### Decision
Implement flow export in Application layer with two formatters.

### Rationale
- Spec requires extensions.conf and ARA SQL formats (FR-032, FR-033)
- Export is business logic (Application layer)
- No external dependencies needed

### Output Formats
1. **extensions.conf**: Asterisk dialplan configuration file
2. **ARA SQL**: MySQL INSERT statements for Asterisk Realtime Architecture
