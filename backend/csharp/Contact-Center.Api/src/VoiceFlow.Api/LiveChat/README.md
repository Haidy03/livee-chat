# LiveChat Module

Self-contained live-chat channel bolted onto the existing `VoiceFlow.Api` host.
Adds SignalR hubs (`/hubs/agent`, `/hubs/customer`), a routing engine, channel
adapters (web widget, mobile app, WhatsApp, Messenger), a webhook controller
(`/webhooks/whatsapp`, `/webhooks/messenger`), and two background workers.

## Requirements

- **MongoDB replica set** — accept step uses a transaction (`InsertOne` +
  `DeleteOne` in one session). Standalone Mongo will not work.
- **Redis** — presence store, offer-timeout zset, and SignalR backplane share a
  single `IConnectionMultiplexer`.

## Configuration (appsettings)

Reuses existing `MongoDB` and `Jwt` sections. Adds:

```jsonc
"ConnectionStrings": { "Redis": "localhost:6379" },
"LiveChat": {
  "OfferTimeoutSeconds": 20,
  "StaleRequestGraceMinutes": 5,
  "AllowedOrigins": [ "http://localhost:8080" ],
  "Channels": {
    "WhatsApp":  { "BaseUrl": "https://graph.facebook.com/v20.0/", "Token": "", "VerifyToken": "" },
    "Messenger": { "BaseUrl": "https://graph.facebook.com/v20.0/", "Token": "" }
  }
}
```

## Run

```bash
dotnet build backend/csharp/Contact-Center.Api/src/VoiceFlow.Api/VoiceFlow.Api.csproj
dotnet run --project backend/csharp/Contact-Center.Api/src/VoiceFlow.Api
```

## Lifecycle

1. Customer → `CustomerHub.StartChat` or `/webhooks/{channel}` → `ClientRequest`
   saved (`locked=false`, online).
2. `RoutingEngine.TryDispatch` picks the best agent (dept + lang + capacity,
   skipping `execludedAgentId`), atomically locks the request, reserves the
   agent's Redis slot, arms a 20 s offer timeout, sends `RequestOffered`.
3. Agent accepts → Mongo transaction creates `Room` and deletes
   `ClientRequest`. Both succeed or both roll back. Then `RoomStarted`.
4. Decline / timeout → decrement load, unlock request, append agent id to
   `execludedAgentId`, re-dispatch.

## SignalR JWT over WebSocket

`Program.cs` extends `AddJwtBearer` with an `OnMessageReceived` handler that
reads `access_token` from the query string for `/hubs/*` (browser WS handshake
cannot set headers).
