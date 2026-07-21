# MakeBoldSpark Architecture

**Source**: migrated(`.documentation/memory/constitution.md` v1.2.0) — supporting detail for [`bold-docs/backbone.md`](../backbone.md)

## Platform Architecture

MakeBoldSpark is a consolidated backend API platform for small, low-volume personal and portfolio APIs, hosted on Azure App Service Linux B1 and consumed by Azure Static Web Apps clients.

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

Public-facing sites default to static content and versioned generated JSON consumed by Azure Static Web Apps. Live API calls are reserved for dynamic features only. Generated static artifacts follow a versioned manifest pattern:

- `/data/manifest.json` — version pointer, short cache TTL
- `/data/{collection}.v{version}.json` — immutable, long cache TTL

### Feature Structure

New features follow the feature folder pattern:

```text
Features/{FeatureName}/
  {FeatureName}Endpoints.cs
  {FeatureName}Service.cs
  {FeatureName}Models.cs
```

Data access follows the layered pattern:

```text
Endpoint → Service → Repository → DbContext
```

`DbContext` is not injected directly into endpoint methods except for intentionally trivial read-only demos.

### Database Deployment Model

```text
/home/site/wwwroot/   — deployed app code (updated by GitHub Actions)
/home/data/           — persistent SQLite database files (never overwritten by deployment)
/home/backups/        — local backup staging area
```

First-start behavior: check for database existence, apply migrations if configured, seed only when the target table is empty.

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

- Features follow Bold's spec-driven workflow: `bold.plan` → `bold.build` → `bold.ship`
- All PRs and reviews verify compliance with `bold-docs/backbone.md` before merge
- API contracts are defined and reviewed before implementation begins
- Security review is required for any changes to authentication, authorization, input handling, or data access
- Deployment artifacts must not include production `.db` files
- No unnecessary packages or dependencies are added without justification
- Documentation and ADRs are updated when architecture decisions change
