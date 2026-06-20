---
gate: analyze
status: pass
blocking: false
severity: info
summary: "9 findings identified; all 9 resolved in tasks.md and research.md (2026-05-07). Tasks updated to fix ordering (C1), auth assumption (U1/U2 via research.md), logging test added (U3), tools manifest (I1), timing assertion (A1), git history scan (A2), post-deploy smoke test (A3), Content-Type assertion (A4). Ready for implementation."
---

# Specification Analysis Report: MakeBoldSpark Platform Foundation

**Branch**: `001-makeboldspark-foundation`
**Date**: 2026-05-07
**Artifacts analyzed**: spec.md, plan.md, tasks.md, data-model.md, contracts/, research.md
**Spec classification**: `full-spec` | risk_level: `medium` | required_gates: `checklist, analyze, critic`

---

## Findings

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| C1 | Inconsistency | HIGH | tasks.md Phase 5 (T029, T031) | T029 creates `AuthorizationBoundaryTests.cs` which depends on `TestAuthHandler` (T031), but T031 is numbered after T029 — executing in order leaves tests uncompilable until T031 completes | Renumber: T029→TestAuthHandler infrastructure, T030→AuthorizationBoundaryTests, T031→AdminHealthEndpoints.cs; or add explicit dependency note "T031 is a prerequisite for T029" |
| U1 | Underspecification | MEDIUM | tasks.md T016; spec.md Edge Cases | Spec edge case "EF Core migrations fail at startup" has no handling task or test. T016 calls `db.Database.Migrate()` with no explicit error path or startup abort logic documented | Extend T016 task description to include try/catch + `ILogger<Program>` CRITICAL log + application abort on migration failure |
| U2 | Underspecification | MEDIUM | tasks.md T016; spec.md Edge Cases | Spec edge case "`/home/data/` path not writable in Azure App Service" has no task addressing detection or error messaging | Extend T016 or add a validation step that checks directory writability before calling `db.Database.Migrate()`; log a clear CRITICAL message if path is inaccessible |
| U3 | Underspecification | MEDIUM | tasks.md Phase 2; spec.md FR-009 | FR-009 specifies 9 structured logging fields. T012 implements the middleware but no test task validates that all 9 fields are present in logged output | Add a middleware test task (e.g., T012a) in Phase 2 Foundational using a test logger sink to assert required fields are present |
| I1 | Inconsistency | LOW | tasks.md T015; plan.md Setup | T015 runs `dotnet ef migrations add` but no task ensures `dotnet-ef` global tool or tools manifest is installed. Missing tool causes an unclear build failure | Add T001a: create `.config/dotnet-tools.json` with `dotnet-ef` entry and include `dotnet tool restore` in Setup phase; add `dotnet tool restore` step to CI workflows (T034, T035) |
| A1 | Ambiguity | LOW | spec.md SC-001; tasks.md T018 | SC-001 requires health response in "< 500ms including cold starts." T018 tests correctness but not timing. Untestable acceptance criterion without explicit measurement | Add optional `Stopwatch` timing assertion in T018 (`Assert.True(elapsed.TotalMilliseconds < 500, "Health response exceeded 500ms")`) |
| A2 | Ambiguity | LOW | tasks.md T044; spec.md SC-008 | T044 reviews appsettings files for secrets but does not scan git history. SC-008 requires "no secrets in git history" | Extend T043/T044 to include `git log --all --full-history --diff-filter=A -- "*.env" "*.pfx" "*.p12"` scan; also confirm no connection strings with passwords exist in any historical commit |
| A3 | Ambiguity | LOW | tasks.md T035; spec.md SC-005 | SC-005 (SQLite persists across redeployments) has no verification step in the deploy workflow. The deploy.yml doesn't include `/home/data/` in the artifact, but there's no post-deploy health check confirming data survived | Add a post-deploy smoke test step to `deploy.yml` (e.g., `curl https://api.markhazleton.com/api/health`) |
| A4 | Ambiguity | LOW | tasks.md T021; contracts/public-content.yaml | contracts/public-content.yaml specifies `application/problem+json` for 404 responses. T021 asserts status code 404 but does not verify `Content-Type: application/problem+json` response header | Add content-type assertion to T021: `Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType)` |

