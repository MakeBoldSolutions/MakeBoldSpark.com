# Implementation Plan: MakeBoldSpark Platform Foundation

**Branch**: `001-makeboldspark-foundation` | **Date**: 2026-05-07 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `.documentation/specs/001-makeboldspark-foundation/spec.md`

## Rationale Summary

### Core Problem

Personal and portfolio APIs are scattered across multiple hosting environments with Windows/IIS dependencies, inconsistent project structures, and high operational overhead. There is no unified, low-cost platform for authoring, hosting, and publishing these APIs under a single maintainable backend.

### Decision Summary

Build one modular ASP.NET Core (.NET 10 LTS) backend API as the shared foundation for all personal and portfolio APIs. The foundation establishes route group boundaries, a relational data layer (EF Core + SQLite), one public content feature, admin authorization boundaries, OpenAPI documentation, and CI/CD automation — demonstrating the architecture without overbuilding CMS, publishing, or Cosmos features prematurely.

### Key Drivers

- Replace scattered API hosting with a single, low-cost, maintainable Azure-hosted platform
- Establish a clean modular foundation that future features can extend without architectural rework
- Demonstrate practical architecture decisions at portfolio grade from day one

### Source Inputs

- MakeBoldSpark Jumpstart Guide (`.documentation/MakeBoldSpark-Jumpstart-Guide.md`) — Sections 5, 7–11, 14–16, 24
- MakeBoldSpark Constitution v1.1.0 (`.documentation/memory/constitution.md`) — Principles I–X
- Feature Spec (`.documentation/specs/001-makeboldspark-foundation/spec.md`) — User Stories 1–6, FR-001 through FR-027

### Tradeoffs Considered

- Option A: Implement all nine phases in a single spec — rejected; overbuilds before foundation is validated
- Option B: Minimal single-file API with no structure — rejected; creates technical debt before the first feature
- Option C: Multi-project layout (Api/Domain/Data/Export) from day one — deferred; warranted later but premature at foundation stage
- Selected: Simplified two-project layout (MakeBoldSpark.Api + MakeBoldSpark.Api.Tests) with feature folders — cleanest start, easy to split later per Jumpstart §5 guidance

### Architectural Impact

- Introduces the single-backend modular API pattern that all future MakeBoldSpark features must follow
- Establishes the SQLite persistence model as the authoritative data store (no Cosmos dependency)
- Sets the authorization boundary contract (public = anonymous, admin/publish/integrations = authenticated)
- GitHub Actions build/test/deploy pipeline becomes the CI/CD baseline for all future work

### Reviewer Guidance

Reviewers should verify: route group structure matches the constitution authorization model; public endpoints are anonymous and read-only; admin routes return 401/403 for unauthenticated callers; SQLite is initialized on first start; no `.db` files or secrets are committed; tests cover meaningful behavior, not just structure.

---

## Summary

Greenfield .NET 10 ASP.NET Core API project. No solution files, project files, or source code exist yet. The plan covers: solution scaffold, route group platform (Phase 0 of Jumpstart), SQLite foundation (Phase 2 of Jumpstart), public content feature (Phase 3 of Jumpstart), and CI/CD (Phase 0 of Jumpstart). Authorization policies are registered but no identity provider is wired yet — admin routes enforce the policy boundary via ASP.NET Core authorization middleware, returning `401 Unauthorized` for any unauthenticated caller.

---

## Technical Context

