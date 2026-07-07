# Feature Specification: API Backend Migration

**Feature Branch**: `001-api-backend-migration`  
**Created**: 2026-05-11  
**Status**: Draft  
**Input**: Build .NET MongoDB backend to replace Supabase backend for VoiceFlow Studio call center platform

## User Scenarios & Testing *(mandatory)*

### User Story 1 - User Authentication (Priority: P1)

A user needs to sign up, log in, manage their profile, and have their session properly authenticated across all API calls. The frontend currently uses Supabase GoTrue for authentication; the new backend must provide equivalent functionality with JWT tokens.

**Why this priority**: Authentication is the foundation — no other feature works without it. Users cannot access any protected resources without authentication.

**Independent Test**: Can be fully tested by creating a user account, logging in, and verifying JWT token validation. Delivers immediate value by enabling secure access to the platform.

**Acceptance Scenarios**:

1. **Given** a new user with valid email and password, **When** they submit the signup form, **Then** the system creates their account, provisions a new tenant, assigns them the Owner role, and returns a valid JWT token.
2. **Given** an existing user with valid credentials, **When** they log in, **Then** the system returns a JWT access token (RS256) containing user ID, tenant ID, and role claims.
3. **Given** a user with an expired access token, **When** they request a token refresh, **Then** the system issues a new access token if the refresh token is valid.
4. **Given** a logged-in user, **When** they request their profile, **Then** the system returns their profile data scoped to their tenant.
5. **Given** a user who forgot their password, **When** they request password recovery, **Then** the system sends a password reset email with a secure link.

---

### User Story 2 - Tenant & Role Management (Priority: P2)

Tenant administrators need to manage organization settings, create and assign roles, and control user permissions across all platform modules. The RBAC system must support the existing permission model with modules and actions.

**Why this priority**: Multi-tenancy and RBAC are core infrastructure that all other features depend on for data isolation and authorization.

**Independent Test**: Can be tested by creating roles with different permissions and verifying users can only access authorized modules/actions.

**Acceptance Scenarios**:

1. **Given** a tenant administrator, **When** they update account settings, **Then** the system persists the changes and returns the updated settings.
2. **Given** a tenant administrator, **When** they create a custom role with specific module permissions, **Then** the system saves the role and it becomes available for assignment.
3. **Given** a tenant administrator, **When** they assign a role to a user, **Then** the user's effective permissions reflect the assigned role.
4. **Given** a user without admin privileges, **When** they attempt to modify roles or settings, **Then** the system returns a 403 Forbidden response.
5. **Given** a system role (is_system = true), **When** an administrator attempts to delete it, **Then** the system rejects the deletion.

---

### User Story 3 - Call Management & AI Features (Priority: P3)

Call center agents and supervisors need to view call history, access recordings, read transcripts, and see AI-generated summaries with sentiment analysis. The system must support filtering, pagination, and search across call records.

**Why this priority**: Call management is the core business value — this is what call center users do every day.

**Independent Test**: Can be tested by creating call records, attaching recordings, generating AI summaries, and verifying retrieval with filters.

**Acceptance Scenarios**:

1. **Given** a user in a tenant, **When** they request call history, **Then** the system returns only calls belonging to their tenant with proper pagination.
2. **Given** a call with a recording, **When** a user requests the recording URL, **Then** the system returns a time-limited signed URL.
3. **Given** a completed call, **When** the system processes it through AI, **Then** it generates a transcript, summary, and sentiment analysis stored with the call record.
4. **Given** a call summary in Arabic, **When** a user requests translation to English, **Then** the system returns the translated summary.
5. **Given** call search criteria (date range, status, direction, tags), **When** a user searches, **Then** the system returns matching calls efficiently.

---

### User Story 4 - IVR Flow Management (Priority: P4)

IVR designers need to create, edit, validate, and publish IVR flows. Flows are visual node-based graphs that can be exported to Asterisk configuration formats for deployment.

**Why this priority**: IVR flows control the customer call experience and are a key differentiator of the platform.

**Independent Test**: Can be tested by creating a flow with nodes/edges, validating it, and exporting to Asterisk format.

**Acceptance Scenarios**:

1. **Given** a user with IVR permissions, **When** they create a new flow with nodes and edges, **Then** the system saves the flow in draft status.
2. **Given** a draft flow, **When** a user requests validation, **Then** the system checks for orphan nodes, valid node types, and required fields.
3. **Given** a valid flow, **When** a user publishes it and assigns an extension, **Then** the flow becomes active for that extension.
4. **Given** a published flow, **When** a user exports it, **Then** the system generates valid Asterisk extensions.conf or ARA SQL format.
5. **Given** an IVR node referencing a voice prompt, **When** the flow is validated, **Then** the system verifies the voice library entry exists.

