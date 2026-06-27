# Implementation Plan: Recipe Maintenance Application

**Branch**: `001-recipe-maintenance-app` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification for a domain-scoped recipe publishing application.

## Rationale Summary

### Core Problem

Recipe publishers need a dedicated, safe way to maintain recipes and their categories without using the unrelated general CMS.

### Decision Summary

Create `MakeBoldSpark.Recipe.Client` as an embedded management SPA that follows the CMS application pattern. Extend the existing Recipe API with publisher-only domain discovery, domain-scoped inventory, and mutation contracts that enforce bounded input validation, optimistic concurrency, and atomic safe category deletion.

### Key Drivers

- Reuse the existing sign-in, session recovery, navigation, embedded-SPA hosting, and API-client patterns.
- Keep the current modular ASP.NET Core application and recipe database as the only backend and source of recipe data.
- Make publisher changes safe across domains and concurrent editing sessions.

### Source Inputs

- [Feature specification](spec.md)
- Existing `MakeBoldSpark.Cms` embedded SPA and API hosting pattern
- Existing `MakeBoldSpark.Api` Recipe feature and Recipe API contract
- MakeBoldSpark Constitution v1.2.0

### Tradeoffs Considered

- Use public recipe reads: rejected because public reads exclude unapproved records and do not enforce an active domain.
- Use timestamps as the concurrency token: rejected because timestamp resolution and client formatting can make conflict detection unreliable.
- Selected: publisher-only reads with a persisted integer version token and standard conditional requests.

### Architectural Impact

- Adds a client library project referenced by the existing API host; it does not add a backend service or database.
- Adds explicit publisher Recipe API contracts for inventory, detail, and domain-aware mutations.
- Adds a forward-only SQLite migration for concurrency tokens and query indexes; existing public endpoints remain compatible.

### Reviewer Guidance

Review authorization on every publisher route, domain/category ownership validation, optimistic concurrency on update and delete, 409/412 error semantics, and migration/rollback safety before accepting implementation.

## Summary

The feature adds a separate recipe-maintenance workspace at `/recipes`. A publisher selects a domain, manages every recipe in that domain (including unapproved records), and manages its categories. The client uses the existing authentication flow and is embedded and hosted by the single MakeBoldSpark API application, like the CMS at `/cms`.

The Recipe API will expose publisher-only, bounded inventory and detail reads. All recipe/category mutations will validate the selected domain, require an expected version, return a conflict instead of overwriting newer data, and refuse to delete a category that still has recipes.

## Technical Context

**Language/Version**: C# / .NET 10; TypeScript with React 19

**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core SQLite, JWT bearer authentication, React Router, Vite, Vitest, Testing Library; no new runtime packages

**Storage**: Existing EF Core SQLite Recipe database under the production `/home/data/` persistence path; a migration adds application-managed integer concurrency tokens and indexes for domain-scoped inventories

**Testing**: MSTest integration/endpoint tests for API contracts and embedded hosting; Vitest + Testing Library tests for client behavior; `dotnet test`, client `npm run lint`, `npm test`, and `npm run build`

**Target Platform**: Azure App Service Linux B1 hosting the consolidated ASP.NET Core API; browser-based internal publisher workspace served from the same origin

**Project Type**: Modular web service with two embedded React management SPAs

**Performance Goals**: A publisher receives a page of at most 50 recipes within 5 seconds under the acceptance-test data set; successful valid saves are visible in the returned representation within 5 seconds

**Constraints**: JWT publisher policy; same-origin embedded SPA; no secrets in source; API-first OpenAPI contract; no unbounded recipe lists; 256 KiB maximum write body; 64 KiB maximum ingredients/instructions fields; production database files are not deployed with the application

**Scale/Scope**: One new management SPA; all configured domains selectable by authorized publishers; default page size 50 and maximum 100 records per inventory request

## Constitution Check

