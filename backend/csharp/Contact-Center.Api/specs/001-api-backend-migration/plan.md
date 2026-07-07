# Implementation Plan: API Backend Migration

**Branch**: `001-api-backend-migration` | **Date**: 2026-05-11 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/001-api-backend-migration/spec.md`

## Summary

Build a .NET 8 MongoDB backend API to replace the existing Supabase/PostgreSQL backend for VoiceFlow Studio, a multi-tenant call center and IVR management platform. The API will serve the existing Lovable frontend, providing authentication, RBAC, call management with AI features, IVR flow editing, voice library, contacts, SIP configuration, billing, and audit logging.

**Technical Approach**: Onion Architecture with 5 layers (Core, Application, Infrastructure, API, Contracts), MongoDB for persistence with repository-level tenant isolation, RS256 JWT authentication, and cloud storage for media files.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (LTS)  
**Primary Dependencies**: ASP.NET Core 8, MongoDB.Driver 2.x, AutoMapper, Polly, Swashbuckle  
**Storage**: MongoDB (document database) + Cloud Object Storage (Azure Blob/S3 for recordings/voice files)  
**Testing**: xUnit + Moq (or NSubstitute), WebApplicationFactory with MongoDB test containers  
**Target Platform**: Linux/Windows server (containerized, Docker-ready)  
**Project Type**: Web API service (REST)  
**Performance Goals**: <500ms p95 response time, 100 concurrent users per tenant  
**Constraints**: Multi-tenant isolation at repository level, RS256 JWT authentication, Arabic/English localization  
**Scale/Scope**: 13 domain entities, 9 feature modules, ~50 API endpoints

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Tenant Isolation First | ✅ PASS | All repositories filter by TenantId; negative-path tests required |
| II | Onion Architecture | ✅ PASS | 5-layer structure: Core → Application → Infrastructure → API + Contracts |
| III | API-First | ✅ PASS | Swagger/OpenAPI documentation required for all endpoints |
| IV | Security by Default | ✅ PASS | RS256 JWT on all endpoints except /health, /ready, /api/v1/auth/* |
| V | Testability | ✅ PASS | Unit tests for all Application services with mocked dependencies |
| VI | Result\<T\> Pattern | ✅ PASS | Business errors via Result<T>, not exceptions |
| VII | Consistency | ✅ PASS | Repository pattern, ApiResponse<T> envelope, AutoMapper profiles |
| VIII | Persistence Configuration | ✅ PASS | Configurations/ and Indexes/ folders for MongoDB setup |
| IX | Projection & Filtering | ✅ PASS | Optional projection parameters, pagination for large collections |
| X | Rate Limiting & Resilience | ✅ PASS | Global rate limiting middleware, Polly circuit breakers |
| XI | Localization | ✅ PASS | Arabic (ar) and English (en) with Accept-Language header |
| XII | Entity Identifiers | ✅ PASS | String IDs with ObjectId storage, no Guids |
| XIII | Service Interface Organization | ✅ PASS | One interface per file (IAuthService.cs, etc.) |
| XIV | Voice & Media Handling | ✅ PASS | Private cloud storage with signed URLs |
| XV | IVR Flow Management | ✅ PASS | JSON graph storage, validation before publish, Asterisk export |
| XVI | Real-Time Communication | ✅ PASS | Secure SIP credentials, tenant-scoped STUN/TURN config |
| XVII | RBAC & Permissions | ✅ PASS | RbacRole + RbacUserRole collections, service-level auth checks |

**Gate Result**: ✅ ALL PASS — Proceed to Phase 0

## Project Structure

### Documentation (this feature)

```text
specs/001-api-backend-migration/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (API contracts)
│   ├── auth.md
│   ├── accounts.md
│   ├── profiles.md
│   ├── rbac.md
│   ├── calls.md
│   ├── flows.md
│   ├── voice-library.md
│   ├── contacts.md
│   ├── tags.md
│   ├── sip-accounts.md
│   ├── invoices.md
│   └── edit-logs.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks command)
```

### Source Code (repository root)

```text
src/
├── VoiceFlow.Core/                    # Domain Layer (innermost)
│   ├── Entities/
│   │   ├── Account.cs
│   │   ├── Profile.cs
│   │   ├── RbacRole.cs
│   │   ├── RbacUserRole.cs
│   │   ├── Call.cs
│   │   ├── Flow.cs
│   │   ├── Contact.cs
│   │   ├── Tag.cs
│   │   ├── VoiceLibraryItem.cs
│   │   ├── SipAccount.cs
│   │   ├── SoftphoneCallLog.cs
│   │   ├── Invoice.cs
│   │   └── EditLog.cs
│   ├── ValueObjects/
│   │   ├── FlowNode.cs
│   │   ├── FlowEdge.cs
│   │   ├── Permission.cs
│   │   └── PhoneNumber.cs
│   ├── Interfaces/
│   │   ├── Repositories/
│   │   │   ├── IAccountRepository.cs
│   │   │   ├── IProfileRepository.cs
│   │   │   ├── IRbacRoleRepository.cs
│   │   │   ├── ICallRepository.cs
│   │   │   ├── IFlowRepository.cs
│   │   │   └── ... (one per entity)
│   │   └── Services/
│   │       ├── IStorageService.cs
│   │       ├── IAiGatewayService.cs
│   │       ├── IEmailService.cs
│   │       └── ITtsService.cs
│   ├── Enums/
│   │   ├── CallDirection.cs
│   │   ├── CallStatus.cs
│   │   ├── FlowStatus.cs
│   │   ├── InvoiceStatus.cs
│   │   └── Sentiment.cs
│   └── Common/
│       ├── Result.cs
│       ├── Error.cs
│       └── ITenantScoped.cs
│
├── VoiceFlow.Application/             # Application Layer
│   ├── Services/
│   │   ├── IAuthService.cs
│   │   ├── AuthService.cs
│   │   ├── IAccountService.cs
│   │   ├── AccountService.cs
│   │   ├── IProfileService.cs
│   │   ├── ProfileService.cs
│   │   ├── IRbacService.cs
│   │   ├── RbacService.cs
│   │   ├── ICallService.cs
│   │   ├── CallService.cs
│   │   ├── IFlowService.cs
│   │   ├── FlowService.cs
│   │   ├── IVoiceLibraryService.cs
│   │   ├── VoiceLibraryService.cs
│   │   ├── IContactService.cs
│   │   ├── ContactService.cs
│   │   ├── ITagService.cs
│   │   ├── TagService.cs
│   │   ├── ISipAccountService.cs
│   │   ├── SipAccountService.cs
│   │   ├── IInvoiceService.cs
│   │   ├── InvoiceService.cs
│   │   └── IEditLogService.cs
│   ├── Validators/
│   │   ├── FlowValidator.cs
│   │   └── ... (validation logic)
│   └── Common/
│       ├── ITenantContext.cs
│       └── ICurrentUser.cs
│
├── VoiceFlow.Infrastructure/          # Infrastructure Layer
│   ├── Persistence/
│   │   ├── MongoDbContext.cs
│   │   ├── CollectionBootstrap.cs
│   │   ├── Configurations/
│   │   │   ├── AccountConfiguration.cs
│   │   │   ├── ProfileConfiguration.cs
│   │   │   └── ... (one per entity)
│   │   ├── Indexes/
│   │   │   ├── AccountIndexes.cs
│   │   │   ├── ProfileIndexes.cs
│   │   │   └── ... (one per entity)
│   │   └── Repositories/
│   │       ├── AccountRepository.cs
│   │       ├── ProfileRepository.cs
│   │       └── ... (one per entity)
│   ├── ExternalServices/
│   │   ├── AzureBlobStorageService.cs
│   │   ├── AiGatewayService.cs
│   │   ├── SmtpEmailService.cs
│   │   └── TtsService.cs
│   ├── Auth/
│   │   ├── JwtTokenService.cs
│   │   ├── PasswordHasher.cs
│   │   └── RefreshTokenStore.cs
│   ├── Mapping/
│   │   ├── AccountMappingProfile.cs
│   │   ├── ProfileMappingProfile.cs
│   │   └── ... (one per entity)
│   └── DependencyInjection.cs
│
├── VoiceFlow.Contracts/               # DTOs Layer
│   ├── Auth/
│   │   ├── SignupRequest.cs
│   │   ├── LoginRequest.cs
│   │   ├── TokenResponse.cs
│   │   └── ...
│   ├── Accounts/
│   │   ├── AccountResponse.cs
│   │   ├── UpdateAccountRequest.cs
│   │   └── ...
│   ├── Profiles/
│   ├── Rbac/
│   ├── Calls/
│   ├── Flows/
│   ├── VoiceLibrary/
│   ├── Contacts/
│   ├── Tags/
│   ├── SipAccounts/
│   ├── Invoices/
│   ├── EditLogs/
│   └── Common/
│       ├── ApiResponse.cs
│       ├── PagedResponse.cs
│       └── ErrorResponse.cs
│
└── VoiceFlow.Api/                     # API Layer (outermost)
    ├── Controllers/
    │   ├── AuthController.cs
    │   ├── AccountsController.cs
    │   ├── ProfilesController.cs
    │   ├── RbacController.cs
    │   ├── CallsController.cs
    │   ├── FlowsController.cs
    │   ├── VoiceLibraryController.cs
    │   ├── ContactsController.cs
    │   ├── TagsController.cs
    │   ├── SipAccountsController.cs
    │   ├── InvoicesController.cs
    │   ├── EditLogsController.cs
    │   └── HealthController.cs
    ├── Middleware/
    │   ├── TenantContextMiddleware.cs
    │   ├── ExceptionHandlingMiddleware.cs
    │   └── LocalizationMiddleware.cs
    ├── Filters/
    │   └── ValidationFilter.cs
    ├── Resources/
    │   ├── Messages.en.json
    │   └── Messages.ar.json
    ├── Program.cs
    ├── appsettings.json
    └── appsettings.Development.json

