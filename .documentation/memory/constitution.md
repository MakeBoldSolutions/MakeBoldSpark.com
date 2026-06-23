<!--
SYNC IMPACT REPORT
Version change: 1.1.0 → 1.2.0
Bump type: MINOR — an explicit anonymous authentication route category was added to Principle VIII.
Modified principles: None renamed or removed.
Added principles: None.
Added sections: None.
Removed sections: None.
Templates reviewed:
  - .devspark/templates/plan-template.md: ✅ Constitution Check gate is generic and backs new principles correctly — no change required.
  - .devspark/templates/spec-template.md: ✅ No changes required.
  - .devspark/templates/tasks-template.md: ✅ No changes required.
Deferred items: None.
-->

# MakeBoldSpark Constitution

**Version**: 1.2.0 | **Ratified**: 2026-05-06 | **Last Amended**: 2026-06-23 | **Author**: Mark Hazleton

## Core Principles

### I. API-First (NON-NEGOTIABLE)

Design and document APIs before implementation begins.
All endpoints MUST have defined OpenAPI/Swagger contracts before any implementation work starts.
API design decisions MUST be explicit, versioned, and backward-compatible.

### II. Test-First (NON-NEGOTIABLE)

Tests MUST be written before or alongside implementation — never after.
No feature is considered complete without passing tests that verify its behavior.
All PRs must include tests; untested code changes are rejected in review.

### III. Simplicity (NON-NEGOTIABLE)

Prefer simple, readable solutions over clever ones.
Complexity MUST be justified. Reject abstractions that serve only one use case.
If a solution requires a long explanation, reconsider the solution.

### IV. Security by Default (NON-NEGOTIABLE)

Treat security as a first-class concern in every change.
Input validation, authentication, authorization, and secrets management MUST be considered at design time.
Security issues are showstopper severity in PR review.

### V. Spec-Driven Development

All features MUST be specified before they are planned or implemented.
The workflow is: specify → plan → tasks → implement → PR.
Skipping specification requires explicit documented justification.

### VI. Ownership Boundary

`.devspark/` is the installed framework payload — the only directory DevSpark installs, upgrades, or removes.
`.documentation/` directories are repository-owned work product and MUST never be modified by framework operations.

### VII. Single Backend Platform (NON-NEGOTIABLE)

MakeBoldSpark MUST remain one modular ASP.NET Core application — not a collection of microservices or per-API repositories.
Multiple small APIs are hosted under one backend through clearly separated route groups and feature folders.
Splitting into separate services or repos MUST only occur when scale, security, release cadence, or reliability
explicitly justifies the additional operational complexity, and requires documented justification.

### VIII. Clear Authorization Boundaries (NON-NEGOTIABLE)

All routes MUST fall under one of the following defined authorization categories:

| Route Area | Access Model |
|---|---|
| `/api/public/*` | Anonymous, read-only |
| `/api/public/auth/*` | Anonymous credential verification and token issuance only |
| `/api/admin/*` | Authenticated admin only |
| `/api/publish/*` | Publisher or admin |
| `/api/integrations/*` | Admin or service token |
| `/api/health` | Anonymous shallow health |
| `/api/admin/health/deep` | Admin only |

Public content routes MUST NOT expose write operations or sensitive data. Public authentication
routes MAY accept credentials only to verify identity and issue a signed access token; they MUST
NOT expose CMS data, accept CMS-data writes, or reveal credential-verification details.
Admin, publishing, backup, and integration routes MUST require explicit ASP.NET Core policy-based authorization.

### IX. Relational-First Data Strategy (NON-NEGOTIABLE)

EF Core + SQLite is the default persistence model for all content, CMS, and API data.
Cosmos DB MUST only be used selectively for document-oriented features or portfolio demonstrations where
document storage is the natural fit.
Cosmos DB MUST NOT become the default store.
Production SQLite database files MUST reside under `/home/data/` (Azure App Service persistent storage)
and MUST NOT be committed to source control or included in deployment artifacts.
Deployments update `/home/site/wwwroot` only and MUST NOT overwrite `/home/data`.

