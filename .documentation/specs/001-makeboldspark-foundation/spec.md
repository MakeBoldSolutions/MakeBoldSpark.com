---
classification: full-spec
risk_level: medium
target_workflow: specify-full
required_artifacts: spec, plan, tasks
recommended_next_step: plan
required_gates: checklist, analyze, critic
---

# Feature Specification: MakeBoldSpark Platform Foundation

**Feature Branch**: `001-makeboldspark-foundation`
**Created**: 2026-05-07
**Status**: Complete <!-- Valid: Draft | In Progress | Complete -->
**Input**: MakeBoldSpark Jumpstart Guide — `.documentation/MakeBoldSpark-Jumpstart-Guide.md`

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

- MakeBoldSpark Jumpstart Guide (`.documentation/MakeBoldSpark-Jumpstart-Guide.md`) — Section 24 "First Milestone Recommendation"
- MakeBoldSpark Constitution v1.1.0 (`.documentation/memory/constitution.md`) — Principles I–X
- Jumpstart Guide Phases 0–2 and Section 19 "Initial Agent Prompt"

### Tradeoffs Considered

- Option A: Implement all nine phases in a single spec — rejected; overbuilds before the foundation is validated
- Option B: Minimal single-file API with no structure — rejected; creates technical debt before the first feature
- Selected: Phased foundation spec (Phases 0–2) — establishes architectural patterns cleanly without premature complexity

### Architectural Impact

- Introduces the single-backend modular API pattern that all future MakeBoldSpark features must follow
- Establishes the SQLite persistence model as the authoritative data store (no Cosmos dependency)
- Sets the authorization boundary contract (public = anonymous, admin/publish/integrations = authenticated)
- GitHub Actions build/test/deploy pipeline becomes the CI/CD baseline for all future work

### Reviewer Guidance

Reviewers should verify: route group structure matches the constitution authorization model; public endpoints are anonymous and read-only; admin routes return 401/403 for unauthenticated callers; SQLite is initialized on first start; no `.db` files or secrets are committed; tests cover meaningful behavior, not just structure.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Platform Health Verification (Priority: P1)

Any caller — monitoring service, developer, or static web app — needs to confirm the MakeBoldSpark backend is running and reachable. A simple, anonymous health endpoint satisfies this without exposing internal state.

**Why this priority**: Every other scenario depends on the platform being available. This is the minimum viable proof that the deployment works.

**Independent Test**: Can be tested by sending `GET /api/health` without authentication and verifying a JSON response with status, service name, and version is returned.

**Acceptance Scenarios**:

1. **Given** the MakeBoldSpark API is deployed and running, **When** an anonymous caller sends `GET /api/health`, **Then** the response is `200 OK` with a JSON body containing `status: "Healthy"`, `service: "MakeBoldSpark"`, and a non-empty version string.
2. **Given** the MakeBoldSpark API is running, **When** the health endpoint is called repeatedly, **Then** it consistently returns `200 OK` without requiring any credentials.

---

### User Story 2 — Public Content Browsing (Priority: P2)

A website visitor, static web app, or developer consuming the API needs to retrieve a list of published articles and individual article details — without logging in. This is the core public read feature that justifies the platform.

**Why this priority**: Demonstrates the primary read path (anonymous public content) and the SQLite data layer working end-to-end. Required before admin or publishing features have any data to act on.

**Independent Test**: Can be tested by seeding the database with sample articles and tags, then calling `GET /api/public/content/articles` and `GET /api/public/content/tags` anonymously; at least one seeded published article must appear in the articles response, draft articles must not appear, and all tags must appear in the tags response.

**Acceptance Scenarios**:

1. **Given** the database contains published articles, **When** an anonymous caller sends `GET /api/public/content/articles`, **Then** the response is `200 OK` with a list containing only published articles.
2. **Given** a published article with slug `"hello-world"` exists, **When** an anonymous caller sends `GET /api/public/content/articles/hello-world`, **Then** the response is `200 OK` with the article details.
3. **Given** a draft article with slug `"my-draft"` exists, **When** an anonymous caller requests that article, **Then** the response is `404 Not Found` (draft content is not exposed publicly).
4. **Given** the database is empty, **When** an anonymous caller requests articles, **Then** the response is `200 OK` with an empty list — not an error.
5. **Given** no article matches the requested slug, **When** an anonymous caller requests that slug, **Then** the response is `404 Not Found` with an appropriate message.
6. **Given** the database contains tags, **When** an anonymous caller sends `GET /api/public/content/tags`, **Then** the response is `200 OK` with a list of all tag names.
7. **Given** the database contains no tags, **When** an anonymous caller requests tags, **Then** the response is `200 OK` with an empty list.

---

### User Story 3 — Admin Route Protection (Priority: P3)

An authenticated admin needs to be able to reach admin-area routes. Unauthenticated callers must be rejected at the authorization boundary without exposing any data.

**Why this priority**: Establishing the authorization boundary early prevents accidental public exposure of admin operations as the platform grows. The actual admin CRUD endpoints are a later phase; this story verifies the boundary works.

