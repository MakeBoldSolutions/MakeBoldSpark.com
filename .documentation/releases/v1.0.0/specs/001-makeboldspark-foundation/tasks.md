# Tasks: MakeBoldSpark Platform Foundation

**Input**: Design documents from `.documentation/specs/001-makeboldspark-foundation/`
**Branch**: `001-makeboldspark-foundation`
**Date**: 2026-05-07
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅ | quickstart.md ✅

**Spec frontmatter**: `classification: full-spec` | `risk_level: medium` | `required_gates: checklist, analyze, critic`
**Tests**: Included — the spec requires test-first development (Constitution Principle II; spec User Story 5).

## Rationale Summary

### Core Problem

No unified, low-cost platform exists for hosting personal and portfolio APIs. The codebase is currently empty (no solution, no projects, no CI).

### Decision Summary

Build one modular ASP.NET Core .NET 10 LTS backend with route groups, EF Core + SQLite, one public content feature, admin authorization placeholder boundaries, Swagger (dev only), and GitHub Actions CI/CD — proving the architecture pattern without overbuilding.

### Key Drivers

- Replace scattered API hosting with a single Azure-hosted platform
- Foundation must be clean enough for future features to extend without rework
- Every PR must pass CI (test-first is non-negotiable per constitution)

### Reviewer Guidance

Verify: route groups match constitution auth table; public endpoints anonymous and read-only; admin routes return 401; no `.db` files or secrets committed; tests cover meaningful behavior.

---

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel with other [P] tasks in the same phase (different files, no blocking dependency)
- **[Story]**: Maps task to a user story (US1–US6) from spec.md

## Path Conventions

All paths relative to repository root:
- **API project**: `src/MakeBoldSpark.Api/`
- **Test project**: `tests/MakeBoldSpark.Api.Tests/`
- **Workflows**: `.github/workflows/`
- **Docs**: `docs/decisions/`

---

## Phase 1: Setup (Solution Scaffold)

**Purpose**: Create the solution and project files. Nothing compiles yet.