tests/
├── VoiceFlow.Core.Tests/
├── VoiceFlow.Application.Tests/
│   ├── AuthServiceTests.cs
│   ├── CallServiceTests.cs
│   └── ... (one per service)
├── VoiceFlow.Infrastructure.Tests/
└── VoiceFlow.Api.Tests/
    ├── Controllers/
    └── Integration/
```

**Structure Decision**: Onion Architecture with 5 projects following the constitution's layer dependencies. The `VoiceFlow.Core` project has zero external dependencies, `VoiceFlow.Application` depends only on Core, `VoiceFlow.Infrastructure` implements Core interfaces, and `VoiceFlow.Api` orchestrates everything.

## Complexity Tracking

No constitution violations requiring justification. The architecture follows all 17 principles.

## Phase 0: Research Summary

See [research.md](./research.md) for detailed findings.

**Key Decisions**:
1. **JWT Implementation**: Use `System.IdentityModel.Tokens.Jwt` with RS256 for token signing
2. **Password Hashing**: BCrypt via `BCrypt.Net-Next` package
3. **MongoDB Driver**: Official `MongoDB.Driver` 2.x with BSON serialization
4. **Cloud Storage**: Abstracted `IStorageService` with Azure Blob implementation (swappable)
5. **Rate Limiting**: ASP.NET Core built-in `Microsoft.AspNetCore.RateLimiting`
6. **Circuit Breaker**: Polly via `Microsoft.Extensions.Http.Polly`
7. **Localization**: JSON resource files with `IStringLocalizer`

## Phase 1: Design Artifacts

- [data-model.md](./data-model.md) — Entity definitions and relationships
- [contracts/](./contracts/) — API endpoint contracts
- [quickstart.md](./quickstart.md) — Developer setup guide

## Next Steps

Run `/speckit-tasks` to generate the task breakdown for implementation.
