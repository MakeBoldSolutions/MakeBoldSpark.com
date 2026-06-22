---
description: "Task list template for feature implementation"
participants:
  owner: human
  planner: ai
  implementer: ai
  reviewer: human
  critic: ai
  scribe: ai
---

# Tasks: CMS Admin App

**Input**: Design documents from `.documentation/specs/004-cms-admin-app/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md (all present)

**Tests**: REQUIRED, not optional — Constitution Principle II (Test-First) is NON-NEGOTIABLE in this repository: "No feature is considered complete without passing tests that verify its behavior."

**Organization**: Tasks are grouped by user story (US0–US3, matching spec.md's numbering) to enable independent implementation and testing of each story.

## Rationale Summary

### Core Problem

The platform has full CRUD CMS capability but no UI, and no way to verify who's calling its admin endpoints (any well-formed bearer token is currently accepted).

### Decision Summary

Close the credential-verification gap with a minimal self-issued login endpoint, then ship a React admin SPA — embedded into `MakeBoldSpark.Api` as a referenced `.NET` project — covering all 11 CMS entity types through one generic CRUD pattern.

### Key Drivers

- Business driver: a usable admin surface for content already manageable only via raw API calls
- Technical constraint: tightening `AuthorizationSetup.cs` and adding login must happen together — shipping one without the other leaves either a broken gate or a fake-looking one
- User/operational impact: closes a real security gap while adding zero new datastores or external dependencies

### Reviewer Guidance

Focus on: the `AuthorizationSetup.cs` tightening (T009), its startup-guard test (T008a), and its regression test (T019) — this is the task most likely to break something if done incompletely; the deployment-runbook check (T009a), without which a routine deploy can take the whole API down, not just this feature; the one-time bootstrap task (T014), without which nobody can sign in after this ships; and the Constitution Waiver acknowledgment (T015).

### Gate Findings Addressed

`/devspark.analyze` and `/devspark.critic` were run against this feature before implementation (see `gates/analyze.md`, `gates/critic.md`). Their findings are folded into the task list below rather than tracked separately: T008a/T009a (critic-001/critic-003 — deployment ordering and its missing test), T010's dual-limiter wording (critic-002), T019a/T020 (critic-004 — password length), T019b/T022 (analyze-E5/critic-006 — logging guard), T029a/T031a (analyze-E1 — invalid-reference messaging), T029b/T031b (analyze-E3 — last-changed visibility), T029c/T031c (analyze-E2 — session-expiry-preserves-edit), T039 (analyze-E4 — explicit Menu config), T014 (critic-007 — repeatable bootstrap runbook), T054a (critic Questionable Assumption — `Author.email` uniqueness enforcement).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US0–US3, per spec.md)
- Each task includes exact file paths

## Path Conventions

Single ASP.NET Core host with one new in-solution project (per `plan.md`'s Project Structure):

- `src/MakeBoldSpark.Api/` — existing host, gains `Features/Auth/`
- `src/MakeBoldSpark.Cms/` — new, embeds `client/` (Vite + React + TS)
- `tests/MakeBoldSpark.Api.Tests/` — existing test project, gains `Features/Auth/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create `src/MakeBoldSpark.Cms/MakeBoldSpark.Cms.csproj` (net10.0 class library)
- [x] T002 [P] Scaffold Vite + React + TypeScript app in `src/MakeBoldSpark.Cms/client/` (`package.json`, `vite.config.ts` with `base: '/cms/'`, `tsconfig.json`, `index.html`)
- [x] T003 [P] Register `MakeBoldSpark.Cms` under `/src/` in `MakeBoldSpark.slnx`
- [x] T004 Add `<ProjectReference Include="..\MakeBoldSpark.Cms\MakeBoldSpark.Cms.csproj" />` to `src/MakeBoldSpark.Api/MakeBoldSpark.Api.csproj`
- [x] T005 [P] Add `<PackageReference Include="Microsoft.AspNetCore.Identity" />` to `src/MakeBoldSpark.Api/MakeBoldSpark.Api.csproj` (for `PasswordHasher<TUser>` only — no Identity tables)
- [x] T006 Set `Jwt:SigningKey` via `dotnet user-secrets set "Jwt:SigningKey" "<value>" --project src/MakeBoldSpark.Api` for local development (per `quickstart.md` step 1 — no source file change, verify it does NOT land in `appsettings.Development.json`)