- [X] T001 Create `.gitignore` (standard .NET: bin/, obj/, *.user, `data/*.db`, `*.db`, `*.db-shm`, `*.db-wal`, `.env`, `publish/`)
- [X] T001a Create `.config/dotnet-tools.json` via `dotnet new tool-manifest`; add `dotnet-ef` tool pinned to the same major version as EF Core packages via `dotnet tool install dotnet-ef`; this ensures `dotnet tool restore` in CI reproducibly installs the correct `dotnet ef` CLI before T015 runs *(resolves analyze-I1)*
- [X] T002 Create `MakeBoldSpark.sln` at repository root using `dotnet new sln -n MakeBoldSpark`
- [X] T003 [P] Create `src/MakeBoldSpark.Api/MakeBoldSpark.Api.csproj` using `dotnet new web -n MakeBoldSpark.Api -o src/MakeBoldSpark.Api --framework net10.0` and add to solution
- [X] T004 [P] Create `tests/MakeBoldSpark.Api.Tests/MakeBoldSpark.Api.Tests.csproj` using `dotnet new xunit -n MakeBoldSpark.Api.Tests -o tests/MakeBoldSpark.Api.Tests --framework net10.0` and add to solution; add project reference to MakeBoldSpark.Api
- [X] T005 Add NuGet packages to `src/MakeBoldSpark.Api/MakeBoldSpark.Api.csproj`: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`, `Swashbuckle.AspNetCore`
- [X] T006 Add NuGet packages to `tests/MakeBoldSpark.Api.Tests/MakeBoldSpark.Api.Tests.csproj`: `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.Sqlite`
- [X] T007 Verify `dotnet build MakeBoldSpark.sln` succeeds with zero errors

**Checkpoint**: `dotnet build` green. Empty projects compile.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that ALL user stories depend on. No user story work starts until this phase is complete.

**⚠️ CRITICAL**: Admin route group, auth policies, DbContext, and logging middleware must be in place before any feature implementation.

### Configuration

- [X] T008 Create `src/MakeBoldSpark.Api/appsettings.json` with: `ConnectionStrings:DefaultConnection` → `Data Source=/home/data/makeboldspark.db;Journal Mode=WAL;Cache=Shared;`, `Database:ApplyMigrationsOnStartup` → `true`, `Database:SeedOnStartup` → `false`, `AllowedOrigins: []` — WAL mode allows concurrent reads during writes, preventing `SQLite Error 5: database is locked` on the Azure App Service single instance *(resolves critic-CR2)*
- [X] T009 Create `src/MakeBoldSpark.Api/appsettings.Development.json` with: `ConnectionStrings:DefaultConnection` → `Data Source=./data/makeboldspark.local.db;Journal Mode=WAL;Cache=Shared;`, `Database:ApplyMigrationsOnStartup` → `true`, `Database:SeedOnStartup` → `true`, `AllowedOrigins: ["http://localhost:5173","http://localhost:3000"]` *(resolves critic-CR2)*

### Authorization Policies

- [X] T010 Create `src/MakeBoldSpark.Api/Infrastructure/Auth/AuthorizationSetup.cs` — **IMPORTANT**: register an authentication scheme AND authorization policies together: `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer()` (uses default dev-safe settings; without a registered scheme, `RequireAuthorization()` throws `InvalidOperationException` on challenge — HTTP 500, not 401); then register three named policies: `AdminOnly` (authenticated + Admin role), `Publisher` (authenticated + Admin or Publisher role), `ServiceOrAdmin` (Admin role OR `scope` claim `makeboldspark.publish`); call `app.UseAuthentication()` and `app.UseAuthorization()` in `Program.cs` middleware pipeline in that order *(resolves critic-SS1)*

### CORS

- [X] T011 Create `src/MakeBoldSpark.Api/Infrastructure/Cors/CorsSetup.cs` — reads `AllowedOrigins` string array from `IConfiguration`; registers named policy `"MakeBoldSparkPolicy"` with those origins, no wildcard; registers `app.UseCors("MakeBoldSparkPolicy")` in `Program.cs`

### Structured Logging Middleware

- [X] T012 Create `src/MakeBoldSpark.Api/Infrastructure/Observability/RequestLoggingMiddleware.cs` — captures per-request: path, method, status code, duration (ms), correlation ID (from `X-Correlation-ID` header or `HttpContext.TraceIdentifier`), user ID (from `ClaimTypes.NameIdentifier` when authenticated), feature name (first route segment after `/api/`), operation name (full route pattern), success/failure (`statusCode < 400`); logs as structured ILogger Information entry; register in `Program.cs`
- [X] T012a [P] Create `tests/MakeBoldSpark.Api.Tests/Infrastructure/Observability/RequestLoggingMiddlewareTests.cs` — use a custom `ILogger` test sink (e.g., a `List<LogEntry>` populated via `ILoggerProvider`); make a request through the test factory; assert that the captured log entry contains all 9 FR-009 fields: `RequestPath`, `Method`, `StatusCode`, `DurationMs`, `CorrelationId`, `FeatureName`, `OperationName`, `Success`; verify `UserId` is absent for anonymous requests and present for authenticated ones *(resolves analyze-U3)*

### Data Layer

- [X] T013 Create `src/MakeBoldSpark.Api/Infrastructure/Data/MakeBoldSparkDbContext.cs` — `DbContext` with `DbSet<Article>` and `DbSet<Tag>`; configure unique index on `Article.Slug` and `Tag.Name` in `OnModelCreating`; read connection string from `IConfiguration["ConnectionStrings:DefaultConnection"]`; register as scoped service in `Program.cs`
- [X] T014 Create EF Core entities in `src/MakeBoldSpark.Api/Infrastructure/Data/`:
  - `Article.cs` — fields: `Id`, `Slug`, `Title`, `Summary`, `Body`, `PublishDate` (nullable `DateTimeOffset`), `Status` (`ArticleStatus` enum), `CreatedAt`, `UpdatedAt`, `Tags` (ICollection<Tag>)
  - `Tag.cs` — fields: `Id`, `Name`, `Articles` (ICollection<Article>)
  - `ArticleStatus.cs` — enum: `Draft = 0`, `Published = 1`
- [X] T015 Run `dotnet ef migrations add InitialCreate --project src/MakeBoldSpark.Api --output-dir Migrations` to generate the first EF Core migration; verify migration file is created
- [X] T016 Create `src/MakeBoldSpark.Api/Infrastructure/Data/DatabaseSetup.cs` — async static helper called from `Program.cs` after `app.Build()`. Implementation requirements: (1) check `/home/data/` directory exists and is writable before proceeding (log CRITICAL + throw if not, so Azure App Service fails fast with a clear error rather than a cryptic SQLite exception); (2) use `await db.Database.MigrateAsync(ct)` — **not** the synchronous `Migrate()` which blocks the thread pool; (3) wrap in try/catch: on `Exception`, log `LogLevel.Critical` with message "Database migration failed — aborting startup" and rethrow to abort startup; (4) call `SeedData.LoadAsync(db, ct)` only when `Articles` table is empty AND `Database:SeedOnStartup` is `true` *(resolves critic-CR1, analyze-U1, analyze-U2)*

### Route Group Skeleton

- [X] T017 Create `src/MakeBoldSpark.Api/Program.cs` (full version) — wire all services (auth, CORS, DbContext, Swagger dev-only, logging middleware); add `builder.WebHost.UseShutdownTimeout(TimeSpan.FromSeconds(15))` so Azure App Service SIGTERM allows in-flight SQLite writes to complete before process kill; define route groups: `publicApi = app.MapGroup("/api/public")`, `adminApi = app.MapGroup("/api/admin").RequireAuthorization("AdminOnly")`, `publishApi = app.MapGroup("/api/publish").RequireAuthorization("Publisher")`, `integrationsApi = app.MapGroup("/api/integrations").RequireAuthorization("ServiceOrAdmin")`; call `DatabaseSetup.InitializeAsync(app)` before `app.Run()`; **add `public partial class Program { }` as the very last line** — required for `WebApplicationFactory<Program>` in the test project to access the class across assembly boundaries (without this, all test projects fail CS0122 at compile time) *(resolves critic-SS2, critic-HP4)*

**Checkpoint**: `dotnet build` green. Route groups registered. `dotnet run` starts and logs startup messages.

---

## Phase 3: User Story 1 — Platform Health Verification (Priority: P1) 🎯 MVP

**Goal**: Any caller can confirm MakeBoldSpark is running via anonymous `GET /api/health`.

**Independent Test**: `GET /api/health` returns `200 OK` with `{"status":"Healthy","service":"MakeBoldSpark","version":"..."}` without authentication.

### Tests for User Story 1

> Write tests FIRST — they must FAIL before implementation (T019).

- [X] T018 [P] [US1] Create `tests/MakeBoldSpark.Api.Tests/Features/Health/HealthEndpointTests.cs` — using `WebApplicationFactory<Program>`: test `GET /api/health` returns `200`; response body deserializes to `HealthResponse` with `status = "Healthy"`, `service = "MakeBoldSpark"`, non-empty `version`; test anonymous access (no auth header required); test repeated calls return consistent `200`; use `Stopwatch` to measure wall-clock response time and assert `elapsed.TotalMilliseconds < 500` (SC-001 — cold start included since `WebApplicationFactory` initializes the host before the first request) *(resolves analyze-A1)*

### Implementation for User Story 1

- [X] T019 [US1] Create `src/MakeBoldSpark.Api/Features/Health/HealthEndpoints.cs` — static class with `MapHealthApi(this WebApplication app)` extension: registers `GET /api/health` → returns `HealthResponse` with `Status = "Healthy"`, `Service = "MakeBoldSpark"`, `Version` from assembly version; `.WithName("GetHealth").WithOpenApi().AllowAnonymous()`; add `HealthResponse` record to `src/MakeBoldSpark.Api/Features/Health/HealthModels.cs`; call `app.MapHealthApi()` from `Program.cs`

**Checkpoint**: `dotnet test --filter "Category=US1"` green. `GET /api/health` returns `200` with correct body.

---

## Phase 4: User Story 2 — Public Content Browsing (Priority: P2)

**Goal**: Anonymous callers retrieve published articles and tags via EF Core + SQLite.

**Independent Test**: Seed DB with 2 published articles + 1 draft + 2 tags. `GET /api/public/content/articles` returns 2 items (no draft). `GET /api/public/content/articles/hello-world` returns full detail. `GET /api/public/content/articles/draft-article` returns `404`. `GET /api/public/content/tags` returns 2 tags.

### Tests for User Story 2

- [X] T020 [P] [US2] Create `tests/MakeBoldSpark.Api.Tests/Infrastructure/Data/ContentRepositoryTests.cs` — use a **named shared-cache SQLite** database (NOT bare `Data Source=:memory:` — each new connection to `:memory:` gets an empty database): use `Data Source=ContentRepoTest_[Guid];Mode=Memory;Cache=Shared;`, open one `SqliteConnection` for the test lifetime, apply migrations via `db.Database.EnsureCreated()`, seed data, then run assertions; test `GetPublishedArticlesAsync` returns only Published articles; test `GetPublishedArticleBySlugAsync` returns null for Draft slug; test `GetAllTagsAsync` returns all tags *(resolves critic-CR3)*
- [X] T021 [P] [US2] Create `tests/MakeBoldSpark.Api.Tests/Features/PublicContent/PublicContentEndpointTests.cs` — `WebApplicationFactory<Program>` must override the connection string to a named shared-cache test database (`Data Source=ContentEndpointTest_[Guid];Mode=Memory;Cache=Shared;`) and keep the `SqliteConnection` open for the factory lifetime (create a custom `MakeBoldSparkWebApplicationFactory` base class shared by T020, T021, T029, T036 that handles this pattern); test `GET /api/public/content/articles` returns `200` with list of only published items; test each article summary has slug, title, summary, publishDate, tags but NO body field; test `GET /api/public/content/articles/hello-world` returns `200` with full body; test `GET /api/public/content/articles/draft-article` returns `404`; test `GET /api/public/content/articles/nonexistent` returns `404`; for 404 responses assert `Content-Type: application/problem+json`; test `GET /api/public/content/tags` returns `200` with all tags; test empty DB returns `200` with empty list *(resolves critic-CR3, critic-HP3, analyze-A4)*

### Implementation for User Story 2

- [X] T022 [P] [US2] Create `src/MakeBoldSpark.Api/Features/PublicContent/ContentModels.cs` — response records: `ArticleSummary(string Slug, string Title, string Summary, DateTimeOffset? PublishDate, IReadOnlyList<string> Tags)`, `ArticleDetail(string Slug, string Title, string Summary, string Body, DateTimeOffset? PublishDate, IReadOnlyList<string> Tags)`, `TagResponse(string Name)`
- [X] T023 [P] [US2] Create `src/MakeBoldSpark.Api/Infrastructure/Data/Repositories/IContentRepository.cs` — interface: `GetPublishedArticlesAsync`, `GetPublishedArticleBySlugAsync`, `GetAllTagsAsync` (matching data-model.md signatures)
- [X] T024 [US2] Create `src/MakeBoldSpark.Api/Infrastructure/Data/Repositories/ContentRepository.cs` — EF Core implementation: filter `Status == ArticleStatus.Published` at query level; slug lookup returns null for Draft; **must use `.Include(a => a.Tags)` explicitly** on every query that returns articles — omitting this causes N+1 queries (EF Core does not lazy-load by default without virtual navigation properties and a lazy-loading proxy); map to response records via LINQ projection (no AutoMapper — manual `.Select(a => new ArticleSummary(...))`); pass `CancellationToken` to all async EF Core methods (`.ToListAsync(ct)`, `.FirstOrDefaultAsync(ct)`) to allow caller cancellation to propagate
- [X] T025 [US2] Register `IContentRepository` → `ContentRepository` as scoped service in `Program.cs`
- [X] T026 [US2] Create `src/MakeBoldSpark.Api/Features/PublicContent/ContentService.cs` — thin service delegating to `IContentRepository`; inject via constructor; no business logic at this phase beyond delegation
- [X] T027 [US2] Create `src/MakeBoldSpark.Api/Features/PublicContent/PublicContentEndpoints.cs` — extension `MapPublicContentApi(this RouteGroupBuilder group)`: `GET /content/articles` → `GetPublishedArticles`; `GET /content/articles/{slug}` → `GetArticleBySlug`: **validate the slug before calling the service** — if slug does not match `^[a-z0-9][a-z0-9-]{0,198}[a-z0-9]$|^[a-z0-9]$` (max 200 chars, lowercase alphanumeric and hyphens only), return `Results.Problem("Invalid slug format", statusCode: 400)` immediately without reaching the repository (prevents log injection via crafted slug values); return `Results.NotFound()` as `TypedResults.NotFound()` when service returns null; `GET /content/tags` → `GetAllTags`; all `.WithOpenApi().AllowAnonymous()`; call `publicApi.MapPublicContentApi()` from `Program.cs` *(resolves critic-HP2)*
- [X] T028 [US2] Create `src/MakeBoldSpark.Api/Infrastructure/Data/Seed/SeedData.cs` — `LoadAsync(MakeBoldSparkDbContext db, CancellationToken ct)`: seeds 2 published articles (`hello-world`, `getting-started-with-makeboldspark`) + 1 draft article (`draft-article`) + tags (`general`, `intro`, `makeboldspark`, `tutorial`); guard: only runs when `db.Articles.AnyAsync()` is false

**Checkpoint**: `dotnet test --filter "Category=US2"` green. Seeded articles retrievable; draft excluded; 404 for missing/draft slugs.

---

## Phase 5: User Story 3 — Admin Route Protection (Priority: P3)

**Goal**: Admin route group enforces the `AdminOnly` policy — unauthenticated callers receive `401`.

**Independent Test**: `GET /api/admin/health/deep` without auth → `401`. With valid Admin credentials → not `401`/`403`.

### Infrastructure for User Story 3 *(must complete before tests)*

- [X] T029 [US3] Create test authentication infrastructure in `tests/MakeBoldSpark.Api.Tests/Infrastructure/Auth/` — **this task MUST complete before T030**: create `TestAuthHandler.cs` (inherits `AuthenticationHandler<AuthenticationSchemeOptions>`) that reads a `TestClaims` header from the request and populates `HttpContext.User` with those claims; create `MakeBoldSparkWebApplicationFactory.cs` (the shared factory base for all test phases) that: overrides `ConnectionStrings:DefaultConnection` to a named shared-cache SQLite (`Data Source=MakeBoldSparkTest_{Guid};Mode=Memory;Cache=Shared;`), keeps the `SqliteConnection` open for the factory lifetime, and registers `TestAuthHandler` as a named scheme `"TestScheme"` so tests can inject Admin or non-Admin claims via request headers; expose helpers `WithAdminClaims()` and `WithPublisherClaims()` *(resolves analyze-C1, critic-HP3)*

### Tests for User Story 3

- [X] T030 [P] [US3] Create `tests/MakeBoldSpark.Api.Tests/Infrastructure/Auth/AuthorizationBoundaryTests.cs` — **depends on T029**: using `MakeBoldSparkWebApplicationFactory`: test `GET /api/admin/health/deep` without any auth header returns `401`; test with `TestClaims` header carrying Admin role returns `200`; test with `TestClaims` header carrying non-Admin authenticated identity returns `403`; test `GET /api/public/content/articles` without auth header returns `200` (public route unaffected by auth boundary) *(resolves analyze-C1)*

### Implementation for User Story 3

- [X] T031 [US3] Create `src/MakeBoldSpark.Api/Features/Health/AdminHealthEndpoints.cs` — `MapAdminHealthApi(this RouteGroupBuilder group)`: registers `GET /health/deep` → checks DB connectivity via `await db.Database.CanConnectAsync()`; returns `200 OK` with `{"status":"Healthy","checks":{"database":"ok"}}` or `503 Service Unavailable` on failure; endpoint lives inside `adminApi` route group which already carries `RequireAuthorization("AdminOnly")` — no additional annotation needed; call `adminApi.MapAdminHealthApi()` from `Program.cs`

**Checkpoint**: `dotnet test --filter "Category=US3"` green. Unauthenticated → `401`; Admin → `200`; non-Admin → `403`.

---

## Phase 6: User Story 4 — API Documentation Discovery (Priority: P4)

**Goal**: Swagger UI available in Development, absent in Production.

**Independent Test**: Run in Development → `GET /swagger` returns `200` HTML page; run in Production → `GET /swagger` returns `404`.

### Tests for User Story 4

- [X] T032 [P] [US4] Create `tests/MakeBoldSpark.Api.Tests/Features/SwaggerAvailabilityTests.cs` — test with `ASPNETCORE_ENVIRONMENT=Development`: swagger endpoint returns `200`; test that all three contracts (health, articles list, article detail, tags) appear in OpenAPI JSON at `/swagger/v1/swagger.json`; test with `ASPNETCORE_ENVIRONMENT=Production`: swagger endpoint returns `404`

### Implementation for User Story 4

- [X] T033 [US4] In `Program.cs`, confirm Swagger/OpenAPI setup is already gated: `if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }` — verify the conditional was added in T017; add `builder.Services.AddEndpointsApiExplorer()` and `builder.Services.AddSwaggerGen()` (already in T017 scope); ensure health and public content endpoints use `.WithOpenApi()` (already specified in T019 and T027)

**Checkpoint**: `dotnet test --filter "Category=US4"` green. Swagger visible in dev; absent in prod.

---

## Phase 7: User Story 5 — Automated Build and Test Validation (Priority: P5)

**Goal**: GitHub Actions runs `dotnet build` + `dotnet test` on every PR to `main`; deploys on merge to `main`.

**Independent Test**: Open a PR and observe the Actions workflow completes with pass/fail status reported on the PR.

### Implementation for User Story 5 (no additional tests — CI itself is the test)

- [X] T034 [P] [US5] Create `.github/workflows/build-test.yml` — triggers on **`pull_request` to `main` only** (do NOT add a `push` trigger — `deploy.yml` already runs build+test on merge to `main`; having both trigger on `push` wastes double CI minutes with no benefit); steps: `actions/checkout@v4`, `actions/setup-dotnet@v4` (version `10.0.x`), `dotnet tool restore` (installs `dotnet-ef` from `.config/dotnet-tools.json`), `dotnet restore`, `dotnet build --configuration Release --no-restore`, `dotnet test --configuration Release --no-build --logger "trx;LogFileName=test-results.trx"`, upload test results artifact *(resolves critic-HP1, analyze-I1)*
- [X] T035 [P] [US5] Create `.github/workflows/deploy.yml` — triggers on `push` to `main`; steps: checkout, setup .NET 10, `dotnet tool restore`, restore, build, test, `dotnet publish src/MakeBoldSpark.Api/MakeBoldSpark.Api.csproj --configuration Release --output ./publish`, `azure/webapps-deploy@v3` with `app-name: makeboldspark` and `publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}`; the publish output directory MUST NOT include `data/` — explicitly exclude with `--no-build` output review; **add a post-deploy smoke test step** after deployment: `curl --fail --max-time 30 https://api.markhazleton.com/api/health` — if the health endpoint does not return 200 within 30 seconds, fail the workflow and alert the team (verifies SC-005: database survived the deployment and app started successfully) *(resolves analyze-A3)*

