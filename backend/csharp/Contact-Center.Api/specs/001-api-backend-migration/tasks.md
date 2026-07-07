# Tasks: API Backend Migration

**Input**: Design documents from `specs/001-api-backend-migration/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/, research.md, quickstart.md

**Tests**: Tests are NOT explicitly requested in the specification. Test tasks are omitted.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create solution structure and configure all 5 projects per Onion Architecture

- [X] T001 Create solution file VoiceFlow.sln at repository root
- [X] T002 Create VoiceFlow.Core project in src/VoiceFlow.Core/VoiceFlow.Core.csproj (no external dependencies)
- [X] T003 [P] Create VoiceFlow.Application project in src/VoiceFlow.Application/VoiceFlow.Application.csproj
- [X] T004 [P] Create VoiceFlow.Infrastructure project in src/VoiceFlow.Infrastructure/VoiceFlow.Infrastructure.csproj
- [X] T005 [P] Create VoiceFlow.Contracts project in src/VoiceFlow.Contracts/VoiceFlow.Contracts.csproj
- [X] T006 [P] Create VoiceFlow.Api project in src/VoiceFlow.Api/VoiceFlow.Api.csproj
- [X] T007 Configure project references per Onion Architecture (Core←Application←Infrastructure, API→all)
- [X] T008 [P] Add NuGet packages to VoiceFlow.Infrastructure (MongoDB.Driver, BCrypt.Net-Next, AutoMapper, Polly)
- [X] T009 [P] Add NuGet packages to VoiceFlow.Api (Swashbuckle, Microsoft.AspNetCore.RateLimiting)
- [X] T010 [P] Create .editorconfig and Directory.Build.props for consistent code style
- [X] T011 Create src/VoiceFlow.Api/appsettings.json with configuration structure
- [X] T012 [P] Create src/VoiceFlow.Api/appsettings.Development.json with local dev settings

**Checkpoint**: Solution builds, all projects created with correct dependencies

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Core Layer Foundation

- [X] T013 Create Result<T> and Error types in src/VoiceFlow.Core/Common/Result.cs
- [X] T014 [P] Create ITenantScoped interface in src/VoiceFlow.Core/Common/ITenantScoped.cs
- [X] T015 [P] Create base Entity class with Id property in src/VoiceFlow.Core/Common/Entity.cs
- [X] T016 [P] Create enums: CallDirection, CallStatus, FlowStatus, InvoiceStatus, Sentiment in src/VoiceFlow.Core/Enums/

### Contracts Foundation

- [X] T017 Create ApiResponse<T> envelope in src/VoiceFlow.Contracts/Common/ApiResponse.cs
- [X] T018 [P] Create PagedResponse<T> in src/VoiceFlow.Contracts/Common/PagedResponse.cs
- [X] T019 [P] Create ErrorResponse in src/VoiceFlow.Contracts/Common/ErrorResponse.cs
- [X] T020 [P] Create PaginationRequest in src/VoiceFlow.Contracts/Common/PaginationRequest.cs

### Infrastructure Foundation

- [X] T021 Create MongoDbContext in src/VoiceFlow.Infrastructure/Persistence/MongoDbContext.cs
- [X] T022 Create CollectionBootstrap in src/VoiceFlow.Infrastructure/Persistence/CollectionBootstrap.cs
- [X] T023 [P] Create base repository interface IRepository<T> in src/VoiceFlow.Core/Interfaces/Repositories/IRepository.cs
- [X] T024 Create base MongoRepository<T> in src/VoiceFlow.Infrastructure/Persistence/Repositories/MongoRepository.cs

### Application Foundation

- [X] T025 Create ITenantContext interface in src/VoiceFlow.Application/Common/ITenantContext.cs
- [X] T026 [P] Create ICurrentUser interface in src/VoiceFlow.Application/Common/ICurrentUser.cs

### API Foundation

- [X] T027 Create Program.cs with DI configuration in src/VoiceFlow.Api/Program.cs
- [X] T028 Create TenantContextMiddleware in src/VoiceFlow.Api/Middleware/TenantContextMiddleware.cs
- [X] T029 [P] Create ExceptionHandlingMiddleware in src/VoiceFlow.Api/Middleware/ExceptionHandlingMiddleware.cs
- [X] T030 [P] Create LocalizationMiddleware in src/VoiceFlow.Api/Middleware/LocalizationMiddleware.cs
- [X] T031 [P] Create Messages.en.json in src/VoiceFlow.Api/Resources/Messages.en.json
- [X] T032 [P] Create Messages.ar.json in src/VoiceFlow.Api/Resources/Messages.ar.json
- [X] T033 Create HealthController in src/VoiceFlow.Api/Controllers/HealthController.cs
- [X] T034 Configure Swagger/OpenAPI in Program.cs
- [X] T035 Configure rate limiting middleware in Program.cs
- [X] T036 Create DependencyInjection.cs in src/VoiceFlow.Infrastructure/DependencyInjection.cs

**Checkpoint**: Foundation ready - API starts, health endpoint works, MongoDB connects

---

## Phase 3: User Story 1 - User Authentication (Priority: P1) 🎯 MVP

**Goal**: Users can sign up, log in, manage profile, and authenticate API calls with JWT tokens

**Independent Test**: Create account via POST /api/v1/auth/signup, login via POST /api/v1/auth/login, verify JWT validation on protected endpoint

### Core Layer (US1)

- [X] T037 [P] [US1] Create Account entity in src/VoiceFlow.Core/Entities/Account.cs
- [X] T038 [P] [US1] Create Profile entity in src/VoiceFlow.Core/Entities/Profile.cs
- [X] T039 [P] [US1] Create PhoneNumber value object in src/VoiceFlow.Core/ValueObjects/PhoneNumber.cs
- [X] T040 [P] [US1] Create IAccountRepository interface in src/VoiceFlow.Core/Interfaces/Repositories/IAccountRepository.cs
- [X] T041 [P] [US1] Create IProfileRepository interface in src/VoiceFlow.Core/Interfaces/Repositories/IProfileRepository.cs
- [X] T042 [P] [US1] Create IPasswordHasher interface in src/VoiceFlow.Core/Interfaces/Services/IPasswordHasher.cs
- [X] T043 [P] [US1] Create IJwtTokenService interface in src/VoiceFlow.Core/Interfaces/Services/IJwtTokenService.cs
- [X] T044 [P] [US1] Create IEmailService interface in src/VoiceFlow.Core/Interfaces/Services/IEmailService.cs

### Contracts (US1)

- [X] T045 [P] [US1] Create SignupRequest in src/VoiceFlow.Contracts/Auth/SignupRequest.cs
- [X] T046 [P] [US1] Create LoginRequest in src/VoiceFlow.Contracts/Auth/LoginRequest.cs
- [X] T047 [P] [US1] Create TokenResponse in src/VoiceFlow.Contracts/Auth/TokenResponse.cs
- [X] T048 [P] [US1] Create RefreshTokenRequest in src/VoiceFlow.Contracts/Auth/RefreshTokenRequest.cs
- [X] T049 [P] [US1] Create PasswordRecoveryRequest in src/VoiceFlow.Contracts/Auth/PasswordRecoveryRequest.cs
- [X] T050 [P] [US1] Create ResetPasswordRequest in src/VoiceFlow.Contracts/Auth/ResetPasswordRequest.cs
- [X] T051 [P] [US1] Create UserResponse in src/VoiceFlow.Contracts/Auth/UserResponse.cs
- [X] T052 [P] [US1] Create ProfileResponse in src/VoiceFlow.Contracts/Profiles/ProfileResponse.cs
- [X] T053 [P] [US1] Create UpdateProfileRequest in src/VoiceFlow.Contracts/Profiles/UpdateProfileRequest.cs

### Infrastructure (US1)

- [X] T054 [P] [US1] Create AccountConfiguration in src/VoiceFlow.Infrastructure/Persistence/Configurations/AccountConfiguration.cs
- [X] T055 [P] [US1] Create ProfileConfiguration in src/VoiceFlow.Infrastructure/Persistence/Configurations/ProfileConfiguration.cs
- [X] T056 [P] [US1] Create AccountIndexes in src/VoiceFlow.Infrastructure/Persistence/Indexes/AccountIndexes.cs
- [X] T057 [P] [US1] Create ProfileIndexes in src/VoiceFlow.Infrastructure/Persistence/Indexes/ProfileIndexes.cs
- [X] T058 [US1] Create AccountRepository in src/VoiceFlow.Infrastructure/Persistence/Repositories/AccountRepository.cs
- [X] T059 [US1] Create ProfileRepository in src/VoiceFlow.Infrastructure/Persistence/Repositories/ProfileRepository.cs
- [X] T060 [US1] Create JwtTokenService in src/VoiceFlow.Infrastructure/Auth/JwtTokenService.cs
- [X] T061 [P] [US1] Create PasswordHasher in src/VoiceFlow.Infrastructure/Auth/PasswordHasher.cs
- [X] T062 [P] [US1] Create RefreshTokenStore in src/VoiceFlow.Infrastructure/Auth/RefreshTokenStore.cs
- [X] T063 [P] [US1] Create SmtpEmailService in src/VoiceFlow.Infrastructure/ExternalServices/SmtpEmailService.cs
- [X] T064 [P] [US1] Create AccountMappingProfile in src/VoiceFlow.Infrastructure/Mapping/AccountMappingProfile.cs
- [X] T065 [P] [US1] Create ProfileMappingProfile in src/VoiceFlow.Infrastructure/Mapping/ProfileMappingProfile.cs

### Application Layer (US1)

- [X] T066 [US1] Create IAuthService interface in src/VoiceFlow.Application/Services/IAuthService.cs
- [X] T067 [US1] Create AuthService in src/VoiceFlow.Application/Services/AuthService.cs
- [X] T068 [US1] Create IProfileService interface in src/VoiceFlow.Application/Services/IProfileService.cs
- [X] T069 [US1] Create ProfileService in src/VoiceFlow.Application/Services/ProfileService.cs

### API Layer (US1)

- [X] T070 [US1] Create AuthController in src/VoiceFlow.Api/Controllers/AuthController.cs
- [X] T071 [US1] Create ProfilesController in src/VoiceFlow.Api/Controllers/ProfilesController.cs
- [X] T072 [US1] Configure JWT authentication in Program.cs
- [X] T073 [US1] Register US1 services in DependencyInjection.cs

**Checkpoint**: User Story 1 complete - signup, login, token refresh, password recovery, profile management all functional

---

## Phase 4: User Story 2 - Tenant & Role Management (Priority: P2)

**Goal**: Admins can manage organization settings, create roles, assign permissions

**Independent Test**: Update account settings, create custom role, assign role to user, verify 403 for non-admins

### Core Layer (US2)

- [X] T074 [P] [US2] Create RbacRole entity in src/VoiceFlow.Core/Entities/RbacRole.cs
- [X] T075 [P] [US2] Create RbacUserRole entity in src/VoiceFlow.Core/Entities/RbacUserRole.cs
- [X] T076 [P] [US2] Create Permission value object in src/VoiceFlow.Core/ValueObjects/Permission.cs
- [X] T077 [P] [US2] Create IRbacRoleRepository interface in src/VoiceFlow.Core/Interfaces/Repositories/IRbacRoleRepository.cs
- [X] T078 [P] [US2] Create IRbacUserRoleRepository interface in src/VoiceFlow.Core/Interfaces/Repositories/IRbacUserRoleRepository.cs

### Contracts (US2)

- [X] T079 [P] [US2] Create AccountResponse in src/VoiceFlow.Contracts/Accounts/AccountResponse.cs
- [X] T080 [P] [US2] Create UpdateAccountRequest in src/VoiceFlow.Contracts/Accounts/UpdateAccountRequest.cs
- [X] T081 [P] [US2] Create RoleResponse in src/VoiceFlow.Contracts/Rbac/RoleResponse.cs
- [X] T082 [P] [US2] Create CreateRoleRequest in src/VoiceFlow.Contracts/Rbac/CreateRoleRequest.cs
- [X] T083 [P] [US2] Create UpdateRoleRequest in src/VoiceFlow.Contracts/Rbac/UpdateRoleRequest.cs
- [X] T084 [P] [US2] Create AssignRoleRequest in src/VoiceFlow.Contracts/Rbac/AssignRoleRequest.cs
- [X] T085 [P] [US2] Create UserRoleResponse in src/VoiceFlow.Contracts/Rbac/UserRoleResponse.cs

### Infrastructure (US2)

- [X] T086 [P] [US2] Create RbacRoleConfiguration in src/VoiceFlow.Infrastructure/Persistence/Configurations/RbacRoleConfiguration.cs
- [X] T087 [P] [US2] Create RbacUserRoleConfiguration in src/VoiceFlow.Infrastructure/Persistence/Configurations/RbacUserRoleConfiguration.cs
- [X] T088 [P] [US2] Create RbacRoleIndexes in src/VoiceFlow.Infrastructure/Persistence/Indexes/RbacRoleIndexes.cs
- [X] T089 [P] [US2] Create RbacUserRoleIndexes in src/VoiceFlow.Infrastructure/Persistence/Indexes/RbacUserRoleIndexes.cs
- [X] T090 [US2] Create RbacRoleRepository in src/VoiceFlow.Infrastructure/Persistence/Repositories/RbacRoleRepository.cs
- [X] T091 [US2] Create RbacUserRoleRepository in src/VoiceFlow.Infrastructure/Persistence/Repositories/RbacUserRoleRepository.cs
- [X] T092 [P] [US2] Create RbacRoleMappingProfile in src/VoiceFlow.Infrastructure/Mapping/RbacRoleMappingProfile.cs
- [X] T093 [P] [US2] Create RbacUserRoleMappingProfile in src/VoiceFlow.Infrastructure/Mapping/RbacUserRoleMappingProfile.cs

### Application Layer (US2)

- [X] T094 [US2] Create IAccountService interface in src/VoiceFlow.Application/Services/IAccountService.cs
- [X] T095 [US2] Create AccountService in src/VoiceFlow.Application/Services/AccountService.cs
- [X] T096 [US2] Create IRbacService interface in src/VoiceFlow.Application/Services/IRbacService.cs
- [X] T097 [US2] Create RbacService in src/VoiceFlow.Application/Services/RbacService.cs

### API Layer (US2)

- [X] T098 [US2] Create AccountsController in src/VoiceFlow.Api/Controllers/AccountsController.cs
- [X] T099 [US2] Create RbacController in src/VoiceFlow.Api/Controllers/RbacController.cs
- [X] T100 [US2] Register US2 services in DependencyInjection.cs

**Checkpoint**: User Story 2 complete - account settings, role CRUD, role assignment, admin-only enforcement all functional

---

## Phase 5: User Story 3 - Call Management & AI Features (Priority: P3)

**Goal**: Users can view call history, access recordings, see AI summaries/transcripts/sentiment

**Independent Test**: Create call records, generate signed URL for recording, trigger AI summary generation, search calls with filters

### Core Layer (US3)

- [X] T101 [P] [US3] Create Call entity in src/VoiceFlow.Core/Entities/Call.cs
- [X] T102 [P] [US3] Create ICallRepository interface in src/VoiceFlow.Core/Interfaces/Repositories/ICallRepository.cs
- [X] T103 [P] [US3] Create IStorageService interface in src/VoiceFlow.Core/Interfaces/Services/IStorageService.cs
- [X] T104 [P] [US3] Create IAiGatewayService interface in src/VoiceFlow.Core/Interfaces/Services/IAiGatewayService.cs

### Contracts (US3)

- [X] T105 [P] [US3] Create CallResponse in src/VoiceFlow.Contracts/Calls/CallResponse.cs
- [X] T106 [P] [US3] Create CallListResponse in src/VoiceFlow.Contracts/Calls/CallListResponse.cs
- [X] T107 [P] [US3] Create CreateCallRequest in src/VoiceFlow.Contracts/Calls/CreateCallRequest.cs
- [X] T108 [P] [US3] Create UpdateCallRequest in src/VoiceFlow.Contracts/Calls/UpdateCallRequest.cs
- [X] T109 [P] [US3] Create CallSearchRequest in src/VoiceFlow.Contracts/Calls/CallSearchRequest.cs
- [X] T110 [P] [US3] Create GenerateSummaryRequest in src/VoiceFlow.Contracts/Calls/GenerateSummaryRequest.cs
- [X] T111 [P] [US3] Create TranslateSummaryRequest in src/VoiceFlow.Contracts/Calls/TranslateSummaryRequest.cs
- [X] T112 [P] [US3] Create SignedUrlResponse in src/VoiceFlow.Contracts/Common/SignedUrlResponse.cs

### Infrastructure (US3)

- [X] T113 [P] [US3] Create CallConfiguration in src/VoiceFlow.Infrastructure/Persistence/Configurations/CallConfiguration.cs
- [X] T114 [P] [US3] Create CallIndexes in src/VoiceFlow.Infrastructure/Persistence/Indexes/CallIndexes.cs
- [X] T115 [US3] Create CallRepository in src/VoiceFlow.Infrastructure/Persistence/Repositories/CallRepository.cs
- [X] T116 [US3] Create AzureBlobStorageService in src/VoiceFlow.Infrastructure/ExternalServices/AzureBlobStorageService.cs
- [X] T117 [US3] Create AiGatewayService in src/VoiceFlow.Infrastructure/ExternalServices/AiGatewayService.cs
- [X] T118 [P] [US3] Create CallMappingProfile in src/VoiceFlow.Infrastructure/Mapping/CallMappingProfile.cs

### Application Layer (US3)

- [X] T119 [US3] Create ICallService interface in src/VoiceFlow.Application/Services/ICallService.cs
- [X] T120 [US3] Create CallService in src/VoiceFlow.Application/Services/CallService.cs

### API Layer (US3)

- [X] T121 [US3] Create CallsController in src/VoiceFlow.Api/Controllers/CallsController.cs
- [X] T122 [US3] Register US3 services in DependencyInjection.cs
- [X] T123 [US3] Configure Polly circuit breaker for AI Gateway in Program.cs

**Checkpoint**: User Story 3 complete - call CRUD, recording URLs, AI summary/transcript/sentiment, search/filter/pagination all functional

---

## Phase 6: User Story 4 - IVR Flow Management (Priority: P4)

**Goal**: Users can create, edit, validate, publish IVR flows and export to Asterisk formats

**Independent Test**: Create flow with nodes/edges, validate flow, publish to extension, export to extensions.conf format

### Core Layer (US4)

- [X] T124 [P] [US4] Create Flow entity in src/VoiceFlow.Core/Entities/Flow.cs
- [X] T125 [P] [US4] Create FlowNode value object in src/VoiceFlow.Core/ValueObjects/FlowNode.cs
- [X] T126 [P] [US4] Create FlowEdge value object in src/VoiceFlow.Core/ValueObjects/FlowEdge.cs
- [X] T127 [P] [US4] Create IFlowRepository interface in src/VoiceFlow.Core/Interfaces/Repositories/IFlowRepository.cs

### Contracts (US4)

- [X] T128 [P] [US4] Create FlowResponse in src/VoiceFlow.Contracts/Flows/FlowResponse.cs
- [X] T129 [P] [US4] Create CreateFlowRequest in src/VoiceFlow.Contracts/Flows/CreateFlowRequest.cs
- [X] T130 [P] [US4] Create UpdateFlowRequest in src/VoiceFlow.Contracts/Flows/UpdateFlowRequest.cs
- [X] T131 [P] [US4] Create PublishFlowRequest in src/VoiceFlow.Contracts/Flows/PublishFlowRequest.cs
- [X] T132 [P] [US4] Create FlowValidationResponse in src/VoiceFlow.Contracts/Flows/FlowValidationResponse.cs
- [X] T133 [P] [US4] Create FlowExportResponse in src/VoiceFlow.Contracts/Flows/FlowExportResponse.cs
- [X] T134 [P] [US4] Create FlowNodeDto in src/VoiceFlow.Contracts/Flows/FlowNodeDto.cs
- [X] T135 [P] [US4] Create FlowEdgeDto in src/VoiceFlow.Contracts/Flows/FlowEdgeDto.cs

### Infrastructure (US4)

- [X] T136 [P] [US4] Create FlowConfiguration in src/VoiceFlow.Infrastructure/Persistence/Configurations/FlowConfiguration.cs
- [X] T137 [P] [US4] Create FlowIndexes in src/VoiceFlow.Infrastructure/Persistence/Indexes/FlowIndexes.cs
- [X] T138 [US4] Create FlowRepository in src/VoiceFlow.Infrastructure/Persistence/Repositories/FlowRepository.cs
- [X] T139 [P] [US4] Create FlowMappingProfile in src/VoiceFlow.Infrastructure/Mapping/FlowMappingProfile.cs

### Application Layer (US4)

- [X] T140 [US4] Create FlowValidator in src/VoiceFlow.Application/Validators/FlowValidator.cs
- [X] T141 [US4] Create AsteriskExporter in src/VoiceFlow.Application/Exporters/AsteriskExporter.cs
- [X] T142 [US4] Create IFlowService interface in src/VoiceFlow.Application/Services/IFlowService.cs
- [X] T143 [US4] Create FlowService in src/VoiceFlow.Application/Services/FlowService.cs

### API Layer (US4)

- [X] T144 [US4] Create FlowsController in src/VoiceFlow.Api/Controllers/FlowsController.cs
- [X] T145 [US4] Register US4 services in DependencyInjection.cs

**Checkpoint**: User Story 4 complete - flow CRUD, validation, publish, Asterisk export all functional

---

## Phase 7: User Story 5 - Voice Library Management (Priority: P5)

**Goal**: Users can upload audio files, generate TTS prompts, get signed URLs for voice files

**Independent Test**: Upload WAV file, generate TTS prompt, retrieve signed URL

### Core Layer (US5)

- [X] T146 [P] [US5] Create VoiceLibraryItem entity in src/VoiceFlow.Core/Entities/VoiceLibraryItem.cs
- [X] T147 [P] [US5] Create IVoiceLibraryRepository interface in src/VoiceFlow.Core/Interfaces/Repositories/IVoiceLibraryRepository.cs
- [X] T148 [P] [US5] Create ITtsService interface in src/VoiceFlow.Core/Interfaces/Services/ITtsService.cs

### Contracts (US5)

- [X] T149 [P] [US5] Create VoiceLibraryItemResponse in src/VoiceFlow.Contracts/VoiceLibrary/VoiceLibraryItemResponse.cs
- [X] T150 [P] [US5] Create CreateVoiceLibraryItemRequest in src/VoiceFlow.Contracts/VoiceLibrary/CreateVoiceLibraryItemRequest.cs
- [X] T151 [P] [US5] Create GenerateTtsRequest in src/VoiceFlow.Contracts/VoiceLibrary/GenerateTtsRequest.cs
- [X] T152 [P] [US5] Create UpdateVoiceLibraryItemRequest in src/VoiceFlow.Contracts/VoiceLibrary/UpdateVoiceLibraryItemRequest.cs

### Infrastructure (US5)

- [X] T153 [P] [US5] Create VoiceLibraryItemConfiguration in src/VoiceFlow.Infrastructure/Persistence/Configurations/VoiceLibraryItemConfiguration.cs
- [X] T154 [P] [US5] Create VoiceLibraryItemIndexes in src/VoiceFlow.Infrastructure/Persistence/Indexes/VoiceLibraryItemIndexes.cs
- [X] T155 [US5] Create VoiceLibraryRepository in src/VoiceFlow.Infrastructure/Persistence/Repositories/VoiceLibraryRepository.cs
- [X] T156 [US5] Create TtsService in src/VoiceFlow.Infrastructure/ExternalServices/TtsService.cs
- [X] T157 [P] [US5] Create VoiceLibraryItemMappingProfile in src/VoiceFlow.Infrastructure/Mapping/VoiceLibraryItemMappingProfile.cs

### Application Layer (US5)

- [X] T158 [US5] Create IVoiceLibraryService interface in src/VoiceFlow.Application/Services/IVoiceLibraryService.cs
- [X] T159 [US5] Create VoiceLibraryService in src/VoiceFlow.Application/Services/VoiceLibraryService.cs

### API Layer (US5)

- [X] T160 [US5] Create VoiceLibraryController in src/VoiceFlow.Api/Controllers/VoiceLibraryController.cs
- [X] T161 [US5] Register US5 services in DependencyInjection.cs

**Checkpoint**: User Story 5 complete - upload, TTS generation, signed URLs, metadata capture all functional

---

## Phase 8: User Story 6 - Contacts & Tags (Priority: P6)

**Goal**: Users can manage contacts and tags, apply tags to calls/contacts

**Independent Test**: Create contact, create tag, apply tag to contact, search by tag

### Core Layer (US6)

- [X] T162 [P] [US6] Create Contact entity in src/VoiceFlow.Core/Entities/Contact.cs
- [X] T163 [P] [US6] Create Tag entity in src/VoiceFlow.Core/Entities/Tag.cs
- [X] T164 [P] [US6] Create IContactRepository interface in src/VoiceFlow.Core/Interfaces/Repositories/IContactRepository.cs
- [X] T165 [P] [US6] Create ITagRepository interface in src/VoiceFlow.Core/Interfaces/Repositories/ITagRepository.cs

### Contracts (US6)

- [X] T166 [P] [US6] Create ContactResponse in src/VoiceFlow.Contracts/Contacts/ContactResponse.cs
- [X] T167 [P] [US6] Create CreateContactRequest in src/VoiceFlow.Contracts/Contacts/CreateContactRequest.cs
- [X] T168 [P] [US6] Create UpdateContactRequest in src/VoiceFlow.Contracts/Contacts/UpdateContactRequest.cs
- [X] T169 [P] [US6] Create ContactSearchRequest in src/VoiceFlow.Contracts/Contacts/ContactSearchRequest.cs
- [X] T170 [P] [US6] Create TagResponse in src/VoiceFlow.Contracts/Tags/TagResponse.cs
- [X] T171 [P] [US6] Create CreateTagRequest in src/VoiceFlow.Contracts/Tags/CreateTagRequest.cs
- [X] T172 [P] [US6] Create UpdateTagRequest in src/VoiceFlow.Contracts/Tags/UpdateTagRequest.cs

### Infrastructure (US6)

- [X] T173 [P] [US6] Create ContactConfiguration in src/VoiceFlow.Infrastructure/Persistence/Configurations/ContactConfiguration.cs
- [X] T174 [P] [US6] Create TagConfiguration in src/VoiceFlow.Infrastructure/Persistence/Configurations/TagConfiguration.cs
- [X] T175 [P] [US6] Create ContactIndexes in src/VoiceFlow.Infrastructure/Persistence/Indexes/ContactIndexes.cs
- [X] T176 [P] [US6] Create TagIndexes in src/VoiceFlow.Infrastructure/Persistence/Indexes/TagIndexes.cs
- [X] T177 [US6] Create ContactRepository in src/VoiceFlow.Infrastructure/Persistence/Repositories/ContactRepository.cs
- [X] T178 [US6] Create TagRepository in src/VoiceFlow.Infrastructure/Persistence/Repositories/TagRepository.cs
- [X] T179 [P] [US6] Create ContactMappingProfile in src/VoiceFlow.Infrastructure/Mapping/ContactMappingProfile.cs
- [X] T180 [P] [US6] Create TagMappingProfile in src/VoiceFlow.Infrastructure/Mapping/TagMappingProfile.cs

### Application Layer (US6)

- [X] T181 [US6] Create IContactService interface in src/VoiceFlow.Application/Services/IContactService.cs
- [X] T182 [US6] Create ContactService in src/VoiceFlow.Application/Services/ContactService.cs
- [X] T183 [US6] Create ITagService interface in src/VoiceFlow.Application/Services/ITagService.cs
- [X] T184 [US6] Create TagService in src/VoiceFlow.Application/Services/TagService.cs

### API Layer (US6)

- [X] T185 [US6] Create ContactsController in src/VoiceFlow.Api/Controllers/ContactsController.cs
- [X] T186 [US6] Create TagsController in src/VoiceFlow.Api/Controllers/TagsController.cs
- [X] T187 [US6] Register US6 services in DependencyInjection.cs

**Checkpoint**: User Story 6 complete - contact CRUD, tag CRUD, tagging, search by tags all functional

---

## Phase 9: User Story 7 - SIP & Softphone Configuration (Priority: P7)

**Goal**: Users can configure SIP credentials, view softphone call logs

**Independent Test**: Create SIP account, retrieve configuration, create softphone call log entry

### Core Layer (US7)

- [X] T188 [P] [US7] Create SipAccount entity in src/VoiceFlow.Core/Entities/SipAccount.cs
- [X] T189 [P] [US7] Create SoftphoneCallLog entity in src/VoiceFlow.Core/Entities/SoftphoneCallLog.cs
- [X] T190 [P] [US7] Create ISipAccountRepository interface in src/VoiceFlow.Core/Interfaces/Repositories/ISipAccountRepository.cs
- [X] T191 [P] [US7] Create ISoftphoneCallLogRepository interface in src/VoiceFlow.Core/Interfaces/Repositories/ISoftphoneCallLogRepository.cs

### Contracts (US7)

- [X] T192 [P] [US7] Create SipAccountResponse in src/VoiceFlow.Contracts/SipAccounts/SipAccountResponse.cs
- [X] T193 [P] [US7] Create CreateSipAccountRequest in src/VoiceFlow.Contracts/SipAccounts/CreateSipAccountRequest.cs
- [X] T194 [P] [US7] Create UpdateSipAccountRequest in src/VoiceFlow.Contracts/SipAccounts/UpdateSipAccountRequest.cs
- [X] T195 [P] [US7] Create SoftphoneCallLogResponse in src/VoiceFlow.Contracts/SipAccounts/SoftphoneCallLogResponse.cs
- [X] T196 [P] [US7] Create CreateSoftphoneCallLogRequest in src/VoiceFlow.Contracts/SipAccounts/CreateSoftphoneCallLogRequest.cs

### Infrastructure (US7)

- [X] T197 [P] [US7] Create SipAccountConfiguration in src/VoiceFlow.Infrastructure/Persistence/Configurations/SipAccountConfiguration.cs
- [X] T198 [P] [US7] Create SoftphoneCallLogConfiguration in src/VoiceFlow.Infrastructure/Persistence/Configurations/SoftphoneCallLogConfiguration.cs
- [X] T199 [P] [US7] Create SipAccountIndexes in src/VoiceFlow.Infrastructure/Persistence/Indexes/SipAccountIndexes.cs
- [X] T200 [P] [US7] Create SoftphoneCallLogIndexes in src/VoiceFlow.Infrastructure/Persistence/Indexes/SoftphoneCallLogIndexes.cs
- [X] T201 [US7] Create SipAccountRepository in src/VoiceFlow.Infrastructure/Persistence/Repositories/SipAccountRepository.cs
- [X] T202 [US7] Create SoftphoneCallLogRepository in src/VoiceFlow.Infrastructure/Persistence/Repositories/SoftphoneCallLogRepository.cs
- [X] T203 [P] [US7] Create SipAccountMappingProfile in src/VoiceFlow.Infrastructure/Mapping/SipAccountMappingProfile.cs
- [X] T204 [P] [US7] Create SoftphoneCallLogMappingProfile in src/VoiceFlow.Infrastructure/Mapping/SoftphoneCallLogMappingProfile.cs

### Application Layer (US7)

- [X] T205 [US7] Create ISipAccountService interface in src/VoiceFlow.Application/Services/ISipAccountService.cs
- [X] T206 [US7] Create SipAccountService in src/VoiceFlow.Application/Services/SipAccountService.cs

### API Layer (US7)

- [X] T207 [US7] Create SipAccountsController in src/VoiceFlow.Api/Controllers/SipAccountsController.cs
- [X] T208 [US7] Register US7 services in DependencyInjection.cs

**Checkpoint**: User Story 7 complete - SIP account CRUD, softphone call logs, secure credential handling all functional

---

## Phase 10: User Story 8 - Billing & Invoices (Priority: P8)

**Goal**: Admins can manage billing info and view invoices

**Independent Test**: Update billing details, create invoice, list invoices, verify admin-only access

### Core Layer (US8)

- [X] T209 [P] [US8] Create Invoice entity in src/VoiceFlow.Core/Entities/Invoice.cs
- [X] T210 [P] [US8] Create IInvoiceRepository interface in src/VoiceFlow.Core/Interfaces/Repositories/IInvoiceRepository.cs

### Contracts (US8)

- [X] T211 [P] [US8] Create InvoiceResponse in src/VoiceFlow.Contracts/Invoices/InvoiceResponse.cs
- [X] T212 [P] [US8] Create CreateInvoiceRequest in src/VoiceFlow.Contracts/Invoices/CreateInvoiceRequest.cs
- [X] T213 [P] [US8] Create UpdateInvoiceRequest in src/VoiceFlow.Contracts/Invoices/UpdateInvoiceRequest.cs

### Infrastructure (US8)

- [X] T214 [P] [US8] Create InvoiceConfiguration in src/VoiceFlow.Infrastructure/Persistence/Configurations/InvoiceConfiguration.cs
- [X] T215 [P] [US8] Create InvoiceIndexes in src/VoiceFlow.Infrastructure/Persistence/Indexes/InvoiceIndexes.cs
- [X] T216 [US8] Create InvoiceRepository in src/VoiceFlow.Infrastructure/Persistence/Repositories/InvoiceRepository.cs
- [X] T217 [P] [US8] Create InvoiceMappingProfile in src/VoiceFlow.Infrastructure/Mapping/InvoiceMappingProfile.cs

### Application Layer (US8)

- [X] T218 [US8] Create IInvoiceService interface in src/VoiceFlow.Application/Services/IInvoiceService.cs
- [X] T219 [US8] Create InvoiceService in src/VoiceFlow.Application/Services/InvoiceService.cs

### API Layer (US8)

- [X] T220 [US8] Create InvoicesController in src/VoiceFlow.Api/Controllers/InvoicesController.cs
- [X] T221 [US8] Register US8 services in DependencyInjection.cs

**Checkpoint**: User Story 8 complete - invoice CRUD, billing updates, admin authorization all functional

---

## Phase 11: User Story 9 - Audit Trail (Priority: P9)

**Goal**: Admins can view audit trail of entity changes

**Independent Test**: Make entity change, verify edit log created, query edit logs

### Core Layer (US9)

- [X] T222 [P] [US9] Create EditLog entity in src/VoiceFlow.Core/Entities/EditLog.cs
- [X] T223 [P] [US9] Create IEditLogRepository interface in src/VoiceFlow.Core/Interfaces/Repositories/IEditLogRepository.cs

### Contracts (US9)

- [X] T224 [P] [US9] Create EditLogResponse in src/VoiceFlow.Contracts/EditLogs/EditLogResponse.cs
- [X] T225 [P] [US9] Create EditLogSearchRequest in src/VoiceFlow.Contracts/EditLogs/EditLogSearchRequest.cs

### Infrastructure (US9)

- [X] T226 [P] [US9] Create EditLogConfiguration in src/VoiceFlow.Infrastructure/Persistence/Configurations/EditLogConfiguration.cs
- [X] T227 [P] [US9] Create EditLogIndexes in src/VoiceFlow.Infrastructure/Persistence/Indexes/EditLogIndexes.cs
- [X] T228 [US9] Create EditLogRepository in src/VoiceFlow.Infrastructure/Persistence/Repositories/EditLogRepository.cs
- [X] T229 [P] [US9] Create EditLogMappingProfile in src/VoiceFlow.Infrastructure/Mapping/EditLogMappingProfile.cs

### Application Layer (US9)

- [X] T230 [US9] Create IEditLogService interface in src/VoiceFlow.Application/Services/IEditLogService.cs
- [X] T231 [US9] Create EditLogService in src/VoiceFlow.Application/Services/EditLogService.cs
- [X] T232 [US9] Integrate edit log creation into all entity services (Account, Profile, Flow, etc.)

### API Layer (US9)

- [X] T233 [US9] Create EditLogsController in src/VoiceFlow.Api/Controllers/EditLogsController.cs
- [X] T234 [US9] Register US9 services in DependencyInjection.cs

**Checkpoint**: User Story 9 complete - edit log creation, query, admin-only access all functional

---

## Phase 12: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements that affect multiple user stories

- [X] T235 [P] Verify all Swagger documentation is complete and accurate
- [X] T236 [P] Review and update all localization messages (en + ar)
- [X] T237 [P] Create Dockerfile for containerized deployment
- [X] T238 [P] Create docker-compose.yml with MongoDB for local development
- [X] T239 Verify tenant isolation across all repositories (negative-path verification)
- [X] T240 Performance review: ensure all large collections have proper pagination
- [X] T241 [P] Update README.md with project documentation
- [X] T242 Run quickstart.md validation to ensure setup instructions work
- [X] T243 Final code cleanup and consistent formatting

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-11)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → ... → P9)
- **Polish (Phase 12)**: Depends on all user stories being complete

### User Story Dependencies

| Story | Priority | Can Start After | Notes |
|-------|----------|-----------------|-------|
| US1 (Auth) | P1 | Phase 2 | No dependencies on other stories |
| US2 (RBAC) | P2 | Phase 2 | Independent, uses Account from US1 |
| US3 (Calls) | P3 | Phase 2 | Uses storage service from Phase 2 |
| US4 (Flows) | P4 | Phase 2 | May reference VoiceLibrary but can stub |
| US5 (Voice) | P5 | Phase 2 | Independent |
| US6 (Contacts) | P6 | Phase 2 | Independent |
| US7 (SIP) | P7 | Phase 2 | Independent |
| US8 (Billing) | P8 | Phase 2 | Uses Account from US1 |
| US9 (Audit) | P9 | Phase 2 | Integrates with all services |

### Parallel Opportunities

Within each phase, tasks marked [P] can run in parallel:
- Phase 1: T003-T006, T008-T012
- Phase 2: T014-T016, T017-T020, T023, T025-T026, T029-T032
- Each User Story: Most entity/contract/configuration tasks are parallelizable

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (Auth)
4. **STOP and VALIDATE**: Test signup, login, JWT validation
5. Deploy/demo if ready — this is a functional authentication API

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 (Auth) → **MVP** — users can authenticate
3. US2 (RBAC) → Admin permissions work
4. US3 (Calls) → Core business value delivered
5. US4-US9 → Additional features in priority order

### Parallel Team Strategy

With 3+ developers:
1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: US1 (Auth) then US3 (Calls)
   - Developer B: US2 (RBAC) then US4 (Flows)
   - Developer C: US5 (Voice) then US6 (Contacts)
3. Remaining stories assigned as capacity allows

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Tests were NOT explicitly requested in the spec — omitted per skill rules