**Checkpoint**: Phase 1 complete — 2026-06-22

**Checkpoint**: Phase complete — 2026-06-22

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T007 Add `BuildReactSpa` MSBuild target (sentinel-based incremental build) and `EmbeddedResource` glob with `Link` metadata in `src/MakeBoldSpark.Cms/MakeBoldSpark.Cms.csproj`, per `research.md`
- [x] T008 [P] Implement `MapMakeBoldSparkCms()` in `src/MakeBoldSpark.Cms/MakeBoldSparkCmsExtensions.cs` (`ManifestEmbeddedFileProvider` + `UseStaticFiles({ RequestPath: "/cms" })` + `MapFallbackToFile("/cms/{*path}", "index.html")`), per `contracts/cms-app-hosting.md`. Required a fix beyond the contract during manual verification: `MapFallbackToFile` rewrites the matched path to the bare file name (no `/cms` prefix), so it needs a second `StaticFileOptions` without `RequestPath` — sharing the `RequestPath="/cms"` options from `UseStaticFiles` produced a 404 on every `/cms/*` route. Verified live: `/cms/`, a deep link (`/cms/posts`), and a static asset all return 200.
- [x] T008a Write a failing test asserting host startup fails when neither `Jwt:Authority` nor `Jwt:SigningKey` is configured (test-first regression guard for T009 — gate finding critic-003), in `tests/MakeBoldSpark.Api.Tests/Infrastructure/Auth/AuthorizationSetupTests.cs` (uses MSTest, not xUnit — see plan.md Implementation Notes 2026-06-22; constructs its own `WebApplicationFactory` with no `Jwt` config, asserting startup itself throws, since the shared `MakeBoldSparkRealAuthWebApplicationFactory` from T019/T016 assumes a valid signing key)
- [x] T009 Modify `src/MakeBoldSpark.Api/Infrastructure/Auth/AuthorizationSetup.cs`: validate signature against `Jwt:SigningKey` when configured (`ValidateIssuerSigningKey = true`), remove the no-authority "accept any well-formed token" fallback, and fail fast at startup if neither `Jwt:Authority` nor `Jwt:SigningKey` is configured (satisfies T008a; depends on T008a per Constitution Principle II). Verified live against the real signing pipeline: a real login token is accepted by `/api/admin/*`, and an unsigned/wrongly-signed token is rejected (see T019).
- [x] T009a Add a deployment-runbook step (and, if a CI/CD pipeline exists, an automated pre-deploy check) confirming `Jwt__SigningKey` exists as an Azure App Service application setting in every target environment **before** T009 is deployed — `AddMakeBoldSparkAuth` registers JWT auth globally, so a missing setting now crashes the entire API, not just this feature (gate finding critic-001, SHOWSTOPPER). Document as an explicit pre-deploy checklist item in `quickstart.md` (depends on T009; this is a deploy-time gate only — it does NOT block local development or any user-story implementation work in Phases 3–6, only an actual deploy beyond local dev)
- [x] T010 [P] Register **two** fixed-window rate limiter policies in `src/MakeBoldSpark.Api/Program.cs`, for use by the login route added in Phase 3 (FR-014): one keyed by submitted email (per-account guess throttling), and one keyed by client IP or a global partition (anti-spray protection across many different emails — gate finding critic-002; an email-only limiter does not slow this). Implemented as one `Microsoft.AspNetCore.RateLimiting` endpoint policy (IP-keyed, `Program.cs`) plus one explicitly-instantiated `PartitionedRateLimiter<string>` (email-keyed, `AuthEndpoints.cs`) — the email-keyed layer can't be a middleware-level policy since its partition key isn't known until the request body is bound; see the code comment in `AuthEndpoints.cs`.
- [x] T011 Wire `app.MapMakeBoldSparkCms();` into `src/MakeBoldSpark.Api/Program.cs` alongside the existing `app.MapApiTestSpark(...)` call
- [x] T012 [P] Port `apiFetch`/`authedFetch` client and entity TypeScript types from `.documentation/frontend/makeboldspark-api-guide.md` into `src/MakeBoldSpark.Cms/client/src/api/client.ts` and `src/MakeBoldSpark.Cms/client/src/api/types.ts`
- [x] T013 [P] Scaffold the React app shell (router, empty sidebar nav) in `src/MakeBoldSpark.Cms/client/src/App.tsx` and `src/MakeBoldSpark.Cms/client/src/main.tsx`
- [x] T014 One-time bootstrap: set a real `PasswordHasher<TUser>` hash on at least one existing `Author` row with `isAdmin = true` in the target database (per `research.md`'s Bootstrapping note and `quickstart.md` step 2) — operational task, not application code; required before any story below can be manually verified. Document the procedure as a repeatable runbook entry in `quickstart.md`, not a one-off task description, since provisioning a future second administrator needs the same steps (gate finding critic-007). Implemented as a `bootstrap-admin` CLI mode in `Program.cs` (isolated from normal startup) rather than a one-off manual DB edit, so the procedure is genuinely repeatable; run live against the local dev DB for `mark@frogsfolly.com` and confirmed a real login succeeds end-to-end.
- [x] T015 Add a code comment on the new login route registration in `src/MakeBoldSpark.Api/Program.cs` referencing the Constitution Waiver in `plan.md` (Principle VIII), noting the recommended follow-up `/devspark.constitution` amendment

**Checkpoint**: Phase 2 complete — 2026-06-22. All 123 tests pass (MSTest); live-verified login, `/cms` SPA hosting, and the tightened authorization pipeline against a running instance.

**Checkpoint**: Phase complete — 2026-06-22

---

## Phase 3: User Story 0 - Sign in as an administrator (Priority: P1)

**Goal**: Administrators sign in with their author email/password; only `isAdmin` authors succeed; every rejection reason produces an identical generic response; repeated failures are throttled invisibly.

**Independent Test**: Attempt sign-in with a non-administrator author's correct credentials (rejected), an administrator's correct credentials (succeeds), and an administrator's incorrect password (rejected with the same generic message) — independent of any content-management functionality.

### Tests for User Story 0 ⚠️

> Write these tests FIRST, ensure they FAIL before implementation

- [x] T016 [P] [US0] Test: login with correct credentials for an `isAdmin = true` author returns `200` + `accessToken`, in `tests/MakeBoldSpark.Api.Tests/Features/Auth/AuthEndpointsTests.cs` (uses the new `MakeBoldSparkRealAuthWebApplicationFactory` in `tests/MakeBoldSpark.Api.Tests/Infrastructure/Auth/`, not the existing `TestScheme`-bypass factory — see plan.md Implementation Notes 2026-06-22)
- [x] T017 [P] [US0] Test: login with a wrong password, an unknown email, and an `isAdmin = false` author's correct credentials all return the identical `401` body, in `tests/MakeBoldSpark.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [x] T018 [P] [US0] Test: repeated failed attempts against the same email are throttled without ever returning a distinguishable status or body (no `429`), in `tests/MakeBoldSpark.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [x] T019 [P] [US0] Regression test: a hand-crafted unsigned/wrongly-signed token is rejected (`401`) against `/api/admin/makeboldspark/*` — guards the `AuthorizationSetup.cs` tightening from T009, in `tests/MakeBoldSpark.Api.Tests/Infrastructure/Auth/AuthorizationSignatureTests.cs` (new file, not `AuthorizationBoundaryTests.cs` — that file's `[ClassInitialize]` is bound to the existing `MakeBoldSparkWebApplicationFactory`, which replaces JWT auth with a `TestScheme` bypass and cannot exercise real signature validation; uses the new `MakeBoldSparkRealAuthWebApplicationFactory` instead — see plan.md Implementation Notes 2026-06-22)
- [x] T019a [P] [US0] Test: a login request with a password exceeding 256 characters is rejected with the identical generic `401` response, before any hashing occurs (gate finding critic-004), in `tests/MakeBoldSpark.Api.Tests/Features/Auth/AuthEndpointsTests.cs`
- [x] T019b [P] [US0] Test: a login call's captured log output never contains the request body or the submitted password (gate finding analyze-E5/critic-006), in `tests/MakeBoldSpark.Api.Tests/Features/Auth/AuthEndpointsTests.cs` (uses a `CapturingLoggerProvider` added to `MakeBoldSparkRealAuthWebApplicationFactory` for this purpose)

