# Data Model: API Backend Migration

**Feature**: 001-api-backend-migration  
**Date**: 2026-05-11

## Overview

All entities use `string` IDs stored as MongoDB ObjectId. All tenant-scoped entities include `TenantId` field for multi-tenant isolation at the repository level.

## Entities

### Account (Tenant)

The canonical tenant entity. `Id` is the `TenantId` used by all other tenant-scoped entities.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Id | string | Yes | ObjectId, canonical TenantId |
| UserId | string | Yes | Owner's user ID |
| OrgName | string | Yes | Organization name |
| AutoAnswer | bool | Yes | Default: true |
| AutoAnswerSecs | int | Yes | Default: 30 |
| ParamName | string | Yes | Default: "caller" |
| DialerUrl | string | Yes | Default: "" |
| DialerMethod | string | Yes | Default: "GET" |
| WaitTime | int | Yes | Default: 30 |
| IvrTimeout | int | Yes | Default: 30 |
| LimitIvr | bool | Yes | Default: true |
| OutboundRingLimit | bool | Yes | Default: false |
| InternalTimeout | bool | Yes | Default: false |
| AcwIn | bool | Yes | After-call work inbound |
| AcwOut | bool | Yes | After-call work outbound |
| AutoAssign | bool | Yes | Default: false |
| AllowReject | bool | Yes | Default: true |
| AllowTransferAway | bool | Yes | Default: true |
| NotifyOnAgentChanges | bool | Yes | Default: true |
| SendInvoicesToAdmins | bool | Yes | Default: true |
| BillingEmails | string | Yes | Default: "" |
| InvoiceName | string | Yes | Default: "<default>" |
| RegistrationNumber | string | Yes | Default: "" |
| VatNumber | string | Yes | Default: "" |
| BillingCountry | string | Yes | Default: "" |
| BillingAddress | string | Yes | Default: "" |
| PaymentMethods | List\<object\> | Yes | JSON array |
| PhoneNumbers | List\<PhoneNumber\> | Yes | JSON array |
| ShowInbound | bool | Yes | Default: false |
| DefaultCountry | string | Yes | Default: "SA" |
| AutoTagging | bool | Yes | Default: true |
| CallTags | string | Yes | Default: "" |
| Domains | string | Yes | Default: "" |
| AwayStatus | string | Yes | Default: "Away" |
| NumberFormat | string | Yes | Default: "intl-no-prefix" |
| CreatedAt | DateTime | Yes | UTC timestamp |
| UpdatedAt | DateTime | Yes | UTC timestamp |

**Indexes**:
- `_id` (primary)
- `UserId` (for owner lookup)

---

### Profile

User profile linked to auth user and tenant.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Id | string | Yes | ObjectId |
| UserId | string | Yes | Auth user ID |
| TenantId | string | Yes | → Account.Id |
| Email | string | No | User email |
| FirstName | string | No | |
| LastName | string | No | |
| DisplayName | string | Yes | |
| Timezone | string | Yes | Default: "UTC+00:00" |
| Language | string | Yes | Default: "English" |
| BrowserNotifications | bool | Yes | Default: false |
| Role | string | Yes | Legacy display hint, default: "agent" |
| Groups | List\<string\> | Yes | Default: [] |
| ExtensionNumber | int? | No | SIP extension |
| Status | string | Yes | online/away/offline |
| Disabled | bool | Yes | Default: false |
| CreatedAt | DateTime | Yes | UTC timestamp |
| UpdatedAt | DateTime | Yes | UTC timestamp |

**Indexes**:
- `_id` (primary)
- `TenantId, UserId` (unique compound)
- `TenantId` (for tenant queries)

---

### RbacRole

Role definitions with permissions.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Id | string | Yes | ObjectId |
| TenantId | string | No | NULL = system template |
| Name | string | Yes | "Owner", "Admin", "Agent", or custom |
| Description | string | Yes | Default: "" |
| Status | string | Yes | "active" / "inactive" |
| IsSystem | bool | Yes | System roles cannot be deleted |
| Permissions | Dictionary\<string, List\<string\>\> | Yes | { module: [actions] } |
| CreatedAt | DateTime | Yes | UTC timestamp |
| UpdatedAt | DateTime | Yes | UTC timestamp |

**Permission Modules**: dashboard, live, calls, missed_calls, users, groups, reports, directory, ivr, softphone, voice_agent, account_settings

**Permission Actions**: view, create, edit, delete, export, approve

**Indexes**:
- `_id` (primary)
- `TenantId` (for tenant queries, includes NULL for system roles)

---

### RbacUserRole

User-to-role assignment (many-to-many).

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Id | string | Yes | ObjectId |
| UserId | string | Yes | Auth user ID |
| RoleId | string | Yes | → RbacRole.Id |
| TenantId | string | Yes | → Account.Id |
| CreatedAt | DateTime | Yes | UTC timestamp |