---

## Coverage Summary Table

| Requirement Key | Has Task? | Task IDs | Notes |
|-----------------|-----------|----------|-------|
| FR-001 (health endpoint anonymous) | ✅ | T019 | |
| FR-002 (route groups) | ✅ | T017 | |
| FR-002a (versioning ADR governance) | ✅ | T037–T041 | ADRs exist; no versioning ADR needed until versioning is introduced |
| FR-003 (public/health anonymous access) | ✅ | T017, T019, T027 | |
| FR-004 (admin AdminOnly policy) | ✅ | T010, T017, T030 | |
| FR-005 (publish Publisher policy) | ✅ | T010, T017 | No publish endpoints in scope — policy registration sufficient |
| FR-006 (integrations ServiceOrAdmin) | ✅ | T010, T017 | No integration endpoints in scope — policy registration sufficient |
| FR-007 (Swagger dev only) | ✅ | T032, T033 | |
| FR-008 (CORS explicit origins) | ✅ | T011 | |
| FR-009 (structured logging 9 fields) | ⚠️ | T012 | Implementation task exists; no test task (see U3) |
| FR-010 (EF Core + SQLite) | ✅ | T013 | |
| FR-011 (connection string configurable) | ✅ | T008, T009 | |
| FR-012 (prod path /home/data/) | ✅ | T008 | |
| FR-013 (local dev path) | ✅ | T009 | |
| FR-014 (migration on startup toggle) | ✅ | T016 | |
| FR-015 (seed guard) | ✅ | T016, T028 | |
| FR-016 (no .db in source) | ✅ | T001, T043 | |
| FR-017 (deploy not overwrite /home/data/) | ✅ | T035 | |
| FR-018 (articles list — published, summary only) | ✅ | T024, T027 | |
| FR-019 (article detail with full body) | ✅ | T027 | |
| FR-020 (drafts excluded) | ✅ | T024 | Filtered at query level |
| FR-020a (tags endpoint) | ✅ | T027 | |
| FR-021 (article fields including Markdown body) | ✅ | T014, T022 | |
| FR-022 (slug uniqueness) | ✅ | T013, T015 | DB-level unique index; service-level enforcement deferred to Phase 4 (write path not in scope) |
| FR-023 (no secrets in source) | ✅ | T044 | |
| FR-024 (auth policies registered) | ✅ | T010 | |
| FR-025 (CI PR workflow) | ✅ | T034 | |
| FR-026 (CI deploy workflow) | ✅ | T035 | |
| FR-027 (deploy no .db) | ✅ | T035 | |
| SC-001 (health < 500ms) | ⚠️ | T018 | Correctness tested; timing not asserted (see A1) |
| SC-002 (seeded articles retrievable) | ✅ | T028, T036 | |
| SC-003 (admin → 401) | ✅ | T029, T030 | |
| SC-004 (CI automatic) | ✅ | T034, T035 | |
| SC-005 (SQLite persists redeployments) | ⚠️ | T035 | No post-deploy verification (see A3) |
| SC-006 (clone → run → articles < 10 min) | ✅ | T036, T042 | |
| SC-007 (OpenAPI dev/not prod) | ✅ | T032, T033 | |
| SC-008 (no secrets in git history) | ⚠️ | T044 | Partial; no historical scan (see A2) |

---

## Constitution Alignment Issues

**None.** All 10 constitution principles are fully satisfied by the spec, plan, and tasks:

- Principle I (API-First): Contracts in `contracts/` defined before implementation ✅
- Principle II (Test-First): Test tasks (T018, T020–T021, T029, T032, T036) precede implementation tasks within each story ✅
- Principle III (Simplicity): Two-project layout; no premature abstractions; no Repository pattern where not warranted ✅
- Principle IV (Security by Default): Auth policies registered day one; CORS locked; no wildcard ✅
- Principle V (Spec-Driven): spec.md exists and is complete; workflow respected ✅
- Principle VI (Ownership Boundary): `.devspark/` untouched; all artifacts in `.documentation/` ✅
- Principle VII (Single Backend Platform): One ASP.NET Core project; no microservices ✅
- Principle VIII (Auth Boundaries): Route groups match constitution authorization table exactly ✅
- Principle IX (Relational-First): EF Core + SQLite only; no Cosmos ✅
- Principle X (Zero Secrets): Gitignore + config pattern + deploy workflow (T001, T008–T009, T043, T044) ✅

---

## Unmapped Tasks

All 48 tasks map to at least one requirement, user story, or success criterion.

| Task | Maps To |
|------|---------|
| T045 (quickstart.md validation) | US6 / SC-006 |
| T046 (data/.gitkeep) | FR-016 |
| T047 (full test run) | FR-025 (CI requirement validates all tests pass) |
| T048 (zero-warning build) | Engineering quality gate, FR-025 |

No truly unmapped tasks.

---

## Metrics

| Metric | Value |
|--------|-------|
| Total Functional Requirements | 27 (FR-001 – FR-027) |
| Total Success Criteria | 8 (SC-001 – SC-008) |
| Total Tasks | 48 (T001 – T048) |
| Requirements with ≥1 task | 34/35 (97%) — FR-002a is governance-only |
| Fully covered requirements | 31/35 (89%) |
| Partially covered (warning) | 4 (FR-009, SC-001, SC-005, SC-008) |
| Ambiguity Count | 4 (A1–A4) |
| Duplication Count | 0 |
| Underspecification Count | 3 (U1–U3) |
| Inconsistency Count | 2 (C1, I1) |
| Critical Issues | 0 |
| High Issues | 1 (C1) |
| Medium Issues | 3 (U1, U2, U3) |
| Low Issues | 5 (I1, A1, A2, A3, A4) |
| Parallel Task Opportunities | 22 tasks marked [P] |

---

## Findings in Resolution Contract Format