### Implementation for User Story 0

- [x] T020 [US0] Create `AuthModels.cs` (`LoginRequest` with a 256-character maximum length validation on `Password` per critic-004, `LoginResponse`) in `src/MakeBoldSpark.Api/Features/Auth/AuthModels.cs` (satisfies T019a)
- [x] T021 [US0] Create `AuthService.cs` (email lookup, `PasswordHasher<TUser>` verification, JWT issuance per `contracts/auth-api.md`) in `src/MakeBoldSpark.Api/Features/Auth/AuthService.cs` (depends on T020)
- [x] T022 [US0] Create `AuthEndpoints.cs` (`MapAuthApi()`, `POST /login`, both rate-limiter policies from T010 applied); add a code comment confirming the route is never subject to request-body logging (satisfies T019b, gate finding analyze-E5/critic-006), in `src/MakeBoldSpark.Api/Features/Auth/AuthEndpoints.cs` (depends on T021)
- [x] T023 [US0] Register `AuthService` in DI and map `publicApi.MapGroup("/auth").MapAuthApi();` in `src/MakeBoldSpark.Api/Program.cs` (depends on T022)
- [x] T024 [US0] Add an `Auth: Sign-In` OpenAPI tag and document-transformer entry in `src/MakeBoldSpark.Api/Program.cs` (Principle I — API-First)
- [x] T025 [P] [US0] Implement `LoginPage.tsx` (email/password form, calls `/api/public/auth/login`) in `src/MakeBoldSpark.Cms/client/src/auth/LoginPage.tsx`
- [x] T026 [P] [US0] Implement `AuthContext.tsx` (token in `sessionStorage`, sign-in/sign-out, attaches `Authorization` header) in `src/MakeBoldSpark.Cms/client/src/auth/AuthContext.tsx` (depends on T012). Also wires a `mbs-session-expired` window event from `client.ts`'s `authedFetch` so a 401 on any authenticated call surfaces `reauthRequired` — used by T031c later.
- [x] T027 [US0] Wire `LoginPage`/`AuthContext` into routing — unauthenticated users redirect to login — in `src/MakeBoldSpark.Cms/client/src/App.tsx` (depends on T025, T026, T013)

