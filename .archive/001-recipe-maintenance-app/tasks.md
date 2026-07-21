# Tasks: Recipe Maintenance Application

**Input**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), and [recipe-maintenance.yaml](contracts/recipe-maintenance.yaml)

**Tests**: Required by Constitution Principle II. Write each listed test before or alongside the behavior it verifies.

## Rationale Summary

### Core Problem

Provide publishers a dedicated, safe recipe and category maintenance workspace without adding a second backend or data store.

### Decision Summary

Build an embedded `MakeBoldSpark.Recipe.Client` SPA and extend the existing publisher Recipe API with domain-scoped, bounded, concurrency-safe maintenance contracts.

### Key Drivers

- Maintain one backend platform and authoritative recipe database.
- Enforce publisher authorization, active-domain isolation, safe deletes, and stale-save protection.
- Reuse CMS client and hosting conventions to minimize new complexity.

### Reviewer Guidance

Verify API contracts and tests precede implementation, every publisher route validates domain ownership and versions, and deployment keeps persistent SQLite data intact.

## Path Conventions

- API feature: `src/MakeBoldSpark.Api/Features/Recipe/`
- Recipe persistence: `src/MakeBoldSpark.Recipe/`
- Embedded client: `src/MakeBoldSpark.Recipe.Client/client/`
- API and hosting tests: `tests/MakeBoldSpark.Api.Tests/`

## Phase 1: Setup

**Purpose**: Establish the embedded client project and contract-first implementation workspace.

- [X] T001 Create `src/MakeBoldSpark.Recipe.Client/MakeBoldSpark.Recipe.Client.csproj` by mirroring the CMS embedded-SPA build and asset packaging configuration.
- [X] T002 Add the `MakeBoldSpark.Recipe.Client` project reference to `src/MakeBoldSpark.Api/MakeBoldSpark.Api.csproj` without implementing the host extension or `/recipes` mapping.
- [X] T003 [P] Create the Vite/React project metadata and test commands in `src/MakeBoldSpark.Recipe.Client/client/package.json`, `src/MakeBoldSpark.Recipe.Client/client/tsconfig.json`, and `src/MakeBoldSpark.Recipe.Client/client/vite.config.ts`.
- [X] T004 [P] Add the `MakeBoldSpark.Recipe.Client` project entry to `MakeBoldSpark.slnx` and document the new publisher contracts in `.documentation/specs/001-recipe-maintenance-app/contracts/recipe-maintenance.yaml`.

**Checkpoint**: Phase complete — 2026-06-24

## Phase 2: Foundational API and Persistence

**Purpose**: Build the shared domain, safety, and API capabilities that block every user story.

**⚠️ CRITICAL**: Complete this phase before client story work because it establishes the protected contract and persistence invariants.

- [X] T005 Add failing publisher API tests for protected configured-domain discovery, authentication, required `domainId`, page-size bounds, cross-domain hiding, approved-only anonymous details, validation errors, and response status contracts in `tests/MakeBoldSpark.Api.Tests/Features/Recipe/RecipeMaintenanceEndpointTests.cs`.
- [X] T006 Add failing persistence and endpoint tests for matching-version updates/deletes, stale-version `412`, category-in-use `409`, concurrent category assignment/delete, and oversized-payload rejection in `tests/MakeBoldSpark.Api.Tests/Features/Recipe/RecipeMaintenanceEndpointTests.cs`.
- [X] T007 Add application-managed `Version` concurrency properties and domain/query indexes to `src/MakeBoldSpark.Recipe/Data/Recipe.cs`, `src/MakeBoldSpark.Recipe/Data/RecipeCategory.cs`, and `src/MakeBoldSpark.Recipe/Data/RecipeDbContext.cs`.
- [X] T008 Generate and review the forward-only concurrency/index migration in `src/MakeBoldSpark.Recipe/Migrations/` with safe initialization for existing recipes and categories.
- [X] T009 Define bounded request/response, configured-domain, pagination, validation-problem, and conditional-request types in `src/MakeBoldSpark.Api/Features/Recipe/RecipeMaintenanceContracts.cs`.
- [X] T010 Extend `src/MakeBoldSpark.Recipe/Interfaces/IRecipeService.cs` and `src/MakeBoldSpark.Recipe/Providers/RecipeProvider.cs` with domain-scoped paged reads, same-domain category validation, version checks, and atomic delete-or-conflict category behavior.
- [X] T011 Implement approved-only anonymous detail reads, protected configured-domain/inventory/detail/mutation behavior, request-size enforcement, and HTTP status mapping in `src/MakeBoldSpark.Api/Features/Recipe/RecipeService.cs` and `src/MakeBoldSpark.Api/Features/Recipe/RecipeEndpoints.cs`.
- [X] T012 Register bounded per-publisher mutation rate limiting, publisher-operation metrics, structured redacted logs, and Recipe database readiness checks in `src/MakeBoldSpark.Api/Program.cs`, `src/MakeBoldSpark.Api/Features/Recipe/RecipeEndpoints.cs`, and `src/MakeBoldSpark.Api/Features/Health/AdminHealthEndpoints.cs`.
- [X] T013 Add runtime OpenAPI/status contract validation and run `tests/MakeBoldSpark.Api.Tests/Features/Recipe/RecipeMaintenanceEndpointTests.cs` until all contract, authorization, isolation, input-limit, concurrency, safe-delete, and public-detail regression tests pass.