**Language/Version**: C# / .NET 10 LTS
**Primary Dependencies**: ASP.NET Core (Minimal APIs), EF Core 10, Microsoft.EntityFrameworkCore.Sqlite, Microsoft.AspNetCore.OpenApi / Swashbuckle.AspNetCore
**Storage**: SQLite via EF Core; local at `./data/makeboldspark.local.db`; production at `/home/data/makeboldspark.db`
**Testing**: xUnit + WebApplicationFactory (integration tests); EF Core in-memory or temp SQLite for repository tests
**Target Platform**: Azure App Service Linux B1 (production); developer workstation (local)
**Project Type**: Web API service
**Performance Goals**: Cold-start health response < 500ms (SC-001); adequate for low-volume personal/portfolio use
**Constraints**: Single App Service instance (SQLite cannot be shared across scale-out); no production `.db` in source; no secrets in source
**Scale/Scope**: Low-volume personal/portfolio; single developer team; foundation spec covers Phases 0–2 of Jumpstart Guide

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Status |
|-----------|-------|--------|
| I. API-First | OpenAPI contracts defined in `contracts/` before implementation; all endpoints documented in OpenAPI YAML before any endpoint code is written | ✅ PASS |
| II. Test-First | xUnit tests are created as a task alongside each endpoint/service/repository; no implementation task is marked complete without a paired test task | ✅ PASS |
| III. Simplicity | Two-project layout (Api + Tests); no premature abstraction; repository pattern used only where it isolates a meaningful boundary (content queries vs. direct context access) | ✅ PASS |
| IV. Security by Default | Authorization policies registered at startup; admin/publish/integrations route groups annotated with `RequireAuthorization`; CORS locked to explicit origins; no wildcard auth routes | ✅ PASS |
| V. Spec-Driven | `spec.md` exists and is complete with all required gates (`checklist`, `analyze`, `critic`) | ✅ PASS |
| VI. Ownership Boundary | Plan and artifacts are in `.documentation/specs/`; `.devspark/` is untouched by this feature work | ✅ PASS |
| VII. Single Backend Platform | One ASP.NET Core project; no microservice split; features use route groups inside the single app | ✅ PASS |
| VIII. Clear Authorization Boundaries | Route groups map exactly to the constitution table: `/api/public/*` anonymous, `/api/admin/*` AdminOnly, `/api/publish/*` Publisher, `/api/integrations/*` ServiceOrAdmin, `/api/health` anonymous | ✅ PASS |
| IX. Relational-First Data Strategy | EF Core + SQLite is the only persistence model; Cosmos DB is not referenced; production db path is `/home/data/makeboldspark.db` | ✅ PASS |
| X. Zero Secrets in Source Control | No connection strings with credentials; no API keys; SQLite path is configuration-only; deploy secrets via GitHub Actions secrets | ✅ PASS |

**Post-Phase-1 Re-check**: All principles continue to hold after design; no violations introduced by data model or contract decisions.

---

## Project Structure

### Documentation (this feature)

```text
.documentation/specs/001-makeboldspark-foundation/
├── plan.md              # This file (/devspark.plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output — OpenAPI YAML contracts
│   ├── health.yaml
│   └── public-content.yaml
├── gates/               # Persisted gate artifacts from analyze/critic/checklist
└── tasks.md             # Phase 2 output (/devspark.tasks command)
```

### Source Code (repository root)

```text
MakeBoldSpark/
  MakeBoldSpark.sln
  README.md
  .gitignore

  src/
    MakeBoldSpark.Api/
      MakeBoldSpark.Api.csproj            (.NET 10 web project)
      Program.cs
      appsettings.json
      appsettings.Development.json

      Features/
        Health/
          HealthEndpoints.cs         (GET /api/health)
        PublicContent/
          PublicContentEndpoints.cs  (GET /api/public/content/articles, /articles/{slug}, /tags)
          ContentService.cs
          ContentModels.cs

      Infrastructure/
        Auth/
          AuthorizationSetup.cs      (policy registration: AdminOnly, Publisher, ServiceOrAdmin)
        Data/
          MakeBoldSparkDbContext.cs
          DatabaseSetup.cs           (migration + seed on startup logic)
          Repositories/
            IContentRepository.cs
            ContentRepository.cs
          Seed/
            SeedData.cs
        Observability/
          RequestLoggingMiddleware.cs (structured request logging)
        Cors/
          CorsSetup.cs               (origin-locked CORS)

      Migrations/                    (EF Core generated)

  tests/
    MakeBoldSpark.Api.Tests/
      MakeBoldSpark.Api.Tests.csproj
      Features/
        Health/
          HealthEndpointTests.cs
        PublicContent/
          PublicContentEndpointTests.cs
          ContentServiceTests.cs
          ContentRepositoryTests.cs
      Infrastructure/
        Auth/
          AuthorizationBoundaryTests.cs

  .github/
    workflows/
      build-test.yml                 (PR → dotnet build + dotnet test)
      deploy.yml                     (main merge → build, test, publish, deploy to Azure)

  docs/
    decisions/
      0001-single-backend-api.md
      0002-sqlite-default.md
      0003-static-web-app-clients.md
      0004-dotnet-10-lts.md
      0005-authentication-boundaries.md

  data/
    seed/                            (JSON seed files, not DB files)
```

**Structure Decision**: Simplified two-project layout (Api + Tests) per Jumpstart §5 "Initial Simplification Option". Domain and Data are in-project folders rather than separate assemblies. Split into `MakeBoldSpark.Domain` and `MakeBoldSpark.Data` projects only when the codebase size or test isolation justifies it. Feature folders follow the constitution §Feature Structure pattern exactly.

---

## Complexity Tracking

No constitution violations requiring justification.
