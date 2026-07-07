# API Contract: Authentication

**Base Path**: `/api/v1/auth`  
**Authentication**: Public (AllowAnonymous)

---

## POST /signup

Create a new user account with automatic tenant provisioning.

**Request**:
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe",
  "orgName": "Acme Corp"
}
```

**Response** (201 Created):
```json
{
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "eyJ...",
    "expiresIn": 3600,
    "user": {
      "id": "507f1f77bcf86cd799439011",
      "email": "user@example.com",
      "tenantId": "507f1f77bcf86cd799439012",
      "roles": ["Owner"]
    }
  },
  "errors": null,
  "metadata": {
    "timestamp": "2026-05-11T12:00:00Z"
  }
}
```

**Errors**:
- `400 Bad Request`: Validation errors (weak password, invalid email)
- `409 Conflict`: Email already exists

---

## POST /login

Authenticate user and issue tokens.

**Request**:
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response** (200 OK):
```json
{
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "eyJ...",
    "expiresIn": 3600,
    "user": {
      "id": "507f1f77bcf86cd799439011",
      "email": "user@example.com",
      "tenantId": "507f1f77bcf86cd799439012",
      "roles": ["Owner", "Admin"]
    }
  },
  "errors": null,
  "metadata": null
}
```

**Errors**:
- `401 Unauthorized`: Invalid credentials

---

## POST /refresh

Refresh access token using refresh token.

**Request**:
```json
{
  "refreshToken": "eyJ..."
}
```

**Response** (200 OK):
```json
{
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "eyJ...",
    "expiresIn": 3600
  },
  "errors": null,
  "metadata": null
}
```

**Errors**:
- `401 Unauthorized`: Invalid or expired refresh token

---

## POST /logout

Revoke refresh token (requires authentication).

**Headers**: `Authorization: Bearer <access_token>`

**Request**:
```json
{
  "refreshToken": "eyJ..."
}
```

**Response** (204 No Content)

---

## POST /recover

Request password reset email.

**Request**:
```json
{
  "email": "user@example.com"
}
```

**Response** (200 OK):
```json
{
  "data": {
    "message": "If the email exists, a reset link has been sent."
  },
  "errors": null,
  "metadata": null
}
```

**Note**: Always returns 200 to prevent email enumeration.

---

## POST /reset-password

Reset password using token from email.

**Request**:
```json
{
  "token": "reset-token-from-email",
  "newPassword": "NewSecurePassword123!"
}
```

**Response** (200 OK):
```json
{
  "data": {
    "message": "Password reset successful."
  },
  "errors": null,
  "metadata": null
}
```

**Errors**:
- `400 Bad Request`: Invalid or expired token, weak password

---

## GET /me

Get current user profile (requires authentication).

**Headers**: `Authorization: Bearer <access_token>`

**Response** (200 OK):
```json
{
  "data": {
    "id": "507f1f77bcf86cd799439011",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "displayName": "John Doe",
    "tenantId": "507f1f77bcf86cd799439012",
    "roles": ["Owner"],
    "permissions": {
      "dashboard": ["view"],
      "calls": ["view", "create", "edit"],
      "users": ["view", "create", "edit", "delete"]
    }
  },
  "errors": null,
  "metadata": null
}
```

---

## JWT Token Claims

```json
{
  "sub": "507f1f77bcf86cd799439011",
  "tenant_id": "507f1f77bcf86cd799439012",
  "email": "user@example.com",
  "roles": "Owner,Admin",
  "iat": 1715428800,
  "exp": 1715432400,
  "iss": "voiceflow-api"
}
```
