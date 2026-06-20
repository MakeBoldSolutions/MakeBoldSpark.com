# Research: MakeBoldSpark Platform Foundation

**Phase**: 0 — Outline & Research
**Branch**: `001-makeboldspark-foundation`
**Date**: 2026-05-07
**Status**: Complete — all NEEDS CLARIFICATION resolved

---

## Research Topics

The technical context for this feature is well-defined by the Jumpstart Guide and the spec. The following topics were investigated to resolve implementation details before design.

---

### 1. EF Core + SQLite Startup Migration Pattern (.NET 10)

**Decision**: Use `db.Database.Migrate()` inside an explicit startup scope, gated by the `Database:ApplyMigrationsOnStartup` configuration flag.

**Rationale**: Controlled migration (not automatic) gives the production environment explicit control. The `Database.EnsureCreated()` API must not be used alongside migrations — it skips migration history.

**Implementation pattern** (from Jumpstart §9.2):

```csharp
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<MakeBoldSparkDbContext>();
if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    db.Database.Migrate();
}
if (!await db.Articles.AnyAsync())
{
    await SeedData.LoadAsync(db);
}
```

**Alternatives considered**:
- `Database.EnsureCreated()`: Rejected — does not apply migrations, breaks migration history tracking.
- `IHostedService` migration runner: Deferred — adds complexity not warranted at foundation stage. Revisit if async startup becomes necessary.
- Auto-migration on every startup (unconditional): Rejected — removes production control, risky for schema changes once data matters.

---

### 2. Authentication Approach for Foundation Phase

**Decision**: Register `JwtBearer` as the default authentication scheme alongside the three authorization policies (AdminOnly, Publisher, ServiceOrAdmin). An external identity provider (e.g., Azure App Service EasyAuth / Entra ID) is NOT configured in this phase — that comes in a future phase. However, **an authentication scheme handler must be registered** to give ASP.NET Core's challenge pipeline a handler that returns `401 Unauthorized` when a protected route is accessed without credentials.

**Correction from original research**: The original assumption — "admin routes return 401 by default when no authentication middleware is configured" — is incorrect for ASP.NET Core 6+. When `RequireAuthorization()` is applied to a route group and no default authentication scheme is registered, ASP.NET Core's authorization middleware calls `context.ChallengeAsync()` but finds no challenge handler, throwing `InvalidOperationException: No authenticationScheme was specified, and there was no DefaultChallengeScheme found`. This produces HTTP 500, not 401. The `/devspark.critic` gate identified this as a showstopper.

**Rationale for JwtBearer as the scheme**: `AddJwtBearer()` (or the new lightweight `.AddBearerToken()`) registers a challenge handler that correctly returns `401 Unauthorized` with `WWW-Authenticate: Bearer` when a request lacks a valid token. In the foundation phase, no token validation key is configured (development mode), so all requests are treated as anonymous — which is the correct behavior. When an OIDC provider is added later, the JwtBearer configuration is updated in one place.

**Implementation**:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(); // No Authority configured yet — all tokens are anonymous in dev
builder.Services.AddAuthorization(options => {
    options.AddPolicy("AdminOnly", p => p.RequireAuthenticatedUser().RequireRole("Admin"));
    options.AddPolicy("Publisher", p => p.RequireAuthenticatedUser().RequireRole("Admin","Publisher"));
    options.AddPolicy("ServiceOrAdmin", p => p.RequireAssertion(ctx =>
        ctx.User.IsInRole("Admin") || ctx.User.HasClaim("scope","makeboldspark.publish")));
});
// In middleware pipeline — order matters:
app.UseAuthentication();  // Must come before UseAuthorization
app.UseAuthorization();
```

**Consequences**: Admin routes now correctly return `401 Unauthorized` for anonymous callers. When an OIDC provider is added, update `AddJwtBearer()` with `Authority` and `Audience` settings — no route changes required.

**Alternatives considered**:
- No auth scheme (policies only): Rejected — throws `InvalidOperationException` on challenge; produces HTTP 500 not 401.
- Configuring Azure Entra ID OIDC in this phase: Deferred — requires tenant configuration not yet established; premature for a local-first foundation.
- Custom stub `EmptyAuthHandler` returning 401: Rejected — more code than necessary; `AddBearerToken()` achieves the same with less.
- No auth configuration at all: Rejected — violates constitution Principle VIII (clear authorization boundaries must be enforced from day one).

---

### 3. Structured Logging Implementation

**Decision**: Use ASP.NET Core's built-in `ILogger<T>` with a custom `RequestLoggingMiddleware` to capture all required fields per spec FR-009. No third-party logging framework (e.g., Serilog) in Phase 0; it can be added in a later phase if structured output format to a sink is needed.

**Required fields per spec FR-009 / Jumpstart §16.1**:
- Request path
- HTTP method
- Status code
- Duration (ms)
- Correlation ID (from `HttpContext.TraceIdentifier` or a custom `X-Correlation-ID` header)
- User ID / auth subject (when authenticated: `HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)`)
- Feature name (derived from route)
- Operation name (derived from route pattern)
- Success/failure indicator (`statusCode < 400`)

**Alternatives considered**:
- Serilog with structured JSON output: Deferred — excellent choice for production but adds a dependency and configuration complexity not needed for foundation phase; add when deploying to Azure with Application Insights sink.
- OpenTelemetry: Deferred — heavier setup; appropriate when distributed tracing across services is needed (not applicable to single-backend model).
- No custom middleware (rely on built-in ASP.NET Core request logging): Rejected — built-in request logging does not capture all required fields (correlation ID, user subject, feature name, success indicator).

---

### 4. CORS Configuration

**Decision**: Configure CORS with named policy `"MakeBoldSparkPolicy"` allowing only explicitly listed origins. Origins are loaded from `AllowedOrigins` configuration array (not hardcoded). In local development, `http://localhost:xxxx` entries are added via `appsettings.Development.json`.

