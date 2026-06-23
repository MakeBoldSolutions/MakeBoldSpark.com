---
participants:
  owner: human
  planner: ai
  implementer: ai
  reviewer: human
  critic: ai
  scribe: ai
---

# Implementation Plan: CMS Admin App

**Branch**: `004-cms-admin-app` | **Date**: 2026-06-22 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `.documentation/specs/004-cms-admin-app/spec.md`

## Rationale Summary

### Core Problem

`MakeBoldSpark.Api` already exposes full CRUD over 11 CMS entity types, but there is no UI for it, and — more critically — the platform has no way to verify who is asking: `Jwt:Authority`/`Jwt:Audience` are empty everywhere (`appsettings.json:12-13`), so `AuthorizationSetup.cs:32-48` disables signature validation and accepts any well-formed bearer token as proof of administrator identity.

### Decision Summary

Build the admin UI as a new in-solution `.NET` project (`MakeBoldSpark.Cms`) whose React build is embedded as manifest resources and mounted into `MakeBoldSpark.Api` via one extension method — mirroring `ApiTestSpark`'s embedding mechanism without its NuGet-publishing overhead. Pair it with a minimal `POST /api/public/auth/login` endpoint that verifies an `Author`'s email/password and issues a properly signed JWT, closing the credential-verification gap instead of papering over it with another client-side workaround.

### Key Drivers

- Business driver: a usable admin surface for the 11 CMS entity types, reusing exactly what the API already supports
- Technical constraint: the platform must keep verifying tokens it issues itself once a real signing key exists — leaving the current "accept anything" fallback in place after adding login would be worse than today's status quo (a real-looking login screen in front of a still-forgeable token)
- User/operational impact: closes a real security gap (FR-001b, FR-014) while adding no new datastore, no new hosting model, and no new external dependency

### Source Inputs

- `spec.md` (this feature) — all 14 functional requirements, 6 success criteria, and 5 resolved clarifications
- `AuthorizationSetup.cs`, `appsettings.json`, `CorsSetup.cs`, `Program.cs` (current auth/static-file/CORS wiring)
- `.documentation/frontend/makeboldspark-api-guide.md` (existing TypeScript types, `apiFetch`/`adminCrud<T>` client pattern, full-replace PUT semantics, entity gotchas)
- ApiTestSpark's `NUGET-PACKAGE-WALKTHROUGH.md` (embedded-resource SPA mechanism: `BuildReactSpa` MSBuild target, `EmbeddedResource` + `Link`, `ManifestEmbeddedFileProvider`, `Map*()` extension)
- `.documentation/branding/MakeBoldSolutions/` and the already-extracted `wwwroot/assets/makebold/brand.css` + fonts (confirmed present in 5 build-output locations) used by the platform's existing public pages
- Prior specs `002-recipe-api`, `003-makeboldspark-api`

### Tradeoffs Considered

- Option A — keep the original "paste a token" / client-side "Generate Dev Token" convenience discussed before this plan: superseded. Once a real login endpoint exists, a client-constructible unsigned token is strictly worse than not having one — it would coexist with real auth and remain forgeable unless the signing fallback is also removed. Both changes are now done together.
- Option B — issue the access token via ASP.NET Core Identity's full membership system (Identity tables, `UserManager`, cookie or hybrid auth): rejected — would add a parallel account system on top of `Author`, which already models email/password/`isAdmin`. Using `PasswordHasher<TUser>` directly (the same vetted PBKDF2 implementation Identity uses) gets the security property without the schema/dependency footprint.
- Option C — package `MakeBoldSpark.Cms` as a publishable NuGet package like `ApiTestSpark`: rejected per explicit user direction — single consumer, in-solution `ProjectReference` is sufficient and avoids CI/publishing overhead.
- Selected — minimal self-issued JWT login (symmetric signing key, `PasswordHasher<TUser>`, built-in ASP.NET Core rate limiting on the login route) + embedded-resource `MakeBoldSpark.Cms` project referenced from `MakeBoldSpark.Api`.

### Architectural Impact