**Checkpoint**: PR to `main` triggers `build-test.yml` and reports status. Merge triggers `deploy.yml`.

---

## Phase 8: User Story 6 — Local Developer Setup (Priority: P6)

**Goal**: Clone → `dotnet run` → seeded DB + health endpoint in under 10 minutes.

**Independent Test**: Fresh clone, `dotnet run`, `GET /api/public/content/articles` returns seeded articles within 10 minutes.

### Tests for User Story 6

- [X] T036 [P] [US6] Create `tests/MakeBoldSpark.Api.Tests/Features/LocalSetupTests.cs` — using `WebApplicationFactory<Program>` with Development environment: test that on first start (empty SQLite) seed data is applied; test `GET /api/public/content/articles` returns at least 2 published articles; test `GET /api/health` responds; test `GET /swagger` returns `200`; test repeated startup does not re-seed (idempotency guard)

### Implementation for User Story 6

- [X] T037 [P] [US6] Create `docs/decisions/0001-single-backend-api.md` — ADR content from Jumpstart §21
- [X] T038 [P] [US6] Create `docs/decisions/0002-sqlite-default.md` — ADR content from Jumpstart §21
- [X] T039 [P] [US6] Create `docs/decisions/0003-static-web-app-clients.md` — ADR content from Jumpstart §21
- [X] T040 [P] [US6] Create `docs/decisions/0004-dotnet-10-lts.md` — ADR content from Jumpstart §21
- [X] T041 [P] [US6] Create `docs/decisions/0005-authentication-boundaries.md` — ADR content from Jumpstart §21
- [X] T042 [US6] Update `README.md` with content from Jumpstart §20: MakeBoldSpark purpose, goals, target runtime, route areas, quick-start command; add link to `quickstart.md`

