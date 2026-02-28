# DandDTemplateParserCSharp
Parses JSON templates to produce styled HTML D&D monster/character sheets.

## Quick start (development)

```bash
docker-compose up --build        # Start SQL Server + API
dotnet run --project DandDTemplateParserCSharp
dotnet test
```

Dev credentials live in `appsettings.Development.json` (git-ignored — create locally from the values below).

---

## Production deployment checklist

Before going live, every item below must be addressed. Each entry lists the exact file/key to change.

### 1. Host filtering — `appsettings.json` › `AllowedHosts`
Currently `"localhost"`. Set to the real hostname the API is served from:
```
"AllowedHosts": "api.yourdomain.com"
```
Requests with a non-matching `Host` header get a `400` before reaching any controller.

### 2. CORS allowed origins — `appsettings.json` › `Cors.AllowedOrigins`
Currently `[]` (all cross-origin requests denied). Set to the actual frontend origin(s):
```json
"Cors": { "AllowedOrigins": [ "https://app.yourdomain.com" ] }
```

### 3. Load balancer / reverse proxy — `Program.cs`
If the API runs behind a load balancer, uncomment this line (and configure `KnownProxies`/`KnownNetworks`):
```csharp
// app.UseForwardedHeaders();   ← line ~185 in Program.cs
```
Without this, `RemoteIpAddress` will be the proxy's IP and rate limiting will bucket all clients together.

### 4. JWT credentials — environment / secrets manager
`appsettings.json` intentionally leaves these blank. Inject via environment variables or a secrets manager:

| Key | Requirement |
|-----|-------------|
| `Jwt__SigningKey` | ≥ 32 random characters — use `openssl rand -base64 32` |
| `Jwt__ApiSecret` | ≥ 8 characters — the password callers POST to `/api/v1/auth/token` |

Never commit real values. `appsettings.Development.json` is git-ignored for this reason.

### 5. Database connection string — environment / secrets manager
Inject `Database__ConnectionString` via environment variable or secrets manager.
Remove `TrustServerCertificate=True` from the connection string — it bypasses TLS certificate validation and is only appropriate for local dev with self-signed certs.