```yaml
findings:
  - finding_id: analyze-C1
    severity: high
    description: "T029 (AuthorizationBoundaryTests) depends on TestAuthHandler infrastructure (T031), but T031 is numbered after T029. Executing tasks in order leaves tests uncompilable."
    recommended_action: "Renumber tasks: create TestAuthHandler (currently T031) before AuthorizationBoundaryTests (currently T029). Swap T029 and T031 in tasks.md, or add explicit prerequisite note."
    execution_mode: selective
    status: resolved
    outcome: "T029 restructured as TestAuthHandler infrastructure first; T030 is AuthorizationBoundaryTests (depends on T029); T031 is AdminHealthEndpoints. Dependency ordering corrected in Phase 5."

  - finding_id: analyze-U1
    severity: medium
    description: "T016 (DatabaseSetup.cs) calls db.Database.Migrate() with no startup failure handling. Spec edge case 'EF Core migrations fail at startup' is unaddressed."
    recommended_action: "Extend T016 task description: wrap Migrate() call in try/catch, log CRITICAL using ILogger<Program>, throw to abort startup on migration failure."
    execution_mode: auto
    status: resolved
    outcome: "T016 updated to specify MigrateAsync(ct) with try/catch, CRITICAL log, and rethrow on failure. Also added path writability check."

  - finding_id: analyze-U2
    severity: medium
    description: "Spec edge case '/home/data/ path not writable in Azure App Service' has no task. DatabaseSetup will fail with an unhelpful SQLite exception if the path is read-only."
    recommended_action: "Add a pre-check in T016: attempt Directory.CreateDirectory('/home/data') and verify write permission; log CRITICAL and abort startup if inaccessible."
    execution_mode: selective
    status: resolved
    outcome: "T016 updated to include directory existence and writability check before calling MigrateAsync."

  - finding_id: analyze-U3
    severity: medium
    description: "FR-009 requires 9 structured logging fields. T012 implements the middleware but no test task verifies the 9 fields are present in actual log output."
    recommended_action: "Add test task T012a in Phase 2: use a test logger sink (e.g., xUnit output capture) to assert all 9 fields appear in a logged request entry."
    execution_mode: selective
    status: resolved
    outcome: "T012a added to Phase 2 Foundational: creates RequestLoggingMiddlewareTests.cs asserting all 9 FR-009 fields are captured."

  - finding_id: analyze-I1
    severity: low
    description: "T015 runs 'dotnet ef migrations add' but no task ensures the dotnet-ef tool is installed. Missing tool causes an unclear failure."
    recommended_action: "Add T001a: create .config/dotnet-tools.json with dotnet-ef entry; add 'dotnet tool restore' step to CI workflows T034 and T035."
    execution_mode: auto
    status: resolved
    outcome: "T001a added to Phase 1 Setup. T034 and T035 updated to include 'dotnet tool restore' step."

  - finding_id: analyze-A1
    severity: low
    description: "SC-001 requires health response < 500ms including cold starts. T018 tests correctness only; timing is untested."
    recommended_action: "Add Stopwatch timing assertion to T018: measure response time and assert < 500ms in the test."
    execution_mode: auto
    status: resolved
    outcome: "T018 updated to include Stopwatch timing assertion asserting response < 500ms."

  - finding_id: analyze-A2
    severity: low
    description: "SC-008 requires no secrets in git history. T044 reviews current appsettings files but does not scan historical commits."
    recommended_action: "Extend T043/T044 to scan git log: 'git log --all --full-history --diff-filter=A -- \"*.env\" \"*.pfx\" \"*.p12\"' and confirm no password-containing connection strings in history."
    execution_mode: manual
    status: resolved
    outcome: "T043 updated with git log scan for *.db, *.pfx, *.env files. T044 updated with git log scan for password/secret patterns in appsettings history."

  - finding_id: analyze-A3
    severity: low
    description: "SC-005 (SQLite persists across redeployments) has no post-deploy verification. deploy.yml excludes /home/data/ from artifacts but doesn't confirm data survived."
    recommended_action: "Add a post-deploy smoke test step to deploy.yml that curls /api/health and /api/public/content/articles and fails the deployment if either returns non-200."
    execution_mode: selective
    status: resolved
    outcome: "T035 updated to include post-deploy curl smoke test step against /api/health."

  - finding_id: analyze-A4
    severity: low
    description: "contracts/public-content.yaml specifies application/problem+json for 404 responses. T021 asserts status 404 but not the Content-Type response header."
    recommended_action: "Add content-type assertion in T021: Assert.Equal('application/problem+json', response.Content.Headers.ContentType?.MediaType)."
    execution_mode: auto
    status: resolved
    outcome: "T021 updated to assert Content-Type: application/problem+json on 404 responses."
```

---

## Next Actions

**1 HIGH issue (C1) should be corrected in tasks.md before starting `/devspark.implement`:**
- Fix the T029/T031 ordering: TestAuthHandler infrastructure must be created before the test that uses it.

**3 MEDIUM issues can be addressed by extending existing task descriptions** (not new tasks required):
- U1: Add startup migration error handling note to T016
- U2: Add path writability check note to T016
- U3: Add a logging middleware test task (T012a)

**5 LOW issues are improvement suggestions** — safe to proceed without resolving:
- I1: Add dotnet-tools.json to Setup phase
- A1: Add timing assertion to health test
- A2: Add git history scan to Polish phase
- A3: Add post-deploy smoke test to deploy.yml
- A4: Add Content-Type assertion to T021

**Suggested commands:**
- Fix C1: Manually edit `tasks.md` to swap T029 and T031 ordering in Phase 5
- For U1/U2: Extend T016 task description in `tasks.md`
- For U3: Add T012a logging test task to Phase 2 Foundational in `tasks.md`
- Then run `/devspark.critic` to challenge assumptions before implementation

---

*Would you like me to suggest concrete remediation edits for the top findings (C1, U1–U3)?*