**Checkpoint**: `dotnet test --filter "Category=US6"` green. Fresh `dotnet run` from clone produces seeded data.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, edge case hardening, and documentation completeness checks.

- [X] T043 [P] Verify `.gitignore` excludes: `data/*.db`, `*.db`, `*.db-shm`, `*.db-wal`, `publish/`, `*.user`, `bin/`, `obj/`; run `git log --all --full-history --diff-filter=A -- "*.db" "*.db-shm" "*.db-wal"` and confirm zero matches (SC-008: no SQLite files in git history); also run `git log --all --full-history --diff-filter=A -- "*.pfx" "*.p12" "*.env" ".env*"` to check for accidentally committed secrets or certificate files *(resolves analyze-A2)*
- [X] T044 [P] Review all `appsettings*.json` files: confirm no connection strings with passwords, no API keys, no tokens; confirm `DefaultConnection` values contain only file paths (no `Password=`, `User Id=`, or credential parameters); review git log for any commits that may have included `appsettings.*.json` changes containing potential secrets: `git log --all -p -- "src/**/appsettings*.json" | grep -i "password\|secret\|key\|token"` — zero matches required *(resolves analyze-A2)*
- [X] T045 Validate `quickstart.md` steps match the actual running application — walk through each step: clone → build → run → curl health → curl articles → curl swagger; note any discrepancies and update `quickstart.md`
- [X] T046 Add `data/` directory with `.gitkeep` and ensure `data/*.db` is in `.gitignore`; create `data/seed/` placeholder for future JSON seed files
- [X] T047 [P] Run `dotnet test` (full suite) and confirm all tests pass with zero failures
- [X] T048 [P] Run `dotnet build --configuration Release` and confirm zero warnings (treat warnings as errors if possible via `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in `.csproj`)

---

## Dependencies & Execution Order

### Phase Dependencies

```text
Phase 1 (Setup)
  └─→ Phase 2 (Foundational) ← BLOCKS all story phases
        ├─→ Phase 3 (US1 Health) ← MVP complete here
        ├─→ Phase 4 (US2 Public Content)
        ├─→ Phase 5 (US3 Admin Boundary)
        ├─→ Phase 6 (US4 Swagger)
        ├─→ Phase 7 (US5 CI/CD)
        └─→ Phase 8 (US6 Local Setup)
              └─→ Phase 9 (Polish)
```

### User Story Dependencies

| Story | Depends On | Notes |
|-------|-----------|-------|
| US1 Health (P1) | Phase 2 complete | Independent — no data layer needed |
| US2 Content (P2) | Phase 2 + T013–T016 (DbContext, entities, migration) | Content repository needs DB |
| US3 Admin (P3) | Phase 2 + T010 (auth policies) + any admin route | Auth boundary test needs a registered admin endpoint |
| US4 Swagger (P4) | T019, T027 (endpoints using .WithOpenApi()) | Swagger lists existing endpoints |
| US5 CI/CD (P5) | Phase 1 (buildable solution) | Workflows don't depend on feature completion |
| US6 Local Setup (P6) | All above complete | Validates the full stack end-to-end |

### Within Each User Story

- Tests created FIRST (must FAIL before implementation)
- Models before services (T022 before T023–T024)
- Repository/service before endpoints (T024–T025 before T027)
- Seed data before integration tests (T028 before T021 full run)

### Parallel Opportunities

```text
Phase 1:  T003 ‖ T004 (project creation can overlap)
Phase 2:  T008 ‖ T009 (config files) → then T010 ‖ T011 ‖ T012 ‖ T013
          T013 → T014 → T015 (sequential: DbContext → entities → migration)
Phase 3:  T018 (test) created, then T019 (impl)
Phase 4:  T020 ‖ T021 ‖ T022 ‖ T023 (tests + models in parallel, then T024 → T025 → T026 → T027 → T028)
Phase 5:  T029 (TestAuthHandler infrastructure) → T030 [P] (AuthorizationBoundaryTests) ‖ T031 (AdminHealthEndpoints)
Phase 6:  T032 (test) then T033 (one task)
Phase 7:  T034 ‖ T035 (two workflow files, fully independent)
Phase 8:  T036 (test) then T037 ‖ T038 ‖ T039 ‖ T040 ‖ T041 (ADRs all independent) → T042
Phase 9:  T043 ‖ T044 ‖ T047 ‖ T048 (all independent)
```

---

## Parallel Execution Examples

### Phase 2 Parallel Batch

```text
Batch A (start immediately):
  T008 — Write appsettings.json
  T009 — Write appsettings.Development.json

Batch B (after T002 solution exists):
  T010 — AuthorizationSetup.cs
  T011 — CorsSetup.cs
  T012 — RequestLoggingMiddleware.cs
  T013 — MakeBoldSparkDbContext.cs

Sequential after Batch B:
  T014 — Entities (Article, Tag, ArticleStatus)
  T015 — EF migration
  T016 — DatabaseSetup.cs
  T017 — Program.cs (wires everything)
```

### Phase 4 Parallel Batch (US2)

```text
Batch A (parallel):
  T020 — ContentRepositoryTests.cs
  T021 — PublicContentEndpointTests.cs
  T022 — ContentModels.cs
  T023 — IContentRepository.cs

Sequential after Batch A:
  T024 — ContentRepository.cs
  T025 — Register DI
  T026 — ContentService.cs
  T027 — PublicContentEndpoints.cs
  T028 — SeedData.cs
```

---

## Gate Acknowledgements

All `analyze` and `critic` gate findings have been resolved in-place in this tasks.md. No proceed-anyway overrides required.

| Gate | Finding ID | Severity | Resolution | Applied In |
|------|-----------|----------|-----------|-----------|
| critic | SS-1 | Showstopper | Register `AddBearerToken()` auth scheme alongside policies | T010 |
| critic | SS-2 | Showstopper | Add `public partial class Program { }` to Program.cs | T017 |
| critic | CR-1 | Critical | Use `await db.Database.MigrateAsync(ct)` | T016 |
| critic | CR-2 | Critical | Add `Journal Mode=WAL;Cache=Shared;` to connection strings | T008, T009 |
| critic | CR-3 | Critical | Use named shared-cache SQLite + `MakeBoldSparkWebApplicationFactory` | T020, T021, T029 |
| critic | HP-1 | High | Remove `push` trigger from `build-test.yml` | T034 |
| critic | HP-2 | High | Add slug regex validation at endpoint layer | T027 |
| critic | HP-3 | High | `MakeBoldSparkWebApplicationFactory` overrides connection string | T029 |
| critic | HP-4 | High | Add `UseShutdownTimeout(15s)` to Program.cs | T017 |
| analyze | C1 | High | Restructure Phase 5: T029=infra first, T030=tests, T031=impl | Phase 5 |
| analyze | U1 | Medium | Migration error handling + try/catch in DatabaseSetup | T016 |
| analyze | U2 | Medium | Path writability check before migration | T016 |
| analyze | U3 | Medium | Add logging middleware test task | T012a |
| analyze | I1 | Low | Add dotnet-tools.json + `dotnet tool restore` in CI | T001a, T034, T035 |
| analyze | A1 | Low | Add Stopwatch timing assertion (< 500ms) to health test | T018 |
| analyze | A2 | Low | Add git log history scan in Polish phase | T043, T044 |
| analyze | A3 | Low | Add post-deploy smoke test curl step | T035 |
| analyze | A4 | Low | Add `Content-Type: application/problem+json` assertion | T021 |

---

## Implementation Strategy

### MVP First (User Story 1 — Health Check)

1. Complete Phase 1: Setup (T001–T007)
2. Complete Phase 2: Foundational (T008–T017)
3. Complete Phase 3: US1 Health (T018–T019)
4. **STOP AND VALIDATE**: `dotnet test` passes; `curl /api/health` returns `200`
5. This is the minimum deployable slice — proves the platform stands up

### Incremental Delivery Order

1. Phases 1–3 → MVP (health endpoint proves platform boots)
2. Phase 4 (US2) → Public content proves the data layer end-to-end
3. Phase 5 (US3) → Auth boundary proves the security model
4. Phase 7 (US5) → CI/CD proves the delivery pipeline (can overlap with Phase 5/6)
5. Phase 6 (US4) + Phase 8 (US6) → Polish developer experience
6. Phase 9 → Final hardening

### Total Task Count

| Phase | Tasks | Notes |
|-------|-------|-------|
| Phase 1: Setup | 8 (T001, T001a, T002–T007) | Sequential scaffold + tools manifest |
| Phase 2: Foundational | 12 (T008–T017 + T012a + T001a) | Critical path; T012a = logging test |
| Phase 3: US1 | 2 (T018–T019) | MVP increment |
| Phase 4: US2 | 9 (T020–T028) | Core content feature |
| Phase 5: US3 | 3 (T029–T031) | Auth infra first, then tests + impl |
| Phase 6: US4 | 2 (T032–T033) | Swagger gate |
| Phase 7: US5 | 2 (T034–T035) | CI/CD workflows |
| Phase 8: US6 | 7 (T036–T042) | Local setup + ADRs |
| Phase 9: Polish | 6 (T043–T048) | Hardening |
| **Total** | **52 tasks** | +4 added for gate findings |

Parallel opportunities: 24 tasks marked [P].

---

## Notes

- [P] tasks = work on different files with no unmet dependencies in the same phase — safe to parallelize
- [Story] label maps each task to a user story for traceability and independent testing
- Each user story phase ends with a checkpoint — validate independently before moving on
- Seed data (T028) is required before US2 integration tests run against a real (temp) SQLite DB
- T029 (test auth infrastructure / `MakeBoldSparkWebApplicationFactory`) is a prerequisite for T030 and all other WebApplicationFactory-based tests — complete it in Phase 5 before any other test phase runs
- All WebApplicationFactory-based tests MUST use `MakeBoldSparkWebApplicationFactory` (from T029) to override the connection string to a named shared-cache SQLite — tests that use the default factory will attempt to open `/home/data/makeboldspark.db` and fail
- `.gitignore` must exclude `*.db` before any `dotnet run` that creates `data/makeboldspark.local.db`
