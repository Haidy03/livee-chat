# API Contracts

This directory contains the API endpoint contracts for the VoiceFlow Studio API.

## Base URL

```
/api/v1
```

## Authentication

All endpoints except `/api/v1/auth/*`, `/health`, and `/ready` require:
- `Authorization: Bearer <access_token>` header

## Response Envelope

All responses follow the `ApiResponse<T>` envelope:

```json
{
  "data": <T>,
  "errors": [
    {
      "code": "ERROR_CODE",
      "message": "Localized error message"
    }
  ],
  "metadata": {
    "timestamp": "2026-05-11T12:00:00Z",
    "page": 1,
    "pageSize": 50,
    "totalCount": 100,
    "totalPages": 2
  }
}
```

## Localization

Include `Accept-Language: ar` or `Accept-Language: en` header for localized error messages.

## Endpoint Modules

| Module | Base Path | Description |
|--------|-----------|-------------|
| [auth.md](./auth.md) | `/api/v1/auth` | Authentication (signup, login, tokens) |
| accounts | `/api/v1/accounts` | Tenant settings |
| profiles | `/api/v1/profiles` | User profiles |
| rbac | `/api/v1/rbac` | Roles and permissions |
| calls | `/api/v1/calls` | Call detail records |
| flows | `/api/v1/flows` | IVR flow management |
| voice-library | `/api/v1/voice-library` | Audio prompts |
| contacts | `/api/v1/contacts` | Phonebook |
| tags | `/api/v1/tags` | Labels |
| sip-accounts | `/api/v1/sip-accounts` | SIP/WebRTC credentials |
| invoices | `/api/v1/invoices` | Billing |
| edit-logs | `/api/v1/edit-logs` | Audit trail |

## Standard CRUD Operations

Most resource endpoints follow RESTful patterns:

| Method | Path | Description |
|--------|------|-------------|
| GET | `/{resource}` | List with pagination, filtering |
| GET | `/{resource}/{id}` | Get by ID |
| POST | `/{resource}` | Create new |
| PUT | `/{resource}/{id}` | Update |
| DELETE | `/{resource}/{id}` | Delete |

## Query Parameters

### Pagination
- `page` (int, default: 1)
- `pageSize` (int, default: 50, max: 100)

### Sorting
- `sortBy` (string, field name)
- `sortOrder` (string, "asc" or "desc")

### Filtering
- Field-specific query parameters (e.g., `status=completed`, `direction=incoming`)
- Date ranges: `startDate`, `endDate`

## Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| VALIDATION_ERROR | 400 | Request validation failed |
| UNAUTHORIZED | 401 | Missing or invalid authentication |
| FORBIDDEN | 403 | Insufficient permissions |
| NOT_FOUND | 404 | Resource not found |
| CONFLICT | 409 | Resource conflict (e.g., duplicate email) |
| RATE_LIMITED | 429 | Too many requests |
| INTERNAL_ERROR | 500 | Server error |

## Special Endpoints

### Calls

```
POST /api/v1/calls/{id}/generate-summary
  → Triggers AI summary generation

POST /api/v1/calls/{id}/translate-summary
  Body: { "targetLanguage": "en" }
  → Translates summary to target language

GET /api/v1/calls/{id}/recording-url
  → Returns time-limited signed URL for recording
```

### Flows

```
POST /api/v1/flows/{id}/validate
  → Validates flow structure

POST /api/v1/flows/{id}/publish
  Body: { "assignedExtension": "100" }
  → Publishes flow and assigns to extension

GET /api/v1/flows/{id}/export?format=extensions.conf
GET /api/v1/flows/{id}/export?format=ara-sql
  → Exports flow to Asterisk format
```

### Voice Library

```
POST /api/v1/voice-library/upload
  Content-Type: multipart/form-data
  → Upload audio file

POST /api/v1/voice-library/tts
  Body: { "text": "...", "language": "ar", "voice": "female" }
  → Generate TTS audio

GET /api/v1/voice-library/{id}/url
  → Returns time-limited signed URL
```

### Admin Functions

```
POST /api/v1/admin/create-user
  → Create user within tenant (admin only)
```