**Checkpoint**: The Publisher API exposes contract-defined, bounded, domain-scoped behavior with tested authorization and persistence safeguards.

**Checkpoint**: Phase complete — 2026-06-26

## Phase 3: User Story 1 — Maintain Recipe Content (Priority: P1) 🎯 MVP

**Goal**: An authorized publisher selects a domain and safely lists, creates, edits, approves/unapproves, and deletes recipes.

**Independent Test**: Sign in, select one domain, create a recipe, update its category and approval state, confirm deletion, and verify only that domain's inventory changes.

- [X] T014 [P] [US1] Add failing client API tests for protected configured-domain discovery, authenticated recipe inventory/detail/create/update/delete, and `If-Match` headers in `src/MakeBoldSpark.Recipe.Client/client/src/api/recipeClient.test.ts`.
- [X] T015 [P] [US1] Add failing component tests for domain selection, paged recipe inventory, recipe editor validation, save confirmation, and delete confirmation in `src/MakeBoldSpark.Recipe.Client/client/src/recipes/RecipeMaintenancePage.test.tsx`.
- [X] T016 [US1] Implement same-origin authenticated fetch, domain-aware request helpers, pagination types, and conditional mutation headers in `src/MakeBoldSpark.Recipe.Client/client/src/api/recipeClient.ts`.
- [X] T017 [US1] Reuse the existing login/session behavior in `src/MakeBoldSpark.Recipe.Client/client/src/auth/AuthContext.tsx` and `src/MakeBoldSpark.Recipe.Client/client/src/auth/LoginPage.tsx` with a recipe-client-specific session key.
- [X] T018 [US1] Implement active-domain selection using the protected configured-domain contract in `src/MakeBoldSpark.Recipe.Client/client/src/recipes/DomainSelector.tsx` and `src/MakeBoldSpark.Recipe.Client/client/src/recipes/useActiveDomain.ts`.
- [X] T019 [US1] Implement the recipe list, bounded pagination controls, empty/loading/error states, and navigation in `src/MakeBoldSpark.Recipe.Client/client/src/recipes/RecipeListPage.tsx` and `src/MakeBoldSpark.Recipe.Client/client/src/recipes/RecipeMaintenancePage.tsx`.
- [X] T020 [US1] Implement recipe create/edit fields, same-domain category selection, publication control, form validation, success notification, and delete confirmation in `src/MakeBoldSpark.Recipe.Client/client/src/recipes/RecipeEditor.tsx`.
- [X] T021 [US1] Implement protected routes, sidebar/layout, `/recipes` router base path, and accessible page structure in `src/MakeBoldSpark.Recipe.Client/client/src/App.tsx` and `src/MakeBoldSpark.Recipe.Client/client/src/recipes/RecipeLayout.tsx`.
- [X] T022 [US1] Implement embedded asset serving and SPA fallback for `/recipes` in `src/MakeBoldSpark.Recipe.Client/MakeBoldSparkRecipeClientExtensions.cs` and register it in `src/MakeBoldSpark.Api/Program.cs`.
- [X] T023 [US1] Add end-to-end embedded-hosting assertions for `/recipes` HTML and generated assets in `tests/MakeBoldSpark.Api.Tests/Features/RecipeClientHostingTests.cs`.
- [X] T024 [US1] Run and repair the recipe-client lint, test, build, API test, and solution build commands recorded in `.documentation/specs/001-recipe-maintenance-app/quickstart.md`.

