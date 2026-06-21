---
gate: critic
status: pass
blocking: false
severity: warning
summary: "9 risks identified; all 9 resolved in tasks.md and research.md (2026-05-07). Both showstoppers fixed: auth scheme (T010) and partial class (T017). All critical and high risks addressed. Ready for implementation."
---

# Technical Risk Assessment: MakeBoldSpark Platform Foundation

**Analysis Date**: 2026-05-07
**Risk Posture**: 🟡 YELLOW — showstoppers present but both are low-effort fixes
**Detected Stack**: C# / .NET 10 LTS · ASP.NET Core Minimal APIs · EF Core 10 · SQLite · Azure App Service Linux B1

---

## Executive Summary

The plan is architecturally sound and the constitution is fully aligned. However, two implementation assumptions are factually incorrect and will cause immediate build or runtime failures: (1) ASP.NET Core throws `InvalidOperationException` — not `401` — when `RequireAuthorization` is evaluated with no registered authentication scheme; (2) `WebApplicationFactory<Program>` cannot compile against a top-level `Program.cs` that generates an `internal` class without an explicit accessibility declaration. Both are one-line fixes but will block every test that uses `WebApplicationFactory`. Fix these before writing a single line of implementation code.

---

## Showstopper Risks (Must Fix Before Implementation)

| ID | Category | Location | Risk Description | Likely Impact | Mitigation Required |
|----|----------|----------|------------------|---------------|---------------------|
| SS-1 | Auth/Security | research.md §2; tasks.md T010, T017 | `RequireAuthorization("AdminOnly")` with no registered auth scheme throws `InvalidOperationException: No authenticationScheme was specified, and there was no DefaultChallengeScheme found` — not `401 Unauthorized`. ASP.NET Core's challenge pipeline has no handler to call, so the middleware crashes. | US3 acceptance criteria ("admin routes return 401") fails with 500; constitution Principle VIII is violated at runtime | Register a minimal challenge scheme. Either add `AddAuthentication().AddBearerToken()` (returns 401 naturally) or register a stub `EmptyAuthHandler` that writes HTTP 401 on `ChallengeAsync`. At minimum: `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer()` with permissive development settings. |
| SS-2 | Test Infrastructure | tasks.md T003, T018–T036 | Top-level `Program.cs` in .NET 6+ generates an `internal` class. A separate test project (`MakeBoldSpark.Api.Tests`) cannot reference `Program` in `WebApplicationFactory<Program>` — compiler error: `'Program' is inaccessible due to its protection level`. All 8 test phases depend on WebApplicationFactory and will fail to compile. | Every test class (T018, T020–T021, T029, T032, T036) fails with a compile error before any test runs | Add `public partial class Program { }` as the last line of `src/MakeBoldSpark.Api/Program.cs`. This is a one-line fix but must be in T017 (Program.cs creation task). |

---

## Critical Risks (High Probability of Costly Issues)