**Independent Test**: Can be tested by calling any `/api/admin/*` route without credentials and confirming `401 Unauthorized` is returned; calling with valid admin credentials must reach the endpoint.

**Acceptance Scenarios**:

1. **Given** an unauthenticated caller, **When** they send any request to `/api/admin/*`, **Then** the response is `401 Unauthorized`.
2. **Given** an authenticated admin caller, **When** they send a request to a registered admin route, **Then** the response is not `401` or `403`.
3. **Given** an authenticated caller without the Admin role, **When** they send a request to an admin-only route, **Then** the response is `403 Forbidden`.

---

### User Story 4 — API Documentation Discovery (Priority: P4)

A developer evaluating or consuming the MakeBoldSpark API needs to explore available endpoints through interactive documentation — but only in development. Production must not expose the Swagger UI.

**Why this priority**: Reduces onboarding friction for future developers; required by the constitution's API-first principle to demonstrate OpenAPI contracts are in place before implementation.

**Independent Test**: Can be tested by running the API in the Development environment and navigating to the Swagger UI endpoint; confirming the UI lists all implemented endpoints; confirming the Production environment does not expose the Swagger UI.

**Acceptance Scenarios**:

1. **Given** the API is running in the Development environment, **When** a developer opens the Swagger UI endpoint, **Then** the OpenAPI documentation listing all implemented endpoints is displayed.
2. **Given** the API is running in the Production environment, **When** a caller requests the Swagger UI endpoint, **Then** the response is `404 Not Found` or the route is not registered.

---

### User Story 5 — Automated Build and Test Validation (Priority: P5)

A developer contributing a code change via a pull request needs confidence that the build succeeds and tests pass before merging — without manual intervention.

**Why this priority**: The constitution requires test-first development; CI enforcement is the gate that makes this non-negotiable.

**Independent Test**: Can be tested by opening a pull request to main and observing that the GitHub Actions build/test workflow runs and reports a pass or fail result.

**Acceptance Scenarios**:

1. **Given** a pull request is opened to the `main` branch, **When** GitHub Actions runs, **Then** the build and all tests complete and a pass/fail result is reported on the PR.
2. **Given** a test is failing, **When** a PR is opened, **Then** GitHub Actions reports failure and the PR is blocked from merging.
3. **Given** all tests pass, **When** a merge to `main` occurs, **Then** GitHub Actions deploys the updated application to Azure App Service.

---

### User Story 6 — Local Developer Setup (Priority: P6)

A new developer (or AI coding agent) who clones the repository needs to get the API running locally with a seeded SQLite database within a short setup time — using only the repository and standard .NET tooling.

**Why this priority**: A clean local setup with seed data is a prerequisite for any future development work. The Jumpstart Guide targets under 10 minutes to first run.

**Independent Test**: Can be tested by cloning the repository, running `dotnet run`, and confirming the health endpoint responds and seeded articles are returned by the public content endpoint.

**Acceptance Scenarios**:

1. **Given** a developer has .NET 10 SDK installed and clones the repository, **When** they run the API project locally without any additional configuration, **Then** the SQLite database is created automatically and seeded with sample data.
2. **Given** the API is running locally, **When** the developer calls `GET /api/public/content/articles`, **Then** seeded articles are returned.
3. **Given** the API is running locally with the Development environment, **When** the developer opens the Swagger UI, **Then** all registered endpoints are discoverable.

---

### Edge Cases

- What happens when the SQLite database file is missing at startup in production?
- What happens when EF Core migrations fail at startup?
- What happens if the `/home/data/` path is not writable in Azure App Service?
- What happens when an article slug contains special characters or URL-unsafe characters?
- What happens when two articles have the same slug?
- What happens if seed data is applied repeatedly (guard logic must prevent duplicate seeding)?
- What happens when the API is called before migrations complete (race condition on first start)?

---

## Requirements *(mandatory)*

### Functional Requirements

**Route Platform**

- **FR-001**: Platform MUST expose a `/api/health` endpoint returning service name, status, and version without requiring authentication.
- **FR-002**: Platform MUST register separate route groups for `/api/public`, `/api/admin`, `/api/publish`, `/api/integrations`, and `/api/health`. Routes MUST NOT include a version prefix; the unversioned path forms are canonical for this platform.
- **FR-002a**: If API versioning is introduced in a future phase, it MUST be documented in an ADR before any route contracts change.
- **FR-003**: All routes under `/api/public` and `/api/health` MUST be accessible without authentication.
- **FR-004**: All routes under `/api/admin` MUST require authentication and the Admin authorization policy.
- **FR-005**: All routes under `/api/publish` MUST require authentication and the Publisher authorization policy.
- **FR-006**: All routes under `/api/integrations` MUST require authentication via admin role or service token.
- **FR-007**: Platform MUST expose OpenAPI/Swagger documentation only when running in the Development environment.
- **FR-008**: Platform MUST configure CORS to allow only explicitly listed static client origins (no wildcard for authenticated routes).
- **FR-009**: Platform MUST use structured logging capturing at minimum on every request: request path, HTTP method, status code, duration, correlation ID, user ID or auth subject (when the caller is authenticated), feature name, operation name, and a success/failure indicator.