- Adds one new feature folder (`Features/Auth/`) to `MakeBoldSpark.Api`, following the existing `{FeatureName}Endpoints.cs` / `Service.cs` / `Models.cs` convention.
- Adds one new in-solution project (`MakeBoldSpark.Cms`) referenced via `ProjectReference` — stays within Principle VII (single backend platform); no new deployable unit.
- Changes existing behavior in `AuthorizationSetup.cs`: the no-authority "accept any well-formed token" fallback is removed and replaced with required signature validation against a self-issued symmetric key. This is a deliberate breaking change to a known-insecure default — anything currently relying on that fallback (nothing identified beyond this feature) stops working, by design.
- **Deployment-ordering risk (identified by `/devspark.critic`, finding critic-001)**: `AddMakeBoldSparkAuth` registers JWT authentication globally during service configuration, for the whole API — not just admin/CMS routes. Once the fail-fast guard above ships, deploying it to any environment where `Jwt:SigningKey`/`Jwt:Authority` isn't yet configured takes down the **entire API**, not only this feature. Mitigated by a pre-deploy runbook check (tasks.md T009a) and a startup-failure message that names the missing setting explicitly, rather than a generic exception.
- No new datastore — reuses `MakeBoldSparkCoreDbContext` and the existing `Author` table; the `password` column's contents change meaning from "stored as entered" to "PBKDF2 hash" going forward. A unique index on `Author.email` is added (tasks.md T054a) to enforce, rather than merely assume, the sign-in matching invariant the resolved clarification relies on.
- Login throttling (FR-014) is two-layered, not single-layered: a per-email fixed-window limiter (deters guessing against one known account) plus a per-IP/global fixed-window limiter (deters spraying a few common passwords across many different emails) — see `research.md` and `contracts/auth-api.md` for the corrected design (originally email-only; broadened per `/devspark.critic` finding critic-002).

### Reviewer Guidance

Focus review on: (1) the `AuthorizationSetup.cs` change removing the insecure fallback, **and the deployment-ordering risk it introduces** — confirm the pre-deploy runbook check (T009a) actually runs before this ships anywhere beyond local dev; (2) the constrained `/api/public/auth/*` authorization category for the new login route; and (3) the one-time bootstrapping step required to set a real password hash for at least one existing `Author` row before anyone can sign in, and its companion runbook entry (T014) for provisioning future administrators without re-deriving the steps.

### Gate Findings Resolved

`/devspark.analyze` and `/devspark.critic` were run against spec.md/plan.md/tasks.md prior to implementation. One SHOWSTOPPER (critic-001, deployment ordering) and one CRITICAL (critic-002, throttle scope) were identified along with several HIGH/MEDIUM coverage gaps; all twelve findings were resolved by adding tasks (T008a, T009a, T019a, T019b, T020 [expanded], T022 [expanded], T031a, T031b, T031c, T029a–c, T039 [expanded], T054a) and by adding `risk_profile`/`change_type`/`archetype` to spec.md's frontmatter. See `gates/analyze.md` and `gates/critic.md` for the full reports.

## Summary

Add a self-issued, password-verified sign-in endpoint to `MakeBoldSpark.Api`, tighten JWT validation to actually check the signature it produces, and ship a React admin SPA — covering all 11 CMS entity types via one generic list/edit pattern — embedded into `MakeBoldSpark.Api` as a referenced `.NET` project and served at `/cms`.

## Technical Context

**Language/Version**: C# / .NET 10 (backend, matches existing `MakeBoldSpark.Api`); TypeScript + React 18 (admin SPA)
**Primary Dependencies**: `Microsoft.AspNetCore.Authentication.JwtBearer` (already referenced) for validation; `Microsoft.AspNetCore.Identity` (new — `PasswordHasher<TUser>` only, no Identity tables/membership system) for password hashing; built-in `Microsoft.AspNetCore.RateLimiting` middleware (ships in the shared framework, no new package) for login throttling; Vite + React + a format-while-you-type rich text component (FR-010) on the frontend
**Storage**: Existing SQLite via `MakeBoldSparkCoreDbContext` / `Author` table — no new store, no schema change (the `password` column is reused to hold a hash instead of plaintext)
**Testing**: xUnit (`tests/MakeBoldSpark.Api.Tests`, existing `WebApplicationFactory` pattern) for the login endpoint and the tightened `AuthorizationSetup`; a lightweight component/unit test setup for the React app (Vitest + React Testing Library) for the generic CRUD component and login form
**Target Platform**: Azure App Service Linux B1 (existing host); browser (SPA), served same-origin from the same host — no new CORS surface
**Project Type**: Web application — single ASP.NET Core host with one new in-solution project providing an embedded admin SPA
**Performance Goals**: No new throughput targets; matches the platform's existing low-volume personal/portfolio scale (Constitution: Platform Architecture). Sign-in should complete well under 1 second under normal load, independent of the throttle delay applied on repeated failures
**Constraints**: Signing key MUST NOT be committed to source control (Principle X) — local dev via `dotnet user-secrets`, deployed environments via Azure App Service application settings; no new datastore (Principle IX); stays inside the single ASP.NET Core host (Principle VII)
**Scale/Scope**: A handful of administrator accounts; all 11 existing CMS entity types; no multi-tenant or high-concurrency requirements

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design — see below.*

