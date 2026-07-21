# MakeBoldSpark Backbone

**Source**: migrated(`.documentation/memory/constitution.md` v1.2.0, ratified 2026-05-06, last amended 2026-06-23, author Mark Hazleton)

Full supporting detail (platform architecture, tech stack, development workflow, governance) lives in [`bold-docs/system/architecture.md`](system/architecture.md).

## Principles

### I. API-First
**Status**: enforced
Design and document APIs before implementation. All endpoints must have OpenAPI/Swagger contracts before implementation starts. API design decisions must be explicit, versioned, and backward-compatible.

### II. Test-First
**Status**: enforced
Tests are written before or alongside implementation, never after. No feature is complete without passing tests. All PRs include tests; untested changes are rejected in review.

### III. Simplicity
**Status**: enforced
Prefer simple, readable solutions. Complexity must be justified; reject abstractions serving only one use case. If a solution needs a long explanation, reconsider it.

### IV. Security by Default
**Status**: enforced
Security is a first-class concern in every change. Input validation, authentication, authorization, and secrets management are considered at design time. Security issues are showstopper severity in review.

### V. Spec-Driven Development
**Status**: adopting
All features are specified before they are planned or implemented. Workflow: specify → plan → tasks → implement → ship. Skipping specification requires explicit documented justification.

### VI. Ownership Boundary
**Status**: adopting
`.bold/` is the installed framework payload — the only directory Bold installs, upgrades, or removes. `bold-docs/` and repo content are project-owned work product, never modified by framework operations.

### VII. Single Backend Platform
**Status**: enforced
MakeBoldSpark remains one modular ASP.NET Core application, not a collection of microservices or per-API repositories. Multiple small APIs are hosted under one backend via clearly separated route groups and feature folders. Splitting into separate services/repos requires documented justification (scale, security, release cadence, or reliability).

### VIII. Clear Authorization Boundaries
**Status**: enforced
All routes fall under a defined authorization category (`/api/public/*` anonymous read-only, `/api/public/auth/*` anonymous credential verification and token issuance only, `/api/admin/*` authenticated admin only, `/api/publish/*` publisher or admin, `/api/integrations/*` admin or service token, `/api/health` anonymous shallow health, `/api/admin/health/deep` admin only). Public routes never expose write operations, sensitive data, or CMS-data access. Admin/publishing/backup/integration routes require explicit ASP.NET Core policy-based authorization.

### IX. Relational-First Data Strategy
**Status**: enforced
EF Core + SQLite is the default persistence model for all content, CMS, and API data. Cosmos DB is used only selectively for document-oriented features or portfolio demonstrations, never as the default store. Production SQLite files reside under `/home/data/` (Azure App Service persistent storage) and are never committed to source control. Deployments update `/home/site/wwwroot` only, never `/home/data`.

### X. Zero Secrets in Source Control
**Status**: enforced
Secrets, API keys, connection strings with credentials, tokens, and Cosmos keys are never committed to source control. Runtime secrets live in Azure App Service application settings and GitHub Actions secrets. Service-token access is limited to narrow, specific routes.

## Governance

This backbone is authoritative over development practices in this repository. Amendments require documentation of the change, author approval, and a migration plan for affected workflows.
