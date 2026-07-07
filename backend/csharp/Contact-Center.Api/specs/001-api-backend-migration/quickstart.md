# Quickstart: VoiceFlow Studio API

## Prerequisites

- .NET 8 SDK
- MongoDB 6.0+ (local or Atlas)
- Docker (optional, for containerized MongoDB)
- Visual Studio 2022 / VS Code / Rider

## Setup

### 1. Clone and Navigate

```bash
cd "VoiceFlow Studio.Api"
```

### 2. Start MongoDB (Docker)

```bash
docker run -d --name voiceflow-mongo \
  -p 27017:27017 \
  -e MONGO_INITDB_ROOT_USERNAME=admin \
  -e MONGO_INITDB_ROOT_PASSWORD=password \
  mongo:6.0
```

### 3. Configure Environment

Create `src/VoiceFlow.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://admin:password@localhost:27017/voiceflow?authSource=admin"
  },
  "Jwt": {
    "Issuer": "voiceflow-api",
    "Audience": "voiceflow-client",
    "PrivateKeyPath": "keys/private.pem",
    "PublicKeyPath": "keys/public.pem",
    "AccessTokenExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  },
  "Storage": {
    "Provider": "AzureBlob",
    "ConnectionString": "<your-azure-connection-string>",
    "RecordingsContainer": "recordings",
    "VoiceLibraryContainer": "voice-library"
  },
  "AiGateway": {
    "BaseUrl": "https://ai-gateway.example.com",
    "ApiKey": "<your-api-key>",
    "TimeoutSeconds": 30
  },
  "Email": {
    "Provider": "Smtp",
    "Host": "smtp.example.com",
    "Port": 587,
    "Username": "<smtp-user>",
    "Password": "<smtp-password>",
    "FromAddress": "noreply@voiceflow.com"
  },
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowMinutes": 1
  }
}
```

### 4. Generate RSA Keys

```bash
mkdir -p src/VoiceFlow.Api/keys

# Generate private key
openssl genrsa -out src/VoiceFlow.Api/keys/private.pem 2048

# Extract public key
openssl rsa -in src/VoiceFlow.Api/keys/private.pem \
  -outform PEM -pubout -out src/VoiceFlow.Api/keys/public.pem
```

### 5. Restore and Build

```bash
dotnet restore
dotnet build
```

### 6. Run

```bash
cd src/VoiceFlow.Api
dotnet run
```

API available at: `https://localhost:5001`

Swagger UI: `https://localhost:5001/swagger`

## Project Structure

```
src/
├── VoiceFlow.Core/          # Domain entities, interfaces (no dependencies)
├── VoiceFlow.Application/   # Services, use cases (depends on Core)
├── VoiceFlow.Infrastructure/# Repositories, external services (depends on Core, Application)
├── VoiceFlow.Contracts/     # DTOs, request/response models
└── VoiceFlow.Api/           # Controllers, middleware, startup
```

## Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/VoiceFlow.Application.Tests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## First API Call

### 1. Create Account (Signup)

```bash
curl -X POST https://localhost:5001/api/v1/auth/signup \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "password": "SecurePassword123!",
    "firstName": "Admin",
    "lastName": "User",
    "orgName": "Test Organization"
  }'
```

### 2. Login

```bash
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "password": "SecurePassword123!"
  }'
```

Save the `accessToken` from the response.

### 3. Get Current User

```bash
curl https://localhost:5001/api/v1/auth/me \
  -H "Authorization: Bearer <accessToken>"
```

### 4. List Calls (Empty Initially)

```bash
curl https://localhost:5001/api/v1/calls \
  -H "Authorization: Bearer <accessToken>" \
  -H "Accept-Language: en"
```

## Common Tasks

### Add a New Entity

1. Create entity class in `VoiceFlow.Core/Entities/`
2. Create repository interface in `VoiceFlow.Core/Interfaces/Repositories/`
3. Create configuration in `VoiceFlow.Infrastructure/Persistence/Configurations/`
4. Create indexes in `VoiceFlow.Infrastructure/Persistence/Indexes/`
5. Implement repository in `VoiceFlow.Infrastructure/Persistence/Repositories/`
6. Create DTOs in `VoiceFlow.Contracts/`
7. Create mapping profile in `VoiceFlow.Infrastructure/Mapping/`
8. Create service interface and implementation in `VoiceFlow.Application/Services/`
9. Create controller in `VoiceFlow.Api/Controllers/`
10. Register DI in `VoiceFlow.Infrastructure/DependencyInjection.cs`

### Add Localization

1. Add key to `src/VoiceFlow.Api/Resources/Messages.en.json`
2. Add Arabic translation to `src/VoiceFlow.Api/Resources/Messages.ar.json`
3. Inject `IStringLocalizer<Messages>` in your service/controller

## Troubleshooting

### MongoDB Connection Failed

Check that MongoDB is running:
```bash
docker ps | grep voiceflow-mongo
```

### JWT Validation Failed

Ensure keys exist and have correct permissions:
```bash
ls -la src/VoiceFlow.Api/keys/
```

### Rate Limited

Default is 100 requests per minute per tenant. Adjust in `appsettings.json`.

## Environment Variables (Production)

For production, use environment variables instead of appsettings:

```bash
ConnectionStrings__MongoDB=<connection-string>
Jwt__PrivateKeyPath=/secrets/private.pem
Jwt__PublicKeyPath=/secrets/public.pem
Storage__ConnectionString=<storage-connection>
AiGateway__ApiKey=<api-key>
```

## Docker Build

```bash
docker build -t voiceflow-api .
docker run -p 5001:5001 \
  -e ConnectionStrings__MongoDB=<connection-string> \
  voiceflow-api
```