| Principle | Status | Notes |
|---|---|---|
| I. API-First (NON-NEGOTIABLE) | PASS | New `POST /api/public/auth/login` contract defined in Phase 1 (`contracts/auth-api.md`) before implementation |
| II. Test-First (NON-NEGOTIABLE) | PASS | `tasks.md` sequences login/authorization tests before/alongside implementation, per existing `MakeBoldSparkWebApplicationFactory` patterns |
| III. Simplicity (NON-NEGOTIABLE) | PASS | Reuses `Author` table instead of a new account system; reuses built-in rate limiting instead of a new package; one generic CRUD component instead of 11 bespoke ones |
| IV. Security by Default (NON-NEGOTIABLE) | PASS | Password hashing, generic failure messages, baseline throttle, secrets out of source control — all addressed; flagged for explicit security review per Reviewer Guidance |
| V. Spec-Driven Development | PASS | specify → clarify → plan, in order |
| VI. Ownership Boundary | PASS | No changes under `.devspark/` |
| VII. Single Backend Platform (NON-NEGOTIABLE) | PASS | `MakeBoldSpark.Cms` is an in-solution `ProjectReference`, not a separate service/repo |
| VIII. Clear Authorization Boundaries (NON-NEGOTIABLE) | PASS | Constitution v1.2.0 explicitly permits `/api/public/auth/*` for anonymous credential verification and token issuance only |
| IX. Relational-First Data Strategy (NON-NEGOTIABLE) | PASS | No new datastore; existing SQLite/EF Core `Author` table reused |
| X. Zero Secrets in Source Control (NON-NEGOTIABLE) | PASS | Signing key sourced from `dotnet user-secrets` (dev) / Azure App Service settings (deployed); never in `appsettings*.json` |

### Constitution Waiver Resolution

The temporary Principle VIII waiver for `POST /api/public/auth/login` was resolved by the
owner-ratified Constitution v1.2.0 amendment on 2026-06-23. The new `/api/public/auth/*`
category permits anonymous credential verification and signed-token issuance only; it does not
permit CMS-data disclosure or CMS-data writes.

## Project Structure

### Documentation (this feature)

```text
.documentation/specs/004-cms-admin-app/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md         # Phase 1 output
├── quickstart.md         # Phase 1 output
├── contracts/
│   ├── auth-api.md        # POST /api/public/auth/login contract
│   └── cms-app-hosting.md # /cms static/SPA-fallback routing contract
├── checklists/
│   └── requirements.md   # Already created by /devspark.specify
└── tasks.md              # Phase 2 output (/devspark.tasks — not created here)
```

### Source Code (repository root)

```text
src/
├── MakeBoldSpark.Api/
│   ├── Features/
│   │   └── Auth/                          # NEW
│   │       ├── AuthEndpoints.cs           # POST /api/public/auth/login
│   │       ├── AuthService.cs             # password verification, token issuance
│   │       └── AuthModels.cs              # LoginRequest, LoginResponse
│   ├── Infrastructure/Auth/
│   │   └── AuthorizationSetup.cs          # MODIFIED — require signature validation against the new signing key; remove the insecure no-authority fallback
│   ├── Program.cs                          # MODIFIED — map auth endpoint, rate limiter, app.MapMakeBoldSparkCms()
│   ├── appsettings.json                    # MODIFIED — Jwt:SigningKey documented as required (value via user-secrets/App Service, not committed)
│   └── MakeBoldSpark.Api.csproj            # MODIFIED — ProjectReference to MakeBoldSpark.Cms; PackageReference to Microsoft.AspNetCore.Identity
├── MakeBoldSpark.Cms/                       # NEW — embedded-resource SPA host
│   ├── MakeBoldSpark.Cms.csproj            # BuildReactSpa MSBuild target + EmbeddedResource glob
│   ├── MakeBoldSparkCmsExtensions.cs       # MapMakeBoldSparkCms()
│   └── client/                              # Vite + React + TS source
│       ├── package.json
│       ├── vite.config.ts                  # base: '/cms/'; build.outDir -> ../build
│       ├── index.html                       # references existing /assets/makebold/brand.css, no asset duplication
│       └── src/
│           ├── main.tsx
│           ├── App.tsx                      # router + sidebar nav
│           ├── auth/
│           │   ├── AuthContext.tsx          # holds access token in sessionStorage
│           │   └── LoginPage.tsx            # email/password form against /api/public/auth/login
│           └── cms/
│               ├── EntityCrudPage.tsx       # generic list+edit table
│               ├── entityConfigs.ts         # field/column definitions for all 11 resources
│               └── overrides/               # Menu tree view, Newsletter (no edit), masked-password fields
├── MakeBoldSpark.Core/                       # UNCHANGED — Author entity already has email/password/isAdmin
└── MakeBoldSpark.Recipe/                     # UNCHANGED

tests/
└── MakeBoldSpark.Api.Tests/
    └── Features/Auth/                        # NEW — login success/failure/throttle/non-admin-reject tests
```