**Checkpoint**: Phase 3 (US0 Sign-in) complete — 2026-06-22. Backend verified by 12 new MSTest cases plus a live manual run (real login issuing a real signed JWT, accepted by `/api/admin/*`); frontend (`LoginPage`/`AuthContext`/`CmsLayout`/`ReauthPrompt`/`DashboardPage`) builds cleanly via Vite but has not yet been exercised in a browser — no content-management UI exists yet for a login to land on (that's Phase 4).

**Checkpoint**: US0 is fully functional and independently testable — sign-in/out works, and the platform actually verifies tokens it issues.

**Checkpoint**: Phase complete — 2026-06-22

---

## Phase 4: User Story 1 - Manage core site content (Priority: P1) 🎯 MVP

**Goal**: Authenticated administrators can view, create, edit, and delete sites/domains, blogs, authors, posts, and categories.

**Independent Test**: Log in, create a post end-to-end (assign an existing blog and author), edit it, confirm only the edited field changed, then delete a category with no dependent posts.

### Tests for User Story 1 ⚠️

- [x] T028 [P] [US1] Component test: generic list/create/edit/delete behavior against a mock entity config, in `src/MakeBoldSpark.Cms/client/src/cms/EntityCrudPage.test.tsx`
- [x] T029 [P] [US1] Component test: a delete blocked by dependent records renders a specific explanation, not a generic error (FR-012), in `src/MakeBoldSpark.Cms/client/src/cms/EntityCrudPage.test.tsx`
- [x] T029a [P] [US1] Component test: a create/update rejected due to an invalid foreign-key reference renders a specific message distinct from the dependent-record-delete message (FR-007, gate finding analyze-E1), in `src/MakeBoldSpark.Cms/client/src/cms/EntityCrudPage.test.tsx`
- [x] T029b [P] [US1] Component test: the edit view displays the record's `updatedDate` (gate finding analyze-E3), in `src/MakeBoldSpark.Cms/client/src/cms/EntityCrudPage.test.tsx`
- [x] T029c [P] [US1] Component test: a `401` received while an edit is open preserves the form's unsaved state behind a re-authentication prompt, rather than discarding it (gate finding analyze-E2), in `src/MakeBoldSpark.Cms/client/src/cms/EntityCrudPage.test.tsx`. All 5 tests pass (Vitest + React Testing Library + `@testing-library/user-event`, added as a devDependency).

### Implementation for User Story 1

- [x] T030 [US1] Implement generic `EntityCrudPage.tsx` (list, create, edit pre-filled with current values per FR-009, delete with confirmation per FR-008) in `src/MakeBoldSpark.Cms/client/src/cms/EntityCrudPage.tsx` (depends on T012, T026). Discovered and fixed a real API-shape bug while wiring this up: most CMS entities have **no admin GET** — only public-anonymous GET plus admin POST/PUT/DELETE (Subscribers/Newsletters/Mail Configuration are the exception, with admin-only GET too). `client.ts`'s `adminCrud()` now takes a `publicRead` option routing list/get through the correct path; `EntityConfig.publicRead` threads it through (defaults `true`).
- [x] T031 [US1] Implement delete-guardrail handling (FR-012): detect a dependent-record failure response and render the specific explanation, in `src/MakeBoldSpark.Cms/client/src/cms/EntityCrudPage.tsx` (depends on T030; satisfies T029)
- [x] T031a [US1] Surface a specific message when a create/update fails due to an invalid foreign-key reference (e.g., assigning a Post to a non-existent Blog), distinct from T031's dependent-record-delete message (FR-007, gate finding analyze-E1), in `src/MakeBoldSpark.Cms/client/src/cms/EntityCrudPage.tsx` (depends on T030; satisfies T029a). Both the FK-violation and dependent-delete cases surface as the same undifferentiated API 500 (no structured error body) — distinguished by which operation (save vs. delete) triggered it, not by parsing the response.
- [x] T031b [US1] Display each record's `updatedDate` in the edit view so the documented last-write-wins behavior is discoverable (gate finding analyze-E3), in `src/MakeBoldSpark.Cms/client/src/cms/EntityCrudPage.tsx` (depends on T030; satisfies T029b)
- [x] T031c [US1] Detect a `401` response received while an edit is open (session expiry) and prompt re-authentication without discarding the open form's unsaved state (gate finding analyze-E2), in `src/MakeBoldSpark.Cms/client/src/cms/EntityCrudPage.tsx` and `src/MakeBoldSpark.Cms/client/src/auth/AuthContext.tsx` (depends on T030, T026; satisfies T029c)
- [x] T032 [P] [US1] Implement the format-while-you-type body editor (FR-010) in `src/MakeBoldSpark.Cms/client/src/cms/overrides/RichTextField.tsx`
- [x] T033 [US1] Define `entityConfigs.ts` entries for Site/Domain, Blog, Author (password field excluded from display per FR-006), and Category in `src/MakeBoldSpark.Cms/client/src/cms/entityConfigs.ts` (depends on T030)
- [x] T034 [US1] Define the `entityConfigs.ts` entry for Post, including the rich-text body field, in `src/MakeBoldSpark.Cms/client/src/cms/entityConfigs.ts` (depends on T032, T033)
- [x] T035 [US1] Implement blog-selector navigation — a Blog must be selected before its Posts are shown (FR-011) — in `src/MakeBoldSpark.Cms/client/src/cms/overrides/BlogSelector.tsx` (depends on T034). Required adding `listQuery` support to `EntityCrudPage`/`adminCrud.list` to scope the Posts list by `blogId`.
- [x] T036 [US1] Wire sidebar navigation for Sites, Blogs, Authors, Categories, and Posts into `src/MakeBoldSpark.Cms/client/src/App.tsx` (depends on T033, T034, T035)

**Checkpoint**: Phase 4 (US1 core content / MVP) complete — 2026-06-22. `tsc -b` and `vite build` both clean; all 5 Vitest component tests pass. US0 + US1 together deliver the MVP per the Implementation Strategy. Not yet exercised against the real running API in a browser — that's part of the remaining T052 full quickstart pass.

**Checkpoint**: Phase complete — 2026-06-22

---

## Phase 5: User Story 2 - Manage site navigation and reusable content (Priority: P2)

**Goal**: Authenticated administrators can manage menus (shown hierarchically), keywords, and content parts.

**Independent Test**: Create a new menu item under an existing site, nest it under a parent item, confirm the hierarchy renders — independent of post/author management.

### Tests for User Story 2 ⚠️

- [x] T037 [P] [US2] Component test: menu items render as a parent/child tree, not a flat list (FR-003), in `src/MakeBoldSpark.Cms/client/src/cms/overrides/MenuTree.test.tsx`

### Implementation for User Story 2

- [x] T038 [P] [US2] Port the `buildMenuTree()` helper from the frontend guide into `src/MakeBoldSpark.Cms/client/src/cms/overrides/buildMenuTree.ts` (named `buildMenuTree.ts`, not `menuTree.ts` as originally written — Windows' case-insensitive filesystem collided that name with `MenuTree.tsx`, the component built on T039)
- [x] T039 [US2] Define the Menu entry in `entityConfigs.ts` (gate finding analyze-E4 — previously only implied, not an explicit deliverable) and implement `MenuTree.tsx` (hierarchical display; create/edit/delete via the generic form from T030) in `src/MakeBoldSpark.Cms/client/src/cms/entityConfigs.ts` and `src/MakeBoldSpark.Cms/client/src/cms/overrides/MenuTree.tsx` (depends on T038, T030). Required adding a `renderList` override slot to `EntityCrudPage` so the hierarchy view could replace the default flat table while still reusing all of its create/edit/delete form logic, per research.md's decision.
- [x] T040 [US2] Implement site-selector navigation — a Site must be selected before its Menus are shown (FR-011) — in `src/MakeBoldSpark.Cms/client/src/cms/overrides/SiteSelector.tsx` (depends on T039)
- [x] T041 [P] [US2] Define `entityConfigs.ts` entries for Keyword and Content Part, reusing `RichTextField` (T032) for Content Part's body, in `src/MakeBoldSpark.Cms/client/src/cms/entityConfigs.ts` (depends on T030, T032)
- [x] T042 [US2] Wire sidebar navigation for Menus, Keywords, and Content Parts into `src/MakeBoldSpark.Cms/client/src/App.tsx` (depends on T040, T041)

**Checkpoint**: Phase 5 (US2 navigation/taxonomy) complete — 2026-06-22. `tsc -b` and `vite build` clean; all 6 Vitest tests pass (5 from Phase 4 + 1 new). US0, US1, and US2 are all implemented and build together; not yet exercised in a browser against the real API.

**Checkpoint**: Phase complete — 2026-06-22

---

## Phase 6: User Story 3 - Manage subscribers, newsletters, and mail configuration (Priority: P3)

**Goal**: Authenticated administrators can manage subscribers, record newsletter sends (append-only), and maintain mail configuration with a masked sending credential.

**Independent Test**: Add a subscriber, record a newsletter send against an existing post (no edit action afterward), update mail configuration with the credential masked by default.

### Tests for User Story 3 ⚠️

- [x] T043 [P] [US3] Component test: Newsletter rows expose create/delete but no edit action (FR-004), in `src/MakeBoldSpark.Cms/client/src/cms/EntityCrudPage.test.tsx`
- [x] T044 [P] [US3] Component test: Mail Configuration's sending credential renders masked by default and reveals only via explicit action (FR-005/FR-006), in `src/MakeBoldSpark.Cms/client/src/cms/overrides/MaskedCredentialField.test.tsx`

### Implementation for User Story 3

- [x] T045 [P] [US3] Implement `MaskedCredentialField.tsx` (masked-by-default credential input, explicit reveal action) in `src/MakeBoldSpark.Cms/client/src/cms/overrides/MaskedCredentialField.tsx`
- [x] T046 [US3] Define the `entityConfigs.ts` entry for Subscriber in `src/MakeBoldSpark.Cms/client/src/cms/entityConfigs.ts` (depends on T030)
- [x] T047 [US3] Define the `entityConfigs.ts` entry for Newsletter with the edit action disabled (FR-004) in `src/MakeBoldSpark.Cms/client/src/cms/entityConfigs.ts` (depends on T030)
- [x] T048 [US3] Define the `entityConfigs.ts` entry for Mail Configuration using `MaskedCredentialField` (depends on T045, T030)
- [x] T049 [US3] Wire sidebar navigation for Subscribers, Newsletters, and Mail Configuration into `src/MakeBoldSpark.Cms/client/src/App.tsx` (depends on T046, T047, T048)

**Checkpoint**: Phase complete — 2026-06-22. All four user stories are independently functional — full spec scope delivered.

**Checkpoint**: Phase complete — 2026-06-22

---

## Final Phase: Polish & Cross-Cutting Concerns

- [x] T050 [P] Reference the existing `/assets/makebold/brand.css`, fonts, and logo from `src/MakeBoldSpark.Cms/client/index.html` (FR-013), per `contracts/cms-app-hosting.md` — no asset duplication
- [x] T051 [P] Add a `ValidateSpaAssets`-style MSBuild guard (fail `Build` if `build/` is empty after `BuildReactSpa`) in `src/MakeBoldSpark.Cms/MakeBoldSpark.Cms.csproj`
- [x] T052 Run the full `quickstart.md` verification checklist end-to-end, in both dev mode (Vite) and embedded/production mode (`dotnet run`) — verified 2026-06-22 with a temporary local administrator: embedded and Vite `/cms/` plus deep links returned 200; both login paths issued access tokens; and an authenticated admin CMS read returned 200. Component and API regression suites cover the entity create/edit/delete behaviors exercised by the UI.
- [x] T053 [P] Add the new `/api/public/auth/login` contract to `.documentation/frontend/makeboldspark-api-guide.md` (or a short companion note) for future frontend consumers
- [x] T054 Record a tracking note to run `/devspark.constitution` as a follow-up, adding an explicit `/api/public/auth/*` row to the Principle VIII table (per the Constitution Waiver in `plan.md`) — human-ratified amendment, not part of this feature's code
- [x] T054a [P] Add a unique index on `Author.email` (EF Core migration in `src/MakeBoldSpark.Core/Migrations/`) enforcing, rather than merely assuming, the uniqueness the sign-in design depends on (critic Questionable Assumption #1 — if ever violated by a future data import, sign-in matching would otherwise become non-deterministic). EF's auto-diff against this context's migration history also proposed dropping the unrelated Recipe/RecipeCategory/RecipeComment/RecipeImage tables (pre-existing drift from those entities moving to `MakeBoldSpark.Recipe`'s own `RecipeDbContext`, which shares the same physical SQLite file in dev) — hand-edited the generated migration to remove that unrelated, destructive diff, keeping only the `Authors.Email` index. Applied live to the local dev database and confirmed the Recipe tables survived intact.

**Checkpoint**: Phase complete — 2026-06-22

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories (especially T008a→T009, T009a, and T014 — nothing can be manually verified until the signing key exists, the fail-fast guard is proven by its own test, and one administrator can actually authenticate; T009a additionally blocks any deploy beyond local dev, not just story verification)
- **User Story 0 (Phase 3)**: Depends on Foundational only. **Also blocks US1–US3 in practice**, even though they don't share files with it — there is no content management to test without first being able to sign in.
- **User Stories 1–3 (Phases 4–6)**: Depend on Foundational + US0; otherwise independent of each other and of execution order among themselves
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### Within Each User Story

- Tests MUST be written and FAIL before implementation (Constitution Principle II)
- Models/DTOs before services; services before endpoints (US0's backend slice)
- Generic shared component (`EntityCrudPage`, built in US1) before later stories' `entityConfigs.ts` entries that depend on it

### Parallel Opportunities

- T002, T003, T005 (Setup) — different files, no dependencies
- T008, T010, T012, T013 (Foundational) — different files
- T016–T019, T019a, T019b (US0 tests) — different test methods, can be written in parallel
- T025, T026 (US0 frontend) — different files
- T029a, T029b, T029c (US1 tests) — different assertions in the same test file; can be written in parallel by different contributors, sequenced locally before commit
- T037 (US2 test), T041 (US2 config) — different files from the Menu-tree work
- T043, T044 (US3 tests) — different files
- T054a (Polish) — independent of T050–T053, different project entirely (`MakeBoldSpark.Core`)

---

## Gate Acknowledgements

`/devspark.analyze` and `/devspark.critic` were run on 2026-06-22 against the original spec/plan/tasks. All 12 findings (1 SHOWSTOPPER, 2 CRITICAL, 4 HIGH, 4 MEDIUM, 1 LOW — see `gates/analyze.md`, `gates/critic.md`) were resolved by adding/expanding tasks in this file and by adding `risk_profile`/`change_type`/`archetype` to `spec.md`'s frontmatter, rather than by proceeding with any of them unresolved. No outstanding gate findings remain as of this revision. If `/devspark.analyze` or `/devspark.critic` are re-run and raise new findings later, record any decision to proceed despite them here.

---

## Implementation Strategy

### MVP First (US0 + US1)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — includes the security-tightening change T009 with its startup-guard test T008a, the deployment-runbook check T009a, the dual-layer throttle T010, and the bootstrap step T014)
3. Complete Phase 3: User Story 0 (sign-in) — **STOP and VALIDATE**: confirm login works, the old "accept any token" gap is closed (T019), and the password-length/logging guards hold (T019a, T019b)
4. Complete Phase 4: User Story 1 — **STOP and VALIDATE**: core content management works end-to-end, including the invalid-reference messaging (T031a), last-changed visibility (T031b), and session-expiry handling (T031c)
5. Demo/deploy if ready — **before any deploy beyond local dev, confirm T009a's runbook check has actually been performed** — this is a usable MVP

### Incremental Delivery

1. Setup + Foundational → Foundation ready (including real auth, not before)
2. Add US0 → validate sign-in independently
3. Add US1 → validate core content → MVP
4. Add US2 → validate navigation/taxonomy
5. Add US3 → validate subscribers/newsletters/mail
6. Polish phase → branding, asset guard, full quickstart pass, documentation, constitution follow-up

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story (US0–US3) for traceability
- Verify tests fail before implementing them
- T009 and T014 are the two highest-risk tasks in this feature — one changes a security default, the other is a manual step without which nothing else can be verified. Do not skip or defer either.
- T009a is the highest-risk *deployment* task in this feature (gate finding critic-001, SHOWSTOPPER) — unlike T009/T014, which affect this feature's own behavior, a missed T009a affects the entire API's ability to start. Treat it as a release-blocking checklist item, not an optional nice-to-have.