**Data Layer**

- **FR-010**: Platform MUST use EF Core + SQLite as the default persistence model.
- **FR-011**: The SQLite connection string MUST be configurable via application settings (not hardcoded).
- **FR-012**: In production, the SQLite database MUST reside at `/home/data/makeboldspark.db` (Azure App Service persistent storage).
- **FR-013**: In local development, the SQLite database MUST default to a path under the project output directory (not under `/home/data`).
- **FR-014**: Platform MUST apply EF Core migrations on startup when the `Database:ApplyMigrationsOnStartup` setting is `true`.
- **FR-015**: Seed data MUST only be applied when the target table (Articles) is empty — never on every startup.
- **FR-016**: Production `.db` files MUST NOT be committed to source control or included in deployment artifacts.
- **FR-017**: Deployment artifacts MUST NOT overwrite the `/home/data/` directory.

**Public Content Feature**

- **FR-018**: Platform MUST expose `GET /api/public/content/articles` returning a list of published articles only. The response MUST return all published articles without pagination (no page/size parameters). Each item in the list MUST include: slug, title, summary, publish date, and tags. The full body content MUST NOT be included in the list response.
- **FR-019**: Platform MUST expose `GET /api/public/content/articles/{slug}` returning the full detail of a specific published article, including the complete body content.
- **FR-020**: Articles with status `Draft` MUST NOT be returned by public endpoints.
- **FR-020a**: Platform MUST expose `GET /api/public/content/tags` returning the list of all tags. This endpoint MUST be accessible without authentication and MUST return at minimum each tag's name.
- **FR-021**: An Article MUST have at minimum: slug, title, summary, content body (stored as raw Markdown), publish date, status (Draft/Published), and associated tags. The API MUST return the body as a raw Markdown string; rendering is the responsibility of the consuming client or export layer.
- **FR-022**: Slugs MUST be unique across all articles.

**Security**

- **FR-023**: Secrets, API keys, connection strings with credentials, and tokens MUST NOT be committed to source control.
- **FR-024**: Authorization policies for Admin, Publisher, and ServiceOrAdmin MUST be registered, even if no admin endpoints are implemented in this phase.

**CI/CD**

- **FR-025**: A GitHub Actions workflow MUST run `dotnet build` and `dotnet test` on every pull request to `main`.
- **FR-026**: A GitHub Actions workflow MUST build, test, publish, and deploy to Azure App Service on merge to `main`.
- **FR-027**: The deploy workflow MUST NOT deploy production `.db` files.

### Key Entities *(include if feature involves data)*

- **Article**: Represents a published content piece. Key attributes: unique slug (URL-safe identifier), title, summary (short description), body (full content), publish date, status (Draft or Published), and zero-or-more associated tags.
- **Tag**: Represents a content categorization label. Key attributes: name (unique), display label. Associated with zero-or-more articles.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The health endpoint responds with a correct healthy status within 500 milliseconds for all callers, including cold starts.
- **SC-002**: All seeded published articles are retrievable via the public content endpoints without authentication.
- **SC-003**: Any unauthenticated request to an admin route is rejected with a `401 Unauthorized` response — no data leaks.
- **SC-004**: GitHub Actions build and test workflow completes successfully on every pull request to `main` with zero manual intervention.
- **SC-005**: The SQLite database persists across application redeployments without data loss.
- **SC-006**: A developer with .NET 10 SDK installed can clone the repository, run the API locally, and retrieve seeded articles within 10 minutes.
- **SC-007**: The OpenAPI documentation endpoint lists all implemented endpoints in the Development environment and is not accessible in Production.
- **SC-008**: No secrets, API keys, or production `.db` files exist in the repository's git history.

---

## Clarifications

### Session 2026-05-07

- Q: Should `GET /api/public/content/articles` support pagination? → A: No pagination — return all published articles in a single response. List items include summary fields only (slug, title, summary, publish date, tags); full body is reserved for the detail endpoint.
- Q: Should routes include a version prefix (e.g., `/api/v1/public/...`)? → A: No versioning — `/api/public/...` is the canonical unversioned route form. Versioning may be introduced later via ADR if breaking changes require it.
- Q: What format should the article body content be stored in? → A: Markdown — raw Markdown string stored and returned as-is; rendering is delegated to the consuming client or export layer.
- Q: Should `GET /api/public/content/tags` be in scope for this foundation spec? → A: Yes — in scope; tags endpoint returns all tag names anonymously alongside the articles endpoints.
- Q: Should FR-009 structured logging align with the full Jumpstart Guide §16 minimum log fields? → A: Yes — full alignment: request path, method, status code, duration, correlation ID, user ID/auth subject (when authenticated), feature name, operation name, and success/failure indicator required on every request.