**Structure Decision**: Single ASP.NET Core host (`MakeBoldSpark.Api`) gains one new feature folder (`Features/Auth/`) and one new in-solution project (`MakeBoldSpark.Cms`), referenced via `ProjectReference` and registered in `MakeBoldSpark.slnx`. No new deployable unit, no new datastore — matches the existing `MakeBoldSpark.Core`/`MakeBoldSpark.Recipe` pattern of separate projects feeding one host.

## Implementation Notes

- **2026-06-22 (T016–T022, T008a, T019)**: Corrected the Technical Context's testing framework — the existing test project (`tests/MakeBoldSpark.Api.Tests`) uses **MSTest** (`MSTest.TestFramework`/`MSTest.TestAdapter`, `[TestClass]`/`[TestMethod]`), not xUnit as originally written above. All new backend tests follow the existing MSTest convention.
- **2026-06-22 (T005)**: `Microsoft.AspNetCore.Identity` ships as part of the ASP.NET Core 10 shared framework (no longer a separately-versioned NuGet package — `dotnet build` confirmed NU1102/NU1510 when an explicit `PackageReference` was added). `PasswordHasher<TUser>` is available via the implicit framework reference alone; T005 is satisfied without an explicit `PackageReference` in `MakeBoldSpark.Api.csproj`.
- **2026-06-22 (T008a, T019)**: Discovered that the existing `MakeBoldSparkWebApplicationFactory` (used by `AuthorizationBoundaryTests.cs` and others) unconditionally replaces JWT Bearer authentication with a test-only `TestScheme` bypass (`X-Test-Claims` header) in `ConfigureWebHost`. This means it cannot exercise the real `AuthorizationSetup.cs` signature-validation path — exactly what T008a (startup fail-fast) and T019 (signature-rejection regression) need to verify. Added a second factory, `MakeBoldSparkRealAuthWebApplicationFactory` (`tests/MakeBoldSpark.Api.Tests/Infrastructure/Auth/MakeBoldSparkRealAuthWebApplicationFactory.cs`), which keeps the real JWT Bearer pipeline active and configures a known `Jwt:SigningKey` for the test run, used only by the new Auth-related tests. The existing factory and its tests are untouched. T019's regression test now lives in a new file, `AuthorizationSignatureTests.cs`, rather than being added to the existing `AuthorizationBoundaryTests.cs` (which is permanently bound to the bypass factory via its `[ClassInitialize]`).
- **2026-06-22 (T014, T052)**: The local development database retains required legacy `Authors.DateCreated`/`DateUpdated` columns that EF no longer maps. The repeatable `bootstrap-admin` command now detects that schema shape and supplies both the legacy and current audit timestamps when creating a new author; clean/current schemas continue through the normal EF insert path. This was verified by provisioning a temporary local administrator and using it for embedded and Vite CMS authentication checks.
- **2026-06-22 (T045–T049)**: Mail-setting responses intentionally omit `userPassword`. The CMS therefore uses a masked field and requires an explicitly entered replacement password on every mail-setting save, preventing the API's full-replace PUT semantics from silently clearing the stored SMTP credential.

## Complexity Tracking

> No unjustified Constitution Check violations — the former Principle VIII deviation was resolved by the owner-ratified Constitution v1.2.0 amendment.