---

### User Story 5 - Voice Library Management (Priority: P5)

Users need to upload audio files or generate TTS prompts for use in IVR flows. Voice prompts must support Arabic and English languages.

**Why this priority**: Voice prompts are required for IVR flows to function — depends on P4 being useful.

**Independent Test**: Can be tested by uploading an audio file, generating a TTS prompt, and retrieving signed URLs.

**Acceptance Scenarios**:

1. **Given** a user with voice library permissions, **When** they upload an audio file (WAV/MP3/OGG), **Then** the system stores it and captures metadata (duration, format).
2. **Given** text and language selection, **When** a user requests TTS generation, **Then** the system creates an audio file from the text.
3. **Given** a voice library entry, **When** a user requests access, **Then** the system returns a time-limited signed URL.
4. **Given** voice prompts in the library, **When** they are listed, **Then** users see only prompts belonging to their tenant.

---

### User Story 6 - Contacts & Tags (Priority: P6)

Users need to manage a phonebook of contacts and organize both contacts and calls using tags/labels.

**Why this priority**: Contact management enhances call handling efficiency but is not blocking for core functionality.

**Independent Test**: Can be tested by creating contacts, applying tags, and filtering calls/contacts by tags.

**Acceptance Scenarios**:

1. **Given** a user, **When** they create a contact with phone, email, and company, **Then** the system saves it scoped to their tenant.
2. **Given** a user, **When** they create a tag with label and color, **Then** the tag becomes available for use on calls and contacts.
3. **Given** a call or contact, **When** a user applies tags, **Then** the tags are associated and searchable.
4. **Given** search criteria including tags, **When** a user searches contacts, **Then** matching results are returned.

---

### User Story 7 - SIP & Softphone Configuration (Priority: P7)

Users need SIP credentials configured for WebRTC softphone functionality. The system must store per-user SIP accounts with STUN/TURN configuration.

**Why this priority**: Required for browser-based calling but can be implemented after core call management.

**Independent Test**: Can be tested by configuring SIP credentials and verifying WebRTC connection parameters.

**Acceptance Scenarios**:

1. **Given** a tenant administrator, **When** they configure SIP account for a user, **Then** the credentials are stored securely (never logged or exposed).
2. **Given** a user with SIP credentials, **When** they request their SIP configuration, **Then** the system returns WS URL, STUN/TURN servers, and auth details.
3. **Given** a user making softphone calls, **When** a call completes, **Then** a softphone call log entry is created.

---

### User Story 8 - Billing & Invoices (Priority: P8)

Tenant administrators need to manage billing information and view invoices.

**Why this priority**: Important for business operations but not blocking core call center functionality.

**Independent Test**: Can be tested by creating invoices, updating billing details, and retrieving invoice history.

**Acceptance Scenarios**:

1. **Given** a tenant administrator, **When** they update billing information, **Then** the system persists the billing details.
2. **Given** an invoice created for a tenant, **When** an administrator views invoices, **Then** they see all invoices for their tenant.
3. **Given** a non-admin user, **When** they attempt to modify invoices, **Then** the system returns 403 Forbidden.

---

### User Story 9 - Audit Trail (Priority: P9)

Administrators need to view an audit trail of changes made to entities across the system.

**Why this priority**: Important for compliance but not blocking core functionality.

**Independent Test**: Can be tested by making changes to entities and verifying edit logs are created.

**Acceptance Scenarios**:

1. **Given** any create/update/delete operation on tracked entities, **When** the operation completes, **Then** an edit log entry is created.
2. **Given** an administrator, **When** they query edit logs, **Then** they see changes within their tenant.

---

### Edge Cases

- What happens when a user signs up with an email that already exists? → Return appropriate error message
- What happens when a tenant's last admin tries to delete their own admin role? → Prevent the action
- How does the system handle concurrent edits to the same IVR flow? → Last-write-wins with updated_at timestamp
- What happens when referenced voice library entry is deleted while used in a flow? → Validation fails on flow save
- How are orphaned recordings handled when calls are deleted? → Calls cannot be deleted (no DELETE policy)

## Requirements *(mandatory)*

### Functional Requirements

**Authentication & Users**
- **FR-001**: System MUST authenticate users via email/password and issue JWT tokens (RS256)
- **FR-002**: System MUST support user signup with automatic tenant provisioning
- **FR-003**: System MUST support password recovery via email
- **FR-004**: System MUST validate JWT tokens on every authenticated request
- **FR-005**: System MUST support token refresh for session extension
- **FR-006**: System MUST allow administrators to create users within their tenant