**Rationale**: Wildcard CORS (`AllowAnyOrigin`) is forbidden for authenticated routes (constitution Principle IV, spec FR-008). Making origins configuration-driven allows Azure App Service settings to control production origins without code changes.

**Configuration pattern**:

```json
// appsettings.json (no origins — safe default)
{
  "AllowedOrigins": []
}

// appsettings.Development.json
{
  "AllowedOrigins": ["http://localhost:5173", "http://localhost:3000"]
}
```

**Azure App Service settings** (production):
```
AllowedOrigins__0=https://markhazleton.com
AllowedOrigins__1=https://promptspark.markhazleton.com
```

**Alternatives considered**:
- Hardcoded origin list: Rejected — origins change as client sites are added; configuration is cleaner.
- `AllowAnyOrigin`: Rejected — violates spec FR-008 and constitution Principle IV for authenticated routes.

---

### 5. SQLite Database Path Strategy and WAL Mode

**Decision**: Use two separate default paths with WAL (Write-Ahead Logging) journal mode enabled:
- **Local development**: `./data/makeboldspark.local.db` (relative to project output, created automatically)
- **Production (Azure App Service Linux)**: `/home/data/makeboldspark.db` (persistent storage, survives redeployments)

`./data/` directory is added to `.gitignore` to prevent accidental commit of local DB files.

**WAL mode is required**: Without `Journal Mode=WAL`, SQLite uses DELETE (rollback) journal mode which acquires an exclusive write lock that blocks ALL concurrent reads. On any startup that performs seeding or migration (writes), all simultaneous GET requests receive `SQLite Error 5: database is locked`. WAL mode allows concurrent readers while a write is in progress and is the correct mode for a web application. The `/devspark.critic` gate identified this as a critical risk.

**Configuration**:

```json
// appsettings.json (production default)
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/home/data/makeboldspark.db;Journal Mode=WAL;Cache=Shared;"
  }
}

// appsettings.Development.json (local override)
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./data/makeboldspark.local.db;Journal Mode=WAL;Cache=Shared;"
  }
}
```

**Alternatives considered**:
- Single path with environment-specific override only in Azure: Rejected — local developers shouldn't need to create `/home/data/`; the Development config override is cleaner.
- Using `Path.Combine(AppContext.BaseDirectory, "makeboldspark.db")`: Rejected — less obvious; `./data/` directory makes the local DB easy to find and easy to `.gitignore`.

---

## Summary of Resolutions

| Topic | Decision | Resolved |
|-------|----------|----------|
| Startup migration | `await db.Database.MigrateAsync(ct)` (async) gated by `Database:ApplyMigrationsOnStartup`; path writability check before migrate; try/catch with CRITICAL log on failure | ✅ |
| Auth for Phase 0 | Register `AddJwtBearer()` as default scheme + 3 named policies; `UseAuthentication()` then `UseAuthorization()` in pipeline; no external IdP yet | ✅ (corrected from original) |
| Structured logging | Custom `RequestLoggingMiddleware` using built-in `ILogger<T>` | ✅ |
| CORS | Named policy; origins from config array; no wildcard | ✅ |
| SQLite paths + WAL | `./data/makeboldspark.local.db;Journal Mode=WAL;Cache=Shared;` (dev) / `/home/data/makeboldspark.db;Journal Mode=WAL;Cache=Shared;` (prod) via config | ✅ (WAL mode added) |

All NEEDS CLARIFICATION items are resolved. Proceed to Phase 1: Design & Contracts.