**Indexes**:
- `_id` (primary)
- `TenantId, UserId` (for user's roles lookup)
- `TenantId, RoleId` (for role's users lookup)

---

### Call

Call detail record (CDR).

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Id | string | Yes | ObjectId |
| TenantId | string | Yes | → Account.Id |
| UserId | string | Yes | Recording owner |
| CallId | string | No | External PBX ID |
| Direction | CallDirection | Yes | Incoming/Outgoing/Internal |
| Status | CallStatus | Yes | Completed/Missed/NoAnswer/Rejected/Voicemail |
| StartedAt | DateTime | Yes | |
| AnsweredAt | DateTime | No | |
| EndedAt | DateTime | No | |
| RingSeconds | int | Yes | Default: 0 |
| HoldSeconds | int | Yes | Default: 0 |
| TotalHoldSeconds | int | Yes | Default: 0 |
| ActiveSeconds | int | Yes | Default: 0 |
| TotalSeconds | int | Yes | Default: 0 |
| HangupCause | string | No | |
| AgentId | string | No | Logical agent identifier |
| GroupId | string | No | |
| Caller | string | Yes | Default: "" |
| Called | string | Yes | Default: "" |
| FromUri | string | No | SIP-level |
| FromDisplay | string | No | |
| ToUri | string | No | |
| ToDisplay | string | No | |
| TagIds | List\<string\> | Yes | Manual tags |
| AutoTagIds | List\<string\> | Yes | Auto-applied tags |
| Sentiment | Sentiment? | No | Positive/Neutral/Negative |
| Inputs | string | Yes | DTMF input, default: "" |
| HasRecording | bool | Yes | Default: false |
| RecordingUrl | string | No | Storage path |
| Summary | string | No | AI summary |
| SummaryLanguage | string | No | Default: "ar" |
| SummaryAccuracyFeedback | string | No | |
| FullTranscript | string | No | |
| Notes | string | Yes | Default: "" |
| CreatedAt | DateTime | Yes | UTC timestamp |
| UpdatedAt | DateTime | Yes | UTC timestamp |

**Indexes**:
- `_id` (primary)
- `TenantId, StartedAt` (for date-range queries)
- `TenantId, Status` (for status filtering)
- `TenantId, Direction` (for direction filtering)
- `TenantId, TagIds` (for tag filtering)
- `TenantId, Caller` (for caller search)

---

### Flow

IVR flow definition.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Id | string | Yes | ObjectId |
| TenantId | string | Yes | → Account.Id |
| UserId | string | Yes | Creator |
| Name | string | Yes | |
| Description | string | Yes | Default: "" |
| Status | FlowStatus | Yes | Draft/Published |
| AssignedExtension | string | No | Extension number |
| Nodes | List\<FlowNode\> | Yes | React Flow nodes |
| Edges | List\<FlowEdge\> | Yes | React Flow edges |
| CreatedAt | DateTime | Yes | UTC timestamp |
| UpdatedAt | DateTime | Yes | UTC timestamp |

**FlowNode** (Value Object):
- Id: string
- Type: string
- Position: { X: double, Y: double }
- Data: { Label: string, Config: object }

**FlowEdge** (Value Object):
- Id: string
- Source: string
- SourceHandle: string
- Target: string
- TargetHandle: string
- Label: string
- Tone: string

**Indexes**:
- `_id` (primary)
- `TenantId` (for tenant queries)
- `TenantId, AssignedExtension` (for extension lookup)

---

### Contact

Phonebook entry.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Id | string | Yes | ObjectId |
| TenantId | string | Yes | → Account.Id |
| UserId | string | Yes | Creator |
| Name | string | Yes | |
| Phone | string | Yes | Default: "" |
| Email | string | Yes | Default: "" |
| Company | string | Yes | Default: "" |
| TagIds | List\<string\> | Yes | Default: [] |
| Notes | string | Yes | Default: "" |
| LastCallAt | DateTime | No | |
| TotalCalls | int | Yes | Default: 0 |
| CreatedAt | DateTime | Yes | UTC timestamp |
| UpdatedAt | DateTime | Yes | UTC timestamp |

**Indexes**:
- `_id` (primary)
- `TenantId, Phone` (for phone lookup)
- `TenantId, Name` (for name search)
- `TenantId, TagIds` (for tag filtering)

---

### Tag

Label for calls/contacts.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Id | string | Yes | ObjectId |
| TenantId | string | Yes | → Account.Id |
| UserId | string | Yes | Creator |
| Label | string | Yes | |
| Color | string | Yes | Default: "#3B82F6" |
| CreatedAt | DateTime | Yes | UTC timestamp |
| UpdatedAt | DateTime | Yes | UTC timestamp |

**Indexes**:
- `_id` (primary)
- `TenantId` (for tenant queries)

---

### VoiceLibraryItem

Audio prompt (uploaded or TTS-generated).

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Id | string | Yes | ObjectId |
| TenantId | string | Yes | → Account.Id |
| UserId | string | Yes | Creator |
| Name | string | Yes | |
| Source | string | Yes | "upload" / "tts" |
| Text | string | No | For TTS prompts |
| FilePath | string | No | Storage path |
| Url | string | No | |
| Language | string | Yes | Default: "ar" |
| Voice | string | Yes | Default: "female" |
| Interruptible | bool | Yes | Default: true |
| Duration | int | No | Seconds |
| CreatedAt | DateTime | Yes | UTC timestamp |
| UpdatedAt | DateTime | Yes | UTC timestamp |

**Indexes**:
- `_id` (primary)
- `TenantId` (for tenant queries)

---

### SipAccount

Per-user SIP/WebRTC credentials.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Id | string | Yes | ObjectId |
| UserId | string | Yes | → Profile.UserId |
| TenantId | string | Yes | → Account.Id |
| DisplayName | string | Yes | Default: "" |
| SipUri | string | Yes | Default: "" |
| AuthId | string | Yes | Default: "" |
| WsUrl | string | Yes | WebSocket URL |
| StunUrls | List\<string\> | Yes | Default: ["stun:stun.l.google.com:19302"] |
| TurnUrl | string | Yes | Default: "" |
| TurnUsername | string | Yes | Default: "" |
| IsActive | bool | Yes | Default: true |
| CreatedAt | DateTime | Yes | UTC timestamp |
| UpdatedAt | DateTime | Yes | UTC timestamp |

**Indexes**:
- `_id` (primary)
- `TenantId, UserId` (unique compound)

---

### SoftphoneCallLog

Browser-based call session history.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Id | string | Yes | ObjectId |
| UserId | string | Yes | → Profile.UserId |
| TenantId | string | Yes | → Account.Id |
| Direction | string | Yes | |
| Status | string | Yes | |
| Number | string | Yes | |
| DisplayName | string | Yes | Default: "" |
| ContactId | string | No | → Contact.Id |
| StartedAt | DateTime | Yes | |
| DurationSec | int | Yes | Default: 0 |
| FailureReason | string | Yes | Default: "" |
| CreatedAt | DateTime | Yes | UTC timestamp |

**Indexes**:
- `_id` (primary)
- `TenantId, UserId, StartedAt` (for user's call log)

---

### Invoice

Billing invoice.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Id | string | Yes | ObjectId |
| TenantId | string | Yes | → Account.Id |
| UserId | string | Yes | Creator |
| InvoiceNumber | string | Yes | |
| IssueDate | DateTime | Yes | |
| DueDate | DateTime | No | |
| Amount | decimal | Yes | Default: 0 |
| Currency | string | Yes | Default: "SAR" |
| Status | InvoiceStatus | Yes | Paid/Unpaid |
| PaidAt | DateTime | No | |
| PdfUrl | string | No | |
| CreatedAt | DateTime | Yes | UTC timestamp |
| UpdatedAt | DateTime | Yes | UTC timestamp |

**Indexes**:
- `_id` (primary)
- `TenantId, IssueDate` (for date-range queries)
- `TenantId, Status` (for status filtering)

---

### EditLog

Audit trail entry.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Id | string | Yes | ObjectId |
| TenantId | string | Yes | → Account.Id |
| UserId | string | Yes | Actor |
| EntityType | string | Yes | |
| EntityId | string | Yes | |
| Action | string | Yes | create/update/delete |
| Field | string | No | Changed field |
| OldValue | object | No | JSON |
| NewValue | object | No | JSON |
| Summary | string | No | |
| Metadata | object | Yes | Default: {} |
| CreatedAt | DateTime | Yes | UTC timestamp |

**Indexes**:
- `_id` (primary)
- `TenantId, EntityType, EntityId` (for entity history)
- `TenantId, CreatedAt` (for recent changes)

---

## Enums

### CallDirection
- Incoming
- Outgoing
- Internal

### CallStatus
- Completed
- Missed
- NoAnswer
- Rejected
- Voicemail

### FlowStatus
- Draft
- Published

### InvoiceStatus
- Paid
- Unpaid

### Sentiment
- Positive
- Neutral
- Negative

---

## Entity Relationships

```
Account (1) ─────────────< Profile (N)
    │                         │
    │                         └────< RbacUserRole (N) >──── RbacRole (N)
    │
    ├─────────────< Call (N)
    │                  └────────────< Tag (N) (via TagIds)
    │
    ├─────────────< Flow (N)
    │                  └────────────< VoiceLibraryItem (N) (via node references)
    │
    ├─────────────< Contact (N)
    │                  └────────────< Tag (N) (via TagIds)
    │
    ├─────────────< Tag (N)
    │
    ├─────────────< VoiceLibraryItem (N)
    │
    ├─────────────< SipAccount (N) ───> Profile (via UserId)
    │
    ├─────────────< SoftphoneCallLog (N)
    │                  └────────────< Contact (1) (optional)
    │
    ├─────────────< Invoice (N)
    │
    └─────────────< EditLog (N)
```

---

## Notes

1. **No foreign key constraints**: MongoDB doesn't enforce FK constraints; referential integrity is application-level
2. **Tenant isolation**: All tenant-scoped collections MUST include TenantId in compound indexes as the first field
3. **Soft deletes**: Not used per spec ("Hard deletes only")
4. **Timestamps**: All entities include CreatedAt; most include UpdatedAt
5. **ID generation**: Use `ObjectId.GenerateNewId().ToString()` or allow MongoDB auto-generation