| ID | Category | Location | Risk Description | Likely Impact | Recommended Action |
|----|----------|----------|------------------|---------------|--------------------|
| CR-1 | Database | tasks.md T013, T016; research.md §1 | `db.Database.Migrate()` is synchronous. ASP.NET Core's hosted application model is async-first. Calling a blocking synchronous method in startup can deadlock the thread pool under certain configurations and prevents using `CancellationToken` for startup timeouts. Azure App Service will SIGTERM the process if startup exceeds the platform timeout (~230 seconds), which a blocked migration could hit on first deployment. | Startup timeout → app fails to start → Azure returns 503 on cold deploy | Change to `await db.Database.MigrateAsync(cancellationToken)` inside a proper async startup path. Use `app.Lifetime.ApplicationStarted` registered callback or a `IHostedService.StartAsync` implementation. |
| CR-2 | Database | tasks.md T013; data-model.md | SQLite connection string does not include `Journal Mode=WAL`. Default SQLite journal mode is DELETE (rollback journal). Under DELETE mode, any write operation takes an exclusive lock, blocking ALL concurrent reads. Azure App Service restarts cause the app to read AND write (seed check + migration) simultaneously. Under any non-trivial load, callers hit `SQLite Error 5: 'database is locked'`. | Public content endpoints return 500 on concurrent requests; requests fail non-deterministically | Add `Journal Mode=WAL;` to both connection strings in `appsettings.json` and `appsettings.Development.json`. WAL allows concurrent readers during writes. Also add `Cache=Shared` for in-memory test databases. |
| CR-3 | Testing | tasks.md T020–T021, T029, T036 | EF Core's SQLite in-memory mode (`Data Source=:memory:`) creates a new database per connection. EF Core opens a new connection per `DbContext` scope. Result: the test inserts seed data on connection A, then the next `DbContext` scope opens connection B — a fresh, empty database. All tests that insert data and then query it will see empty results. | All repository and endpoint integration tests appear to pass (empty results match empty DB) but fail to actually validate behavior with data | Use `Data Source=:memory:;Mode=Memory;Cache=Shared;` and ensure the `SqliteConnection` is kept open for the lifetime of the test. Use a shared `SqliteConnection` injected into the `WebApplicationFactory` configuration. Alternatively, use a temp file path per test run. |

---

## High-Priority Concerns