**Checkpoint**: A publisher can independently complete the P1 recipe-maintenance flow for one selected domain.

**Checkpoint**: Phase complete — 2026-06-26

## Phase 4: User Story 2 — Maintain Recipe Categories (Priority: P2)

**Goal**: An authorized publisher manages categories in the active domain without losing or orphaning recipes.

**Independent Test**: In a selected domain, create and edit a category, attempt to delete an in-use category and receive an in-use message, then delete an unused category after confirmation.

- [X] T025 [P] [US2] Add failing client API tests for domain-scoped category inventory and category conditional mutations in `src/MakeBoldSpark.Recipe.Client/client/src/api/recipeClient.test.ts`.
- [X] T026 [P] [US2] Add failing component tests for category create/edit/delete, in-use errors, and confirmation behavior in `src/MakeBoldSpark.Recipe.Client/client/src/recipes/CategoryMaintenancePage.test.tsx`.
- [X] T027 [US2] Extend `src/MakeBoldSpark.Recipe.Client/client/src/api/recipeClient.ts` with typed category inventory, detail, create, update, and delete methods.
- [X] T028 [US2] Implement category list, editor, validation, delete confirmation, and in-use error messaging in `src/MakeBoldSpark.Recipe.Client/client/src/recipes/CategoryMaintenancePage.tsx` and `src/MakeBoldSpark.Recipe.Client/client/src/recipes/CategoryEditor.tsx`.
- [X] T029 [US2] Add active-domain category routing and navigation in `src/MakeBoldSpark.Recipe.Client/client/src/App.tsx` and `src/MakeBoldSpark.Recipe.Client/client/src/recipes/RecipeLayout.tsx`.
- [X] T030 [US2] Run category client tests and the publisher endpoint suite in `src/MakeBoldSpark.Recipe.Client/client/src/recipes/CategoryMaintenancePage.test.tsx` and `tests/MakeBoldSpark.Api.Tests/Features/Recipe/RecipeMaintenanceEndpointTests.cs`.

**Checkpoint**: Category management is independently usable and preserves every recipe/category relationship.

**Checkpoint**: Phase complete — 2026-06-26

## Phase 5: User Story 3 — Recover from Expired or Stale Sessions (Priority: P3)

**Goal**: Publishers recover from expired authentication and stale records without losing unsaved recipe or category work.

**Independent Test**: Expire an active edit session and force a stale version in an open editor; reauthenticate/reload as prompted and verify unsaved field values remain visible.

- [X] T031 [US3] Add failing tests for 401 reauthentication prompts preserving recipe/category editor values in `src/MakeBoldSpark.Recipe.Client/client/src/recipes/RecipeMaintenancePage.test.tsx` and `src/MakeBoldSpark.Recipe.Client/client/src/recipes/CategoryMaintenancePage.test.tsx`.
- [X] T032 [US3] Add failing tests for `412` conflict handling that preserves drafts and requires an explicit reload in `src/MakeBoldSpark.Recipe.Client/client/src/recipes/RecipeMaintenancePage.test.tsx` and `src/MakeBoldSpark.Recipe.Client/client/src/recipes/CategoryMaintenancePage.test.tsx`.
- [X] T033 [US3] Add 401 event handling and in-place reauthentication prompt behavior in `src/MakeBoldSpark.Recipe.Client/client/src/api/recipeClient.ts`, `src/MakeBoldSpark.Recipe.Client/client/src/auth/AuthContext.tsx`, and `src/MakeBoldSpark.Recipe.Client/client/src/auth/ReauthPrompt.tsx`.
- [X] T034 [US3] Add stale-save conflict presentation, explicit reload action, and draft retention to `src/MakeBoldSpark.Recipe.Client/client/src/recipes/RecipeEditor.tsx` and `src/MakeBoldSpark.Recipe.Client/client/src/recipes/CategoryEditor.tsx`.
- [X] T035 [US3] Run recovery and conflict tests in `src/MakeBoldSpark.Recipe.Client/client/src/recipes/` and the stale-version endpoint tests in `tests/MakeBoldSpark.Api.Tests/Features/Recipe/RecipeMaintenanceEndpointTests.cs`.