**Multi-Tenancy & RBAC**
- **FR-010**: System MUST isolate all data by tenant_id at the repository level
- **FR-011**: System MUST support custom roles with module/action permissions
- **FR-012**: System MUST enforce RBAC on all protected operations
- **FR-013**: System MUST prevent deletion of system roles
- **FR-014**: System MUST allow only tenant administrators to manage roles and settings

**Call Management**
- **FR-020**: System MUST store call detail records with all metadata (direction, status, duration, recordings, etc.)
- **FR-021**: System MUST support call search with filtering by date, status, direction, tags
- **FR-022**: System MUST support pagination for call history (cursor or offset-based)
- **FR-023**: System MUST generate signed URLs for recording access
- **FR-024**: System MUST integrate with AI Gateway for transcript/summary/sentiment generation
- **FR-025**: System MUST support summary translation between Arabic and English

**IVR Flows**
- **FR-030**: System MUST store IVR flows as JSON graphs (nodes/edges) compatible with React Flow
- **FR-031**: System MUST validate flows before publishing (no orphan nodes, valid types, required fields)
- **FR-032**: System MUST support flow export to Asterisk extensions.conf format
- **FR-033**: System MUST support flow export to ARA MySQL SQL format
- **FR-034**: System MUST track flow changes in edit logs

**Voice Library**
- **FR-040**: System MUST accept audio file uploads (WAV, MP3, OGG)
- **FR-041**: System MUST capture audio metadata on upload (duration, sample rate)
- **FR-042**: System MUST support TTS generation for voice prompts
- **FR-043**: System MUST generate signed URLs for voice file access

**Contacts & Tags**
- **FR-050**: System MUST support CRUD operations on contacts
- **FR-051**: System MUST support CRUD operations on tags
- **FR-052**: System MUST allow tagging calls and contacts

**SIP & Softphone**
- **FR-060**: System MUST store per-user SIP credentials securely
- **FR-061**: System MUST provide SIP configuration with STUN/TURN details
- **FR-062**: System MUST log softphone call sessions

**Billing**
- **FR-070**: System MUST support invoice CRUD for administrators
- **FR-071**: System MUST store billing details per tenant

**Audit**
- **FR-080**: System MUST create edit log entries for entity changes
- **FR-081**: System MUST restrict edit log modifications to administrators

**API Standards**
- **FR-090**: All endpoints MUST return consistent ApiResponse envelope
- **FR-091**: All endpoints MUST be documented in OpenAPI/Swagger
- **FR-092**: All error responses MUST include localized messages (Arabic/English)

### Key Entities

- **Account (Tenant)**: Organization-level settings, billing info, phone numbers, dialer config
- **Profile**: User information linked to auth user and tenant
- **RbacRole**: Role definitions with permissions JSON (module → actions mapping)
- **RbacUserRole**: User-to-role assignments within a tenant
- **Call**: Call detail record with recordings, transcripts, summaries, sentiment
- **Flow**: IVR flow definition with nodes/edges graph structure
- **Contact**: Phonebook entry with phone, email, company, tags
- **Tag**: Label with color for organizing calls/contacts
- **VoiceLibrary**: Audio prompts (uploaded or TTS-generated)
- **SipAccount**: Per-user SIP/WebRTC credentials
- **SoftphoneCallLog**: Browser-based call session history
- **Invoice**: Billing invoice with status and PDF URL
- **EditLog**: Audit trail entry for entity changes

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Frontend can complete user signup and login flow in under 3 seconds
- **SC-002**: All API endpoints return responses within 500ms for typical payloads
- **SC-003**: Call history loads first page of 50 records in under 1 second
- **SC-004**: System supports 100 concurrent users per tenant without degradation
- **SC-005**: Zero cross-tenant data leakage verified by automated tests
- **SC-006**: 100% of existing frontend functionality works with new backend
- **SC-007**: AI features (transcript, summary, sentiment) complete within 30 seconds per call
- **SC-008**: Voice file upload completes within 10 seconds for files under 10MB
- **SC-009**: IVR flow validation completes within 2 seconds for flows with up to 100 nodes
- **SC-010**: All API responses include proper localized error messages

## Assumptions

- The existing Lovable frontend will require minimal changes (primarily API base URL and auth header format)
- AI Gateway integration will use the same or similar API contract as the current Lovable AI Gateway
- Cloud storage provider (Azure Blob, S3, etc.) will be configured separately — the API is storage-agnostic
- MongoDB Atlas or equivalent managed MongoDB service will be used for production
- The frontend handles IVR flow editing UI — the backend only stores and validates the graph structure
- Call recordings are created by external PBX/telephony systems and uploaded to storage
- TTS generation will use a third-party service (specific provider to be determined)
- Email sending for password recovery will use a configured SMTP service or email provider
- The system will initially support a single MongoDB database; multi-database support is optional
- Performance targets assume standard web hosting infrastructure (not edge/serverless constraints)