| ID | Category | Location | Issue | Impact | Suggestion |
|----|----------|----------|-------|--------|------------|
| HP-1 | CI/CD | tasks.md T034, T035 | Both `build-test.yml` (T034) and `deploy.yml` (T035) trigger on `push` to `main`. Every merge to `main` runs two complete build+test cycles: one from build-test.yml, one from the test step inside deploy.yml. Doubles CI cost and elapsed time. | 2× build time on every merge; wasted Actions minutes | Remove `push: branches: [main]` from `build-test.yml`. The deploy workflow already runs build+test. OR: make deploy.yml depend on build-test.yml completion using `needs:` and `workflow_run:` triggers. |
| HP-2 | Security | tasks.md T027; contracts/public-content.yaml | `GET /api/public/content/articles/{slug}` passes the raw slug value to the repository with no validation at the endpoint layer. EF Core parameterized queries prevent SQL injection, but: (1) the slug is logged verbatim by the request logging middleware (T012) — log injection via crafted slug values; (2) unrestricted slug length allows multi-megabyte path parameters, causing DoS via log inflation. | Log injection; DoS via log inflation | Add slug validation in the endpoint before calling service: validate against regex `^[a-z0-9-]{1,200}$`; return `400 Bad Request` for invalid slugs. Add `[MaxLength(200)]` constraint to `Article.Slug` in EF entity. |
| HP-3 | Testing | tasks.md T017–T036 | Tests use `WebApplicationFactory<Program>` which reads `appsettings.json` (production path: `/home/data/makeboldspark.db`) unless explicitly overridden. No task details how `IConfiguration` is overridden in the factory to point to a test-specific SQLite. If forgotten, tests will attempt to open `/home/data/makeboldspark.db` on developer workstations (path doesn't exist on Windows → exception) or worse, open the real development database and mutate it. | Tests fail on Windows (path not found); tests corrupt local dev database on Linux | The `WebApplicationFactory<Program>` setup (T031-equivalent infrastructure) must explicitly override the connection string: `factory.WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={testDbPath}"))`. Document this explicitly in the auth handler task. |
| HP-4 | Operational | tasks.md T017; plan.md §Technical Context | No graceful shutdown configuration. Azure App Service sends `SIGTERM` then kills the process after 5 seconds (default). A SQLite migration or seed data write in progress at shutdown time will leave the database in an inconsistent state. EF Core does not roll back uncommitted transactions on process kill. | Corrupt SQLite database on app service restart during a busy deployment window | Configure `builder.WebHost.UseShutdownTimeout(TimeSpan.FromSeconds(10))` in Program.cs; ensure `DatabaseSetup` uses a `CancellationToken` from `app.Lifetime.ApplicationStopping` so it can abort gracefully. |

---

## ASP.NET Core + EF Core Specific Risk Checklist

| Risk | Status | Notes |
|------|--------|-------|
| Missing authentication scheme causes unhandled exception on challenge | ❌ SHOWSTOPPER | SS-1 above |
| `WebApplicationFactory<Program>` internal class access | ❌ SHOWSTOPPER | SS-2 above |
| `db.Database.Migrate()` synchronous blocking in async host | ❌ CRITICAL | CR-1 above |
| SQLite WAL mode not configured | ❌ CRITICAL | CR-2 above |
| EF Core SQLite in-memory shared cache for tests | ❌ CRITICAL | CR-3 above |
| Slug path parameter unvalidated | ❌ HIGH | HP-2 above |
| Test WebApplicationFactory connection string override | ❌ HIGH | HP-3 above |
| N+1 query risk on Article→Tags navigation | ⚠️ MEDIUM | Task T024 says "eager-load Tags" but doesn't specify `.Include(a => a.Tags)` is required; implementer may omit it |
| Duplicate CI trigger on `push` to `main` | ❌ HIGH | HP-1 above |
| `CancellationToken` propagation in repository queries | ⚠️ MEDIUM | Repository interface defines `ct` params; implementer must pass to `.ToListAsync(ct)` etc. — not guaranteed without explicit task note |
| CORS wildcard for development | ✅ OK | `appsettings.Development.json` allows only localhost origins, not `*` |
| Secrets in source control | ✅ OK | Only file paths in connection strings; policies enforced |
| HTTPS enforcement | ✅ OK | Azure App Service handles TLS termination |
| Missing health check endpoint | ✅ OK | `GET /api/health` implemented (T019) |
| Missing structured logging | ✅ OK | T012 implements middleware |
| `dotnet ef` tool not in tools manifest | ⚠️ LOW | Identified in analyze-I1 |

---

## Architecture Red Flags

| Check | Status |
|-------|--------|
| Over-engineered for stated requirements | ✅ No — two-project layout is appropriate |
| Under-engineered for implied scale | ✅ No — B1 single-instance + SQLite is right-sized for personal/portfolio |
| Single point of failure without redundancy | ⚠️ Acknowledged — B1 has no standby; acceptable for stated scope |
| Missing standard patterns for problem domain | ✅ No — feature folder + repository pattern is correct |
| Inadequate async/concurrency handling | ❌ Yes — CR-1 (synchronous Migrate) and CR-2 (SQLite WAL) affect concurrency |

---

## Missing Critical Tasks

- **Auth Scheme Registration**: No task creates a development-safe authentication scheme stub. T010 registers policies but no task registers a scheme. Add task between T010 and T011: "Register development authentication scheme in `AuthorizationSetup.cs` using `AddAuthentication().AddBearerToken()` or a stub JWT scheme."
- **Program.cs partial class**: T017 must include the line `public partial class Program { }` at the end of `Program.cs`. No existing task mentions this.
- **SQLite WAL Mode**: T008 and T009 must set `Journal Mode=WAL;Cache=Shared;` in the `DefaultConnection` value. No existing task mentions this.
- **WebApplicationFactory test DB override**: The test infrastructure task (currently missing a dedicated setup task for the factory) must detail the connection string override pattern.
- **Graceful Shutdown**: No task configures `UseShutdownTimeout` or startup `CancellationToken`.
- **Slug Validation**: No task adds input validation at the `GET /articles/{slug}` endpoint layer.

---

## Questionable Assumptions

1. **"Admin routes return 401 by default when no authentication middleware is configured"** (research.md §2) → **Why this will fail**: ASP.NET Core's `RequireAuthorization()` calls `context.ChallengeAsync()` when the user is not authenticated. Without a registered scheme, this throws `InvalidOperationException`. The app returns 500, not 401. The assumption is factually incorrect for ASP.NET Core 6+.

2. **"`WebApplicationFactory<Program>` works from a separate test project"** (tasks.md T018 etc.) → **Why this will fail**: Top-level `Program.cs` in .NET 6+ generates an `internal sealed` class. A separate assembly cannot reference it. All `WebApplicationFactory<Program>` usage fails at compile time with CS0122 ("inaccessible due to its protection level").

3. **"EF Core SQLite in-memory database persists across test operations"** (tasks.md T020–T021) → **Why this will fail**: Each `DbContext` opens a new connection to `Data Source=:memory:`, which is a fresh, empty database. Seed data inserted by the factory's startup is lost by the time the test client makes its first request. Tests see empty databases and incorrectly pass "no articles returned" checks.

4. **"Two GitHub Actions workflows triggering on `push` to `main` is fine"** (tasks.md T034, T035) → **Why this will fail**: Not a hard failure, but the redundant build doubles CI cost. More importantly, if build-test.yml and deploy.yml run concurrently, there's a race between the two test suites accessing the same SQLite test fixture (if any shared test infrastructure is used in CI).

5. **"The slug parameter is safe to pass directly to EF Core"** (implicit assumption) → **Why this will bite**: While EF Core's parameterized queries prevent SQL injection, an unbounded slug parameter enables log injection attacks (OWASP A09). A crafted slug like `\n[CRITICAL] Fake security breach` pollutes structured logs, potentially triggering false alerts.

---

## Dependencies Risk Assessment

| Dependency | Concern | Alternative to Consider |
|------------|---------|-------------------------|
| `Swashbuckle.AspNetCore` | As of .NET 9+, ASP.NET Core ships `Microsoft.AspNetCore.OpenApi` built-in. Swashbuckle has been slower to update. In .NET 10, there may be compatibility issues between Swashbuckle and the new OpenAPI document generation. | Use `Microsoft.AspNetCore.OpenApi` + `Scalar` (or Swashbuckle) — verify .NET 10 compatibility before T005 |
| `azure/webapps-deploy@v3` | GitHub Action for Azure App Service deployment is version-specific. `@v3` may not support all .NET 10 publish output formats or Linux runtime stack targeting. | Pin to a tested version; add a test deployment step that validates the deployed endpoint |
| EF Core + SQLite on .NET 10 | EF Core 10 and `Microsoft.EntityFrameworkCore.Sqlite 10.x` should be stable by 2026-05-07 but verify no known migration issues with .NET 10 targeting. | Check NuGet for stable 10.x release of EF Core before T005 |
| `dotnet-ef` tool version | T015 runs `dotnet ef migrations add` — the tool version must match the EF Core version in the project. Version mismatch causes `Your startup project 'MakeBoldSpark.Api' doesn't reference EntityFrameworkCore.Design` errors. | Pin the `dotnet-ef` tool in `.config/dotnet-tools.json` to the same major version as EF Core packages |

---

## Estimated Technical Debt at Launch

- **Code Debt**: Synchronous database startup (must refactor to async in Phase 1 of next sprint); no slug validation (must add before Phase 4 admin CRUD)
- **Operational Debt**: No alerting thresholds; no Application Insights integration; no backup strategy (deferred by design — Jumpstart Phase 6)
- **Documentation Debt**: No runbook for "SQLite database corrupted" recovery; no documentation of authentication setup steps for when an identity provider is added
- **Testing Debt**: No load/performance tests; no contract tests against the OpenAPI YAML; timing assertions deferred (analyze-A1)

---

## Findings in Resolution Contract Format

```yaml
findings:
  - finding_id: critic-SS1
    severity: critical
    description: "RequireAuthorization with no registered authentication scheme throws InvalidOperationException on challenge, returning HTTP 500 not 401. Breaks US3 acceptance criteria and constitution Principle VIII at runtime."
    recommended_action: "Add auth scheme registration before T010: builder.Services.AddAuthentication().AddBearerToken() (or a stub EmptyAuthHandler) so challenge pipeline has a handler that returns 401."
    execution_mode: auto
    status: resolved
    outcome: "Resolved 2026-05-07 — see tasks.md Gate Acknowledgements table for task-level resolution details."

  - finding_id: critic-SS2
    severity: critical
    description: "Top-level Program.cs generates an internal class inaccessible from the test project. WebApplicationFactory<Program> fails to compile (CS0122) in all 8 test phases."
    recommended_action: "Add 'public partial class Program { }' as the last line of src/MakeBoldSpark.Api/Program.cs. Include this in T017 task description explicitly."
    execution_mode: auto
    status: resolved
    outcome: "Resolved 2026-05-07 — see tasks.md Gate Acknowledgements table for task-level resolution details."

  - finding_id: critic-CR1
    severity: high
    description: "db.Database.Migrate() is synchronous. In an async ASP.NET Core host, this blocks a thread pool thread and cannot honor startup CancellationToken. On Azure App Service, a slow migration risks hitting the platform startup timeout."
    recommended_action: "Change T016 to use await db.Database.MigrateAsync(cancellationToken) inside an async startup callback. Pass app.Lifetime.ApplicationStopping as the cancellation token."
    execution_mode: auto
    status: resolved
    outcome: "Resolved 2026-05-07 — see tasks.md Gate Acknowledgements table for task-level resolution details."

  - finding_id: critic-CR2
    severity: high
    description: "SQLite connection string lacks 'Journal Mode=WAL'. Default DELETE journal mode takes exclusive write locks, causing 'database is locked' errors under concurrent reads/writes. Any app restart during a write produces this error."
    recommended_action: "Add 'Journal Mode=WAL;' to both connection strings in appsettings.json and appsettings.Development.json. Specify this in T008 and T009 task descriptions."
    execution_mode: auto
    status: resolved
    outcome: "Resolved 2026-05-07 — see tasks.md Gate Acknowledgements table for task-level resolution details."

  - finding_id: critic-CR3
    severity: high
    description: "EF Core SQLite in-memory mode (Data Source=:memory:) creates a new database per connection. Test seed data is lost between the factory startup scope and the test client scope. Tests incorrectly see empty databases."
    recommended_action: "Use 'Data Source=TestDb;Mode=Memory;Cache=Shared;' and keep a shared SqliteConnection open for the test lifetime. Document in the test infrastructure setup task."
    execution_mode: selective
    status: resolved
    outcome: "Resolved 2026-05-07 — see tasks.md Gate Acknowledgements table for task-level resolution details."

  - finding_id: critic-HP1
    severity: medium
    description: "Both build-test.yml and deploy.yml trigger on push to main, causing two full build+test runs on every merge. Doubles CI cost and elapsed time."
    recommended_action: "Remove push trigger from build-test.yml (T034), keeping only pull_request trigger. The deploy.yml (T035) already runs build+test before deploying."
    execution_mode: auto
    status: resolved
    outcome: "Resolved 2026-05-07 — see tasks.md Gate Acknowledgements table for task-level resolution details."

  - finding_id: critic-HP2
    severity: medium
    description: "GET /api/public/content/articles/{slug} passes raw slug to repository with no endpoint-layer validation. Enables log injection via crafted slug values. Unrestricted length enables DoS via log inflation."
    recommended_action: "Add slug validation in T027 endpoint: validate against regex ^[a-z0-9-]{1,200}$, return 400 for invalid slugs before calling service layer."
    execution_mode: auto
    status: resolved
    outcome: "Resolved 2026-05-07 — see tasks.md Gate Acknowledgements table for task-level resolution details."

  - finding_id: critic-HP3
    severity: medium
    description: "WebApplicationFactory uses production appsettings.json (DefaultConnection points to /home/data/makeboldspark.db) unless explicitly overridden. Tests on Windows will throw FileNotFoundException; on Linux they may corrupt the local dev database."
    recommended_action: "Add explicit connection string override in WebApplicationFactory configuration: factory.WithWebHostBuilder(b => b.UseSetting('ConnectionStrings:DefaultConnection', testDbPath)). Document this requirement in test infrastructure tasks."
    execution_mode: selective
    status: resolved
    outcome: "Resolved 2026-05-07 — see tasks.md Gate Acknowledgements table for task-level resolution details."

  - finding_id: critic-HP4
    severity: medium
    description: "No graceful shutdown configuration. Azure App Service sends SIGTERM before a 5-second kill. A migration or seed write in progress at shutdown leaves SQLite in inconsistent state."
    recommended_action: "Add builder.WebHost.UseShutdownTimeout(TimeSpan.FromSeconds(15)) to Program.cs (T017) and pass app.Lifetime.ApplicationStopping CancellationToken to DatabaseSetup (T016)."
    execution_mode: auto
    status: resolved
    outcome: "Resolved 2026-05-07 — see tasks.md Gate Acknowledgements table for task-level resolution details."
```

---

## Metrics

| Metric | Value |
|--------|-------|
| Showstopper Count | 2 (SS-1, SS-2) |
| Critical Risk Count | 3 (CR-1, CR-2, CR-3) |
| High Risk Count | 4 (HP-1, HP-2, HP-3, HP-4) |
| Missing Operational Tasks | 4 (auth scheme, partial class, WAL mode, graceful shutdown) |
| Underspecified Security Requirements | 1 (slug validation) |
| Scale Bottlenecks Identified | 1 (SQLite WAL mode) |
| Constitution Violations | 0 |

---

## GO/NO-GO RECOMMENDATION

```text
[x] CONDITIONAL — Fix 2 showstoppers first, then reassess
```

**Required Actions Before Implementation:**

1. **SS-1** — Add auth scheme registration in `AuthorizationSetup.cs` (or `Program.cs`): `builder.Services.AddAuthentication().AddBearerToken()` (or stub handler). Affects T010 and T017.
2. **SS-2** — Add `public partial class Program { }` as the final line of `Program.cs`. Affects T017.

**Recommended Risk Mitigations (implement alongside showstopper fixes):**

- Add `Journal Mode=WAL;` to connection strings (T008, T009) — prevents "database is locked" errors
- Change `db.Database.Migrate()` to `await db.Database.MigrateAsync(ct)` (T016) — correct async behavior
- Document SQLite shared in-memory cache pattern for test `WebApplicationFactory` (add to test infrastructure task)
- Add slug validation at endpoint layer in T027
- Fix T034 CI trigger to remove redundant `push` to `main` trigger
- Add graceful shutdown timeout to T017

**Both showstoppers are one-line or two-line fixes. Resolve them, then implementation can proceed with high confidence.**

---

## Resolution Record (2026-05-07)

All 9 findings resolved. Gate status updated to PASS. Implementation may proceed.

| Finding | Resolution Applied |
|---------|-------------------|
| SS-1 | T010: `AddJwtBearer()` registered as default scheme; research.md corrected |
| SS-2 | T017: `public partial class Program { }` added as explicit requirement |
| CR-1 | T016: `MigrateAsync(ct)` + try/catch + CRITICAL log on failure |
| CR-2 | T008/T009: `Journal Mode=WAL;Cache=Shared;` in both connection strings |
| CR-3 | T020/T021/T029: Named shared-cache SQLite + `MakeBoldSparkWebApplicationFactory` |
| HP-1 | T034: `push` trigger removed from `build-test.yml` |
| HP-2 | T027: Slug regex validation returning 400 before service layer |
| HP-3 | T029: `MakeBoldSparkWebApplicationFactory` overrides connection string for all tests |
| HP-4 | T017: `UseShutdownTimeout(15s)` added; T016: `ApplicationStopping` CT passed |