| Principle | Plan response | Status |
| --- | --- | --- |
| I. API-First | Define and validate the publisher Recipe API OpenAPI contract against runtime operations and every documented status response before client implementation. | Pass |
| II. Test-First | Add endpoint/client tests before or alongside every behavior task. | Pass |
| III. Simplicity | Reuse CMS hosting/auth/client patterns and add no runtime dependencies or service. | Pass |
| IV. Security by Default | Enforce Publisher policy, approved-only anonymous detail reads, same-origin requests, domain ownership checks, body/field validation limits, and non-secret configuration. | Pass |
| V. Spec-Driven Development | This plan, contract, model, quickstart, tasks, and gates precede implementation. | Pass |
| VI. Ownership Boundary | All generated work is feature documentation; framework payload is not changed. | Pass |
| VII. Single Backend Platform | Client library is hosted by the existing API application; no microservice is introduced. | Pass |
| VIII. Clear Authorization Boundaries | New reads/writes and configured-domain discovery use `/api/publish/recipes/*` and the `Publisher` policy; public endpoints remain read-only and approved-only. | Pass |
| IX. Relational-First Data Strategy | Keep EF Core + SQLite, run forward migration, and preserve `/home/data/` deployment handling. | Pass |
| X. Zero Secrets in Source Control | Reuse JWT settings via environment/user secrets; do not add credentials or connection strings to the client. | Pass |

**Post-design result**: Pass. No constitution waivers are required.

## Project Structure

### Documentation (this feature)

```text
.documentation/specs/001-recipe-maintenance-app/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── recipe-maintenance.yaml
├── checklists/
│   └── requirements.md
├── gates/
│   ├── analyze.md
│   └── critic.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── MakeBoldSpark.Api/
│   ├── Features/Recipe/
│   │   ├── RecipeEndpoints.cs
│   │   ├── RecipeService.cs
│   │   └── RecipeMaintenanceContracts.cs
│   └── Program.cs
├── MakeBoldSpark.Recipe/
│   ├── Data/
│   ├── Migrations/
│   └── Providers/
└── MakeBoldSpark.Recipe.Client/
    ├── MakeBoldSpark.Recipe.Client.csproj
    ├── MakeBoldSparkRecipeClientExtensions.cs
    └── client/
        ├── src/
        │   ├── api/
        │   ├── auth/
        │   ├── recipes/
        │   └── App.tsx
        └── package.json

tests/
└── MakeBoldSpark.Api.Tests/
    ├── Features/Recipe/
    └── Features/RecipeClientHostingTests.cs
```

**Structure Decision**: Use a new embedded client project that mirrors `MakeBoldSpark.Cms`, with one shared API host and no standalone backend. Recipe domain persistence stays in `MakeBoldSpark.Recipe`; HTTP boundary behavior stays in the API feature folder.

## Implementation Phases

1. Contract and persistence foundation: publish and validate the OpenAPI contract, repair anonymous approved-only detail reads, add concurrency/index migration, and implement publisher domain discovery/read/query contracts with tests.
2. Recipe maintenance MVP: add the embedded client, authentication/session handling, domain selection, paged recipe list, recipe editor, and safe recipe mutations.
3. Category maintenance: add domain-scoped category management and in-use delete protection.
4. Recovery and operations: stale-save UI recovery, rate limits, readiness and endpoint telemetry, migration rehearsal/backup validation, dependency audits, hosting tests, documentation, and release validation.

## Operational and Release Controls

- **Readiness**: retain `/api/health` as shallow anonymous liveness; extend the existing admin deep-health route to verify the Recipe database and migration readiness without returning connection details.
- **Telemetry**: emit per-operation request count, error count, and duration signals for publisher Recipe operations using built-in runtime metrics/structured logs with correlation IDs; document alert thresholds for elevated 4xx/5xx rates and p95 latency approaching five seconds.
- **Migration continuity**: require a backup verification, rehearsal on a database copy, post-migration integrity check, and forward-fix runbook before production deployment. A deployment does not proceed when any prerequisite fails.
- **Supply chain**: use lockfile-based restores and include NuGet and npm vulnerability audits in final validation and CI.
- **Contract compatibility**: compare the documented Recipe Maintenance OpenAPI contract with the runtime OpenAPI document and exercise every documented success/error response.

## Complexity Tracking

No constitution violations or waivers. The extra client library project is justified because it mirrors the existing embedded CMS packaging pattern while retaining one backend platform.

## Implementation Notes

- 2026-06-26 (T010-T011): Domain-scoped publisher maintenance operations were kept in `src/MakeBoldSpark.Api/Features/Recipe/RecipeService.cs` against `RecipeDbContext` instead of expanding the legacy `IRecipeService`/`RecipeProvider` surface. This keeps the public legacy provider boundary stable while enforcing pagination, same-domain category validation, optimistic version checks, and safe category-delete behavior at the protected API boundary with `RecipeMaintenanceEndpointTests` coverage.
