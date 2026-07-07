# VoiceFlow Studio API

A .NET 8 call center and IVR platform API built on **Onion Architecture** with **MongoDB** persistence and **custom JWT authentication**.

---

## Architecture

```
VoiceFlow.Core          — Domain entities, interfaces, enums, value objects (no external deps)
VoiceFlow.Application   — Business logic services, validators, exporters
VoiceFlow.Infrastructure — MongoDB repositories, JWT/auth, external service adapters
VoiceFlow.Contracts     — Request/response DTOs shared across layers
VoiceFlow.Api           — ASP.NET Core Web API, middleware, controllers
```

### Dependency Rules

```
Core ← Application ← Infrastructure
Core ←                Infrastructure
Core ←                               ← Contracts
                                       ← API (references all)
```

---

## Tech Stack

| Concern | Technology |
|---------|-----------|
| Runtime | .NET 8 / C# 12 |
| Web Framework | ASP.NET Core 8 |
| Database | MongoDB 7 (MongoDB.Driver) |
| Authentication | Custom JWT (RS256 asymmetric keys) |
| Password Hashing | BCrypt.Net-Next |
| API Docs | Swashbuckle / Swagger UI |
| Rate Limiting | ASP.NET Core built-in RateLimiter |
| Containerization | Docker / docker-compose |

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [MongoDB 7+](https://www.mongodb.com/try/download/community) or Docker
- (Optional) OpenSSL for RSA key generation

---

## Quick Start

### Option A — Docker Compose (recommended)

```bash
docker compose up --build
```

- API: `http://localhost:8080`
- Swagger UI: `http://localhost:8080/swagger`
- Mongo Express: `http://localhost:8081`

### Option B — Local Development

1. **Clone and restore**:

   ```bash
   git clone <repo-url>
   cd "VoiceFlow Studio.Api"
   dotnet restore
   ```

2. **Configure `appsettings.Development.json`** (already git-ignored):

   ```json
   {
     "MongoDB": {
       "ConnectionString": "mongodb://localhost:27017",
       "DatabaseName": "voiceflow"
     },
     "Jwt": {
       "Issuer": "voiceflow-studio",
       "Audience": "voiceflow-clients",
       "AccessTokenExpiryMinutes": 60,
       "RefreshTokenExpiryDays": 30,
       "PrivateKeyPath": "keys/private.pem",
       "PublicKeyPath": "keys/public.pem"
     }
   }
   ```

3. **(Optional) Generate RSA keys** — the API falls back to a dev-mode token validator when keys are absent:

   ```bash
   mkdir -p src/VoiceFlow.Api/keys
   openssl genrsa -out src/VoiceFlow.Api/keys/private.pem 2048
   openssl rsa -in src/VoiceFlow.Api/keys/private.pem -pubout -out src/VoiceFlow.Api/keys/public.pem
   ```

4. **Run**:

   ```bash
   dotnet run --project src/VoiceFlow.Api
   ```

5. Open **Swagger UI**: `https://localhost:7xxx/swagger`

---

## API Endpoints

All endpoints are prefixed with `/api/v1`.

| Group | Path | Description |
|-------|------|-------------|
| Health | `GET /health` | Liveness probe |
| Auth | `POST /auth/signup` | Create account |
| Auth | `POST /auth/login` | Obtain JWT tokens |
| Auth | `POST /auth/refresh` | Rotate refresh token |
| Auth | `POST /auth/logout` | Revoke refresh token |
| Auth | `POST /auth/recover` | Request password reset |
| Auth | `POST /auth/reset` | Confirm password reset |
| Profiles | `GET /profiles/me` | Get own profile |
| Profiles | `PATCH /profiles/me` | Update own profile |
| Accounts | `GET /accounts/{id}` | Get account details |
| Accounts | `PATCH /accounts/{id}` | Update account settings |
| RBAC | `GET /rbac/roles` | List roles |
| RBAC | `POST /rbac/roles` | Create role |
| RBAC | `PATCH /rbac/roles/{id}` | Update role |
| RBAC | `DELETE /rbac/roles/{id}` | Delete role |
| RBAC | `POST /rbac/assignments` | Assign role to user |
| RBAC | `DELETE /rbac/assignments/{id}` | Revoke role |
| Calls | `GET /calls` | Search call history |
| Calls | `POST /calls` | Create call record |
| Calls | `GET /calls/{id}` | Get call details |
| Calls | `PATCH /calls/{id}` | Update call record |
| Calls | `GET /calls/{id}/recording` | Get signed recording URL |
| Calls | `POST /calls/{id}/summary` | Generate AI summary |
| Flows | `GET /flows` | List IVR flows |
| Flows | `POST /flows` | Create flow |
| Flows | `GET /flows/{id}` | Get flow |
| Flows | `PATCH /flows/{id}` | Update flow |
| Flows | `DELETE /flows/{id}` | Delete flow |
| Flows | `GET /flows/{id}/validate` | Validate flow |
| Flows | `POST /flows/{id}/publish` | Publish flow |
| Flows | `GET /flows/{id}/export` | Export Asterisk config |
| Voice Library | `GET /voice-library` | List voice items |
| Voice Library | `POST /voice-library` | Upload audio file |
| Voice Library | `POST /voice-library/tts` | Generate TTS audio |
| Voice Library | `GET /voice-library/{id}/url` | Get signed URL |
| Contacts | `GET /contacts` | Search contacts |
| Contacts | `POST /contacts` | Create contact |
| Contacts | `PATCH /contacts/{id}` | Update contact |
| Contacts | `DELETE /contacts/{id}` | Delete contact |
| Tags | `GET /tags` | List tags |
| Tags | `POST /tags` | Create tag |
| Tags | `PATCH /tags/{id}` | Update tag |
| Tags | `DELETE /tags/{id}` | Delete tag |
| SIP | `GET /sip/account` | Get SIP config |
| SIP | `POST /sip/account` | Create SIP account |
| SIP | `PATCH /sip/account` | Update SIP account |
| SIP | `GET /sip/call-logs` | List softphone logs |
| SIP | `POST /sip/call-logs` | Create softphone log |
| Invoices | `GET /invoices` | List invoices |
| Invoices | `POST /invoices` | Create invoice |
| Invoices | `GET /invoices/{id}` | Get invoice |
| Invoices | `PATCH /invoices/{id}` | Update invoice |
| Invoices | `DELETE /invoices/{id}` | Delete invoice |
| Audit | `GET /audit` | Query audit trail |

---

## Multi-Tenancy

Tenant isolation is enforced at the **repository layer**. Every entity that implements `ITenantScoped` has a `TenantId` field. All queries automatically filter by the tenant resolved from the JWT claim `tenant_id` via `TenantContextMiddleware`.

---

## Authentication Flow

```
POST /auth/signup  →  creates AuthUser + Account + Profile
POST /auth/login   →  returns { accessToken, refreshToken, expiresIn }
Authorization: Bearer <accessToken>   (all protected routes)
POST /auth/refresh  →  rotates refresh token, issues new access token
POST /auth/logout   →  revokes refresh token server-side
```

JWT tokens use **RS256** (asymmetric) signing. Place PEM key files in `src/VoiceFlow.Api/keys/` (excluded from git). In development without key files, signature validation is bypassed.

---

## MongoDB Indexes

Indexes are created on startup via `CollectionBootstrap`. Each collection has a static `*Indexes.CreateAsync()` class that creates compound and unique indexes optimized for common query patterns.

---

## Localization

Pass `Accept-Language: ar` to receive Arabic error messages. Default is English (`en`).

---

## Rate Limiting

A fixed-window rate limiter is applied globally. Configurable via `appsettings.json`:

```json
"RateLimit": {
  "PermitLimit": 100,
  "WindowSeconds": 60
}
```

---

## Project Structure

```
VoiceFlow Studio.Api/
├── src/
│   ├── VoiceFlow.Api/             # Web API host
│   │   ├── Controllers/           # HTTP endpoints
│   │   ├── Middleware/            # Exception, Tenant, Localization
│   │   ├── Resources/             # Messages.en.json, Messages.ar.json
│   │   └── Services/              # TenantContext, CurrentUser
│   ├── VoiceFlow.Application/     # Business logic
│   │   ├── Services/              # IXxxService + XxxService
│   │   ├── Validators/            # FlowValidator
│   │   └── Exporters/             # AsteriskExporter
│   ├── VoiceFlow.Core/            # Domain (no external deps)
│   │   ├── Common/                # Result<T>, Entity, ITenantScoped
│   │   ├── Entities/              # All domain entities
│   │   ├── Enums/                 # Domain enumerations
│   │   ├── Interfaces/            # IRepository<T>, IXxxService
│   │   └── ValueObjects/          # PhoneNumber, Permission, FlowNode
│   ├── VoiceFlow.Contracts/       # DTOs
│   │   └── {Domain}/              # Requests + Responses per domain
│   └── VoiceFlow.Infrastructure/  # External concerns
│       ├── Auth/                  # JWT, BCrypt, RefreshTokenStore
│       ├── Configuration/         # MongoDbSettings, JwtSettings
│       ├── ExternalServices/      # Storage, AI Gateway, TTS, Email
│       └── Persistence/           # MongoDbContext, Repositories, Indexes
├── specs/                         # Feature specifications (Spec Kit)
├── Dockerfile
├── docker-compose.yml
└── VoiceFlow.sln
```

---

## Building & Running Tests

```bash
# Build
dotnet build

# Run API
dotnet run --project src/VoiceFlow.Api

# Publish
dotnet publish src/VoiceFlow.Api -c Release -o ./publish
```

---

## License

Proprietary — All rights reserved.