### X. Zero Secrets in Source Control (NON-NEGOTIABLE)

Secrets, API keys, connection strings containing credentials, GitHub tokens, Cosmos keys, and publishing
tokens MUST NOT be committed to source control under any circumstance.
Use Azure App Service application settings and GitHub Actions secrets for all runtime secrets.
Service-token access MUST be limited to narrow, specific routes.

## Platform Architecture

MakeBoldSpark is a consolidated backend API platform for small, low-volume personal and portfolio APIs,
hosted on Azure App Service Linux B1 and consumed by Azure Static Web Apps clients.

### Hosting Model

| Component | Role |
|---|---|
| Azure App Service Linux B1 | Single backend API host (`api.markhazleton.com`) |
| Azure Static Web Apps | Public websites and static clients |
| SQLite `/home/data/makeboldspark.db` | Default persistent relational data store |
| Cosmos DB | Selective document-oriented features only |
| Blob Storage | Backups and exported artifacts |
| GitHub | Source control, deployment automation, and optional publishing target |

### Static-First Client Model

Public-facing sites SHOULD default to static content and versioned generated JSON consumed by Azure Static Web Apps.
Live API calls should be reserved for dynamic features only.
Generated static artifacts follow a versioned manifest pattern:

- `/data/manifest.json` — version pointer, short cache TTL
- `/data/{collection}.v{version}.json` — immutable, long cache TTL

### Feature Structure

New features MUST follow the feature folder pattern:

```text
Features/{FeatureName}/
  {FeatureName}Endpoints.cs
  {FeatureName}Service.cs
  {FeatureName}Models.cs
```

Data access MUST follow the layered pattern:

```text
Endpoint → Service → Repository → DbContext
```

Do not inject `DbContext` directly into endpoint methods except for intentionally trivial read-only demos.

### Database Deployment Model

```text
/home/site/wwwroot/   — deployed app code (updated by GitHub Actions)
/home/data/           — persistent SQLite database files (never overwritten by deployment)
/home/backups/        — local backup staging area
```

First-start behavior MUST: check for database existence, apply migrations if configured, and seed only when
the target table is empty.

## Technology Stack

- **Runtime**: .NET 10 LTS
- **Framework**: ASP.NET Core (Minimal APIs with route groups)
- **API Documentation**: OpenAPI / Swagger (development environment only)
- **Data (default)**: EF Core + SQLite
- **Data (selective)**: Azure Cosmos DB
- **Hosting**: Azure App Service Linux B1
- **Static Clients**: Azure Static Web Apps
- **CI/CD**: GitHub Actions (build/test on PR; deploy on merge to `main`)
- **Testing**: xUnit / NUnit / MSTest with test-first discipline
- **Scripts**: PowerShell (primary), Bash (cross-platform fallback)
- **Source Control**: Git / GitHub

## Development Workflow

- All features follow the DevSpark spec-driven workflow:
  `/devspark.specify` → `/devspark.plan` → `/devspark.tasks` → `/devspark.implement` → `/devspark.create-pr`
- All PRs and reviews MUST verify compliance with this constitution before merge
- API contracts MUST be defined and reviewed before implementation begins
- Security review is required for any changes to authentication, authorization, input handling, or data access
- Deployment artifacts MUST NOT include production `.db` files
- No unnecessary packages or dependencies MUST be added without justification
- Documentation and ADRs MUST be updated when architecture decisions change

## Governance

This constitution is authoritative over all development practices in this repository.
Amendments require documentation of the change, author approval, and a migration plan for any affected workflows.

Version increments follow semantic versioning:

- **MAJOR**: Backward-incompatible governance or principle removals/redefinitions.
- **MINOR**: New principle or section added, or materially expanded guidance.
- **PATCH**: Clarifications, wording fixes, non-semantic refinements.

**Project**: MakeBoldSpark
**Version**: 1.2.0
**Ratified**: 2026-05-06
**Last Amended**: 2026-06-23