**Checkpoint**: Expired sessions and concurrent edits fail safely without discarding publisher work.

**Checkpoint**: Phase complete — 2026-06-26

## Phase 6: Polish and Cross-Cutting Validation

**Purpose**: Validate production safety, documentation, and release readiness across all stories.

- [X] T036 [P] Verify no request logs, metrics, or client errors expose credentials, access tokens, rejected request bodies, or recipe editor payloads in `src/MakeBoldSpark.Api/Infrastructure/Observability/RequestLoggingMiddleware.cs` and `src/MakeBoldSpark.Recipe.Client/client/src/api/recipeClient.ts`.
- [X] T037 [P] Verify liveness, Recipe database readiness, publisher rate limiting, request correlation, request/error/duration metrics, and documented alert thresholds through `tests/MakeBoldSpark.Api.Tests/Infrastructure/Observability/RequestLoggingMiddlewareTests.cs`, `tests/MakeBoldSpark.Api.Tests/Features/Recipe/RecipeMaintenanceEndpointTests.cs`, and `.documentation/specs/001-recipe-maintenance-app/quickstart.md`.
- [X] T038 Create and run migration-rehearsal, backup-verification, post-migration-integrity, and forward-fix runbook checks in `scripts/powershell/verify-recipe-migration.ps1` and `.documentation/specs/001-recipe-maintenance-app/quickstart.md`.
- [X] T039 Run the complete build, timed five-second inventory/save acceptance harness, API/OpenAPI contract suite, client lint/test/build, NuGet audit, npm audit, and quickstart smoke-test sequence from `.documentation/specs/001-recipe-maintenance-app/quickstart.md`.
- [X] T040 Re-run `/devspark.analyze` and `/devspark.critic`, record any accepted gate acknowledgements in `.documentation/specs/001-recipe-maintenance-app/tasks.md`, and resolve blocking findings before implementation handoff.

**Checkpoint**: Phase complete — 2026-06-26

## Dependencies and Execution Order

- Phase 1 precedes Phase 2 because the client host and API project references must exist before integration tests can exercise `/recipes`.
- Phase 2 blocks all stories because domain isolation, validation, pagination, concurrency, safe deletion, and rate limits are shared invariants.
- US1 is the MVP and precedes US2 because category management shares the client shell and active-domain state.
- US3 depends on the session/client shell from US1 and conditional mutation behavior from Phase 2; it can then be completed alongside US2 where files do not overlap.
- Polish runs after all selected stories and before implementation handoff.

## Parallel Opportunities

- T003 and T004 can run in parallel after T001.
- T005 and T006 share an endpoint test file and run sequentially before persistence implementation.
- T014 and T015 can run in parallel; T025 and T026 can run in parallel; T031 and T032 share component test files and run sequentially.
- T036 and T037 can run in parallel after the relevant API/client behavior exists.

## Implementation Strategy

### MVP First

1. Complete Setup and Foundational API/Persistence.
2. Complete US1 through its independent test and hosting checks.
3. Demonstrate a publisher managing recipes in one active domain before starting category and recovery enhancements.

### Incremental Delivery

1. Publisher inventory + safe recipe writes.
2. Category management with in-use protection.
3. Reauthentication and stale-edit recovery.
4. Operations and release validation.

## Gate Acknowledgements

None. Required analyze and critic gates run after task generation; no unresolved gate finding has been accepted for deferral.
