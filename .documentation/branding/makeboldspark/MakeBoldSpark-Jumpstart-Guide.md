# MakeBoldSpark Jumpstart Guide

**Version:** 0.1  
**Target Runtime:** .NET 10 LTS  
**Primary Hosting Target:** Azure App Service Linux B1  
**Primary Client Model:** Azure Static Web Apps  
**Primary Data Model:** EF Core + SQLite, with selective Cosmos DB usage  
**Audience:** Human maintainers, solution architects, developers, and AI coding agents

---

## 1. Executive Summary

**MakeBoldSpark** is a consolidated backend API platform for small, low-volume personal and portfolio APIs. It is intended to replace scattered API hosting, Windows/IIS dependency, and fragmented project structures with a single modular ASP.NET Core backend deployed to Azure App Service Linux.

The core strategy is simple:

> Use one backend API as the shared platform for content, CMS/admin functions, publishing workflows, integrations, and reusable data services. Use Azure Static Web Apps for public-facing clients that consume static JSON or public read-only API endpoints.

MakeBoldSpark is not a microservices platform. It is a practical, low-cost, modular backend designed for personal systems, portfolio projects, content websites, prompt catalogs, publishing workflows, and lightweight integrations.

---

## 2. Guiding Principles

1. **Single backend, many clients**  
   Host multiple small APIs in one ASP.NET Core application and expose them through clear route groups.

2. **Static-first public websites**  
   Public sites should default to static content, generated JSON, and browser/client caching. Live API calls should be reserved for dynamic features.

3. **Relational by default**  
   Use EF Core + SQLite as the primary data access model because it is simple, portable, inexpensive, and easy to migrate later.

4. **Cosmos DB selectively**  
   Use Cosmos DB only where document storage makes sense or where demonstrating Cosmos skillset adds portfolio value.

5. **Low-cost Azure-native hosting**  
   Target Azure App Service Linux B1 for the backend and Azure Static Web Apps for public clients.

6. **Browser-based CMS capability**  
   Support authenticated admin workflows for content creation, editing, publishing, and backup.

7. **Clear security boundaries**  
   Public read endpoints are anonymous. Admin, publishing, backup, and integration endpoints require authorization.

8. **Avoid premature microservices**  
   Split APIs only when scale, security, release cadence, or reliability justifies the additional operational complexity.

9. **AI-agent-friendly repository**  
   Structure the repo so AI coding agents can safely add features without spreading logic randomly across the codebase.

10. **Portfolio-grade documentation**  
    The repository should demonstrate practical architecture decisions, not just working code.

---

## 3. Target Architecture

```text
Azure Static Web Apps
  ├── markhazleton.com
  ├── PromptSpark client
  ├── GitHubStatsSpark client
  ├── DevSpark docs/client
  └── Other static/public clients
          │
          │ reads static JSON or public API endpoints
          ▼
Azure App Service Linux B1
  api.markhazleton.com
  └── MakeBoldSpark ASP.NET Core API
        ├── /api/public/*
        ├── /api/admin/*
        ├── /api/publish/*
        ├── /api/integrations/*
        └── /api/health
              │
              ├── EF Core + SQLite
              │     └── /home/data/makeboldspark.db
              │
              ├── Optional Cosmos DB features
              │
              ├── Blob Storage backups/exports
              │
              └── GitHub publishing/export workflows
```

---

## 4. Runtime Responsibility Split

| Component | Responsibility |
|---|---|
| Azure Static Web Apps | Public websites, static clients, generated content consumption, static assets, custom domains |
| MakeBoldSpark Backend API | Shared API platform, CMS/admin, integrations, publishing, content export, dynamic endpoints |
| SQLite | Low-cost relational data store for CMS/content/API data |
| Cosmos DB | Selective document-oriented features and portfolio examples |
| Blob Storage | Backups, exported artifacts, media, recovery copies |
| GitHub | Source control, deployment automation, audit trail, optional publishing target |

---

## 5. Recommended Repository Structure

```text
MakeBoldSpark/
  README.md
  MakeBoldSpark.sln

  src/
    MakeBoldSpark.Api/
      Program.cs
      appsettings.json
      appsettings.Development.json
      Features/
        PublicContent/
        AdminCms/
        Publishing/
        Prompts/
        Systems/
        Feeds/
        Search/
        Integrations/
        CosmosDemo/
        Health/
      Infrastructure/
        Auth/
        Data/
        Sqlite/
        Cosmos/
        Storage/
        Publishing/
        Observability/
        Options/
      Common/
        Results/
        Validation/
        Extensions/
        Middleware/

    MakeBoldSpark.Domain/
      Content/
      Prompts/
      Systems/
      Tags/
      Navigation/
      Publishing/
      Shared/

    MakeBoldSpark.Data/
      PlatformDbContext.cs
      Sqlite/
      Cosmos/
      Repositories/
      Migrations/
      Seed/

    MakeBoldSpark.Export/
      StaticJson/
      SearchIndex/
      GitHubPublishing/

  tests/
    MakeBoldSpark.Api.Tests/
    MakeBoldSpark.Domain.Tests/
    MakeBoldSpark.Data.Tests/
    MakeBoldSpark.Export.Tests/

  docs/
    architecture/
      overview.md
      data-strategy.md
      static-publishing.md
      authentication-authorization.md
      deployment-topology.md
    deployment/
      azure-app-service-linux.md
      github-actions.md
      sqlite-backup-restore.md
    decisions/
      0001-single-backend-api.md
      0002-sqlite-default-with-selective-cosmos.md
      0003-static-web-app-clients.md
      0004-dotnet-10-lts.md
      0005-authentication-boundaries.md

  data/
    seed/
    samples/
    exports/

  .github/
    workflows/
      build-test.yml
      deploy-azure-app-service.yml
```

### Initial Simplification Option

If the first implementation should stay smaller, start with:

```text
MakeBoldSpark/
  src/
    MakeBoldSpark.Api/
      Features/
      Infrastructure/
      Data/
  tests/
    MakeBoldSpark.Api.Tests/
  docs/
  .github/workflows/
```

Then split into additional projects when the codebase warrants it.

---

## 6. Route Design

### Public Routes

Anonymous, read-only, safe for Static Web Apps to consume.

```text
/api/public/content
/api/public/content/articles
/api/public/content/articles/{slug}
/api/public/systems
/api/public/prompts
/api/public/feeds
/api/public/search
/api/public/manifest
```

### Admin Routes

Authenticated and authorized. Used by browser-based CMS/admin tools.

```text
/api/admin/content
/api/admin/content/articles
/api/admin/content/articles/{id}
/api/admin/prompts
/api/admin/systems
/api/admin/settings
/api/admin/backups
```

### Publishing Routes

Authenticated and authorized. Used to generate or publish static artifacts.

```text
/api/publish/export
/api/publish/static-json
/api/publish/github
/api/publish/preview
```

### Integration Routes

Authenticated or service-token protected. Used for external syncs or automation.

```text
/api/integrations/github
/api/integrations/rss
/api/integrations/cosmos-demo
```

### Health Routes

```text
/api/health              # anonymous shallow health
/api/admin/health/deep   # admin-only deep diagnostics
```

---

## 7. Minimal API Route Group Pattern

Use route groups to keep endpoint boundaries clear.

```csharp
var publicApi = app.MapGroup("/api/public");
publicApi.MapContentApi();
publicApi.MapPromptApi();
publicApi.MapSystemsApi();

var adminApi = app.MapGroup("/api/admin")
    .RequireAuthorization("AdminOnly");
adminApi.MapContentAdminApi();
adminApi.MapBackupApi();

var publishApi = app.MapGroup("/api/publish")
    .RequireAuthorization("Publisher");
publishApi.MapPublishingApi();

var integrationApi = app.MapGroup("/api/integrations")
    .RequireAuthorization("ServiceOrAdmin");
integrationApi.MapGitHubIntegrationApi();
integrationApi.MapCosmosDemoApi();
```

Recommended file pattern:

```text
Features/
  PublicContent/
    PublicContentEndpoints.cs
    ContentService.cs
    ContentModels.cs

  AdminCms/
    AdminContentEndpoints.cs
    AdminContentService.cs
    AdminContentModels.cs

  Publishing/
    PublishingEndpoints.cs
    StaticJsonExportService.cs
    GitHubPublishService.cs
```

---

## 8. Data Strategy

### 8.1 Default: SQLite + EF Core

SQLite is the default store for:

- content metadata
- articles/pages
- tags
- systems/projects
- prompts
- navigation
- publishing state
- CMS configuration
- lightweight API data
- local development

Production database path:

```text
/home/data/makeboldspark.db
```

Local development path:

```text
./data/makeboldspark.local.db
```

Use configuration to control the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./data/makeboldspark.local.db"
  }
}
```

Azure App Service setting:

```text
ConnectionStrings__DefaultConnection=Data Source=/home/data/makeboldspark.db
```

### 8.2 Optional: Multiple SQLite Databases

Use one database initially unless there is a clear isolation need.

Possible future split:

```text
/home/data/content.db
/home/data/prompts.db
/home/data/systems.db
/home/data/makeboldspark-admin.db
```

Use separate databases only when backup/restore or domain isolation justifies it.

### 8.3 Cosmos DB Selective Use

Cosmos DB should be used for:

- document-shaped sample features
- portfolio demonstrations
- features that naturally partition by document owner/category/site
- low-risk cloud-native examples

Cosmos should not become the default store merely because it is available.

### 8.4 Repository Abstraction

Use interfaces to preserve migration options.

```csharp
public interface IContentRepository
{
    Task<Article?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArticleSummary>> GetPublishedAsync(CancellationToken cancellationToken = default);
    Task<Article> SaveAsync(Article article, CancellationToken cancellationToken = default);
}
```

Potential implementations:

```text
SqliteContentRepository
AzureSqlContentRepository
CosmosContentRepository, only where appropriate
```

---

## 9. SQLite Deployment Model

### 9.1 Code vs Data Separation

```text
/home/site/wwwroot/        deployed app code
/home/data/                persistent SQLite database files
/home/backups/             optional local backup staging
```

GitHub deployments update `/home/site/wwwroot`. They should not overwrite `/home/data`.

### 9.2 First-Start Creation

On first startup:

1. Check whether `/home/data/makeboldspark.db` exists.
2. If missing, create database.
3. Apply EF Core migrations.
4. Seed initial data if configured.

Controlled startup migration example:

```csharp
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<MakeBoldSparkDbContext>();

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    db.Database.Migrate();
}
```

Azure setting:

```text
Database__ApplyMigrationsOnStartup=true
```

### 9.3 Seeding Strategy

Recommended seed sources:

| Seed Type | Source |
|---|---|
| Schema | EF Core migrations |
| Sample content | JSON seed files |
| Curated initial database | Sanitized `.db` upload |
| Production restore | Backup `.db` from Blob Storage |

Do not reseed blindly on every deployment.

Use guard logic:

```csharp
if (!await db.Articles.AnyAsync(cancellationToken))
{
    await SeedData.LoadAsync(db, cancellationToken);
}
```

---

## 10. Backup and Restore Strategy

### 10.1 Backup Goals

Backups should support:

- manual download
- scheduled backup
- pre-migration backup
- restore to local development
- restore to App Service
- archival of content state

### 10.2 Recommended Backup Flow

```text
MakeBoldSpark App Service
  /home/data/makeboldspark.db

Backup action
  1. Create consistent SQLite backup copy
  2. Store temporary copy under /home/backups
  3. Upload timestamped backup to Blob Storage
  4. Optionally expose protected download link
```

Backup filename convention:

```text
makeboldspark-YYYY-MM-DD-HHmm.db
makeboldspark-YYYY-MM-DD-HHmm.db.zip
```

### 10.3 SQLite Backup API

Do not rely only on copying the live `.db` file during active writes. Use SQLite backup behavior.

Conceptual .NET example:

```csharp
using var source = new SqliteConnection("Data Source=/home/data/makeboldspark.db");
using var destination = new SqliteConnection("Data Source=/home/backups/makeboldspark-backup.db");

source.Open();
destination.Open();

source.BackupDatabase(destination);
```

### 10.4 Admin Backup Endpoints

Protected by admin authorization:

```text
POST /api/admin/backups/create
GET  /api/admin/backups
GET  /api/admin/backups/{backupId}/download
POST /api/admin/backups/restore
```

Restore should require additional safeguards:

- admin authorization
- confirmation token or explicit parameter
- app maintenance mode if needed
- automatic backup before restore

---

## 11. Authentication and Authorization

### 11.1 Security Model

| Route Area | Access Model |
|---|---|
| `/api/public/*` | Anonymous read-only |
| `/api/admin/*` | Authenticated admin |
| `/api/publish/*` | Publisher or admin |
| `/api/integrations/*` | Admin or service token |
| `/api/health` | Anonymous shallow health |
| `/api/admin/health/deep` | Admin only |

### 11.2 Recommended First Version

Use:

- Azure App Service Authentication for browser/admin access
- ASP.NET Core policy-based authorization inside the app
- service tokens only for narrow automation scenarios

### 11.3 Authorization Policies

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });

    options.AddPolicy("Publisher", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin", "Publisher");
    });

    options.AddPolicy("ServiceOrAdmin", policy =>
    {
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") ||
            context.User.HasClaim("scope", "makeboldspark.publish"));
    });
});
```

### 11.4 API Key Guidance

API keys may be acceptable for service-to-service calls such as GitHub Actions publishing triggers.

Do not use API keys in browser clients.

Recommended header:

```text
X-MakeBoldSpark-Key: <secret>
```

Limit service-token access to narrow routes.

### 11.5 CORS

Allow only known static client origins.

Example:

```text
https://markhazleton.com
https://promptspark.markhazleton.com
https://github-stats-spark.example.com
https://admin.example.com
```

Do not use wide-open CORS for authenticated routes.

---

## 12. Static Publishing Strategy

### 12.1 Operating Model

```text
CMS/Admin edits content
  → Data saved in SQLite
  → Publish action generates versioned JSON
  → Static Web Apps consume generated JSON
  → Browser caches content aggressively
```

### 12.2 Generated Files

```text
/data/manifest.json
/data/articles.v{version}.json
/data/systems.v{version}.json
/data/prompts.v{version}.json
/data/navigation.v{version}.json
/data/search-index.v{version}.json
```

### 12.3 Manifest Example

```json
{
  "version": "2026.05.06.001",
  "lastPublishedUtc": "2026-05-06T21:30:00Z",
  "files": [
    "/data/articles.v2026.05.06.001.json",
    "/data/systems.v2026.05.06.001.json",
    "/data/navigation.v2026.05.06.001.json",
    "/data/search-index.v2026.05.06.001.json"
  ]
}
```

### 12.4 Client Cache Strategy

Recommended public site behavior:

```text
1. Load cached content from IndexedDB or Cache API
2. Render immediately
3. Fetch lightweight manifest
4. If version changed, download new JSON files
5. Update client cache
6. Re-render
7. If API/static content unavailable, continue using cached version
```

### 12.5 Cache Headers

```text
/data/manifest.json
  Cache-Control: no-cache or short max-age

/data/articles.v2026.05.06.001.json
  Cache-Control: public, max-age=31536000, immutable
```

### 12.6 Publishing Targets

Option A: Commit generated JSON to a static site repo.

```text
CMS Publish
  → Generate JSON
  → Commit to GitHub repo
  → GitHub Action builds/deploys Static Web App
```

Option B: Upload generated JSON to Blob Storage.

```text
CMS Publish
  → Generate JSON
  → Upload to Blob Storage/CDN
  → Static site reads versioned JSON files
```

Recommended first version: **GitHub commit-based publishing**, because GitHub remains an audit trail.

---

## 13. CMS Capability Roadmap

### Phase 1 CMS Features

- Login-protected admin shell
- Article/page list
- Create/edit article metadata
- Create/edit tags
- Create/edit systems/projects
- Save draft
- Publish action generates JSON
- Manual backup action

### Phase 2 CMS Features

- Markdown editor
- Preview mode
- Search index generation
- Media metadata management
- Publish history
- GitHub commit integration
- Restore from backup

### Phase 3 CMS Features

- Workflow states: Draft, Review, Published, Archived
- Content validation rules
- Broken link checks
- SEO metadata validation
- AI-assisted editing and summarization
- Cross-site publishing
- Scheduled publishing

---

## 14. GitHub Actions

### 14.1 Build/Test Workflow

```yaml
name: Build and Test

on:
  pull_request:
    branches: [ main ]
  push:
    branches: [ main ]

jobs:
  build-test:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Test
        run: dotnet test --configuration Release --no-build
```

### 14.2 Deploy Workflow

```yaml
name: Deploy MakeBoldSpark

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Test
        run: dotnet test --configuration Release --no-build

      - name: Publish
        run: dotnet publish src/MakeBoldSpark.Api/MakeBoldSpark.Api.csproj --configuration Release --output ./publish

      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: makeboldspark
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish
```

### 14.3 Deployment Rules

- Deploy code only.
- Do not deploy production `.db` files.
- Do not overwrite `/home/data`.
- Run migrations carefully.
- Back up before schema migrations once content matters.
- Keep deployment secrets in GitHub Actions secrets.

---

## 15. Configuration Strategy

### 15.1 Local Development

`appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./data/makeboldspark.local.db"
  },
  "Database": {
    "ApplyMigrationsOnStartup": true,
    "SeedOnStartup": true
  },
  "Cosmos": {
    "Enabled": false
  }
}
```

### 15.2 Azure App Service

App settings:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Data Source=/home/data/makeboldspark.db
Database__ApplyMigrationsOnStartup=true
Database__SeedOnStartup=false
Cosmos__Enabled=false
AllowedOrigins__0=https://markhazleton.com
AllowedOrigins__1=https://promptspark.markhazleton.com
```

### 15.3 Secrets

Do not commit:

- Cosmos keys
- GitHub tokens
- API keys
- connection strings containing secrets
- publishing tokens
- admin service keys

Use:

- Azure App Service configuration
- GitHub Actions secrets
- Key Vault later if needed

---

## 16. Observability

### 16.1 Logging

Use structured logging.

Minimum log fields:

- request path
- method
- status code
- duration
- correlation id
- user id or auth subject when available
- feature name
- operation name
- success/failure

### 16.2 Health Checks

Public shallow health:

```text
GET /api/health
```

Returns:

```json
{
  "status": "Healthy",
  "service": "MakeBoldSpark",
  "version": "0.1.0"
}
```

Admin deep health:

```text
GET /api/admin/health/deep
```

Checks:

- SQLite availability
- backup path availability
- Blob Storage connectivity if configured
- Cosmos connectivity if enabled
- GitHub publishing configuration if enabled

### 16.3 Application Insights

Use Application Insights selectively. Avoid excessive telemetry volume for personal projects.

Recommended:

- request summaries
- exceptions
- dependency failures
- publishing events
- backup events

Avoid:

- high-volume debug logs
- content payload logging
- secrets or tokens in telemetry

---

## 17. Development Phases

## Phase 0: Repository Foundation

**Goal:** Create a clean, buildable .NET 10 solution with documentation and CI.

Deliverables:

- GitHub repo initialized
- `.gitignore`
- `README.md`
- solution file
- API project
- test project
- docs folder
- initial ADRs
- build/test workflow

Acceptance criteria:

- `dotnet build` succeeds
- `dotnet test` succeeds
- README clearly explains MakeBoldSpark purpose
- GitHub Action validates pull requests

---

## Phase 1: API Skeleton

**Goal:** Establish route groups, Swagger, health checks, logging, and configuration.

Deliverables:

- ASP.NET Core API project
- OpenAPI/Swagger in development
- `/api/health`
- route group structure
- CORS configuration
- options binding
- structured logging

Acceptance criteria:

- API runs locally
- Swagger is available locally
- `/api/health` returns healthy response
- public/admin/publish route groups are present

---

## Phase 2: SQLite Foundation

**Goal:** Add EF Core + SQLite as the primary data layer.

Deliverables:

- `MakeBoldSparkDbContext`
- SQLite connection string configuration
- initial migration
- local database creation
- production database path support
- seed data structure

Acceptance criteria:

- database is created locally
- migrations run successfully
- app can run against SQLite
- no `.db` files are accidentally deployed as production state

---

## Phase 3: Public Content Feature

**Goal:** Implement the first SQLite-backed public feature.

Deliverables:

- Article/content domain model
- content repository
- public content endpoints
- seed article data
- tests

Example endpoints:

```text
GET /api/public/content/articles
GET /api/public/content/articles/{slug}
GET /api/public/content/tags
```

Acceptance criteria:

- seeded articles can be retrieved
- public endpoints are anonymous
- tests cover repository and endpoints

---

## Phase 4: Admin CMS Foundation

**Goal:** Add authenticated admin content management APIs.

Deliverables:

- admin route group
- authorization policies
- create/update article endpoints
- draft/published state
- admin list/detail endpoints

Example endpoints:

```text
GET    /api/admin/content/articles
POST   /api/admin/content/articles
PUT    /api/admin/content/articles/{id}
DELETE /api/admin/content/articles/{id}
```

Acceptance criteria:

- admin routes require authorization
- content can be created/edited
- public routes expose only published content

---

## Phase 5: Static JSON Export

**Goal:** Generate static artifacts for Azure Static Web Apps.

Deliverables:

- export service
- manifest generation
- article JSON generation
- navigation JSON generation
- search index JSON generation
- publish endpoint

Example endpoint:

```text
POST /api/publish/static-json
```

Acceptance criteria:

- versioned JSON files are generated
- manifest references generated files
- output can be consumed by a static client

---

## Phase 6: Backup and Restore

**Goal:** Protect SQLite runtime data.

Deliverables:

- backup service using SQLite backup behavior
- backup admin endpoints
- Blob Storage upload option
- manual download support
- backup documentation

Acceptance criteria:

- admin can create a backup
- backup can be downloaded or uploaded to Blob Storage
- backup process does not corrupt active database

---

## Phase 7: Azure Deployment

**Goal:** Deploy MakeBoldSpark to Azure App Service Linux.

Deliverables:

- Azure App Service Linux resource
- app settings configured
- GitHub deploy workflow
- persistent database path configured
- CORS origins configured
- custom domain optional

Acceptance criteria:

- deployment from GitHub works
- database persists across deployments
- public endpoints work from deployed URL
- admin endpoints are protected

---

## Phase 8: Cosmos Demo Feature

**Goal:** Add one optional Cosmos-backed feature to demonstrate skillset.

Deliverables:

- Cosmos configuration
- optional feature registration
- Cosmos repository abstraction
- sample document model
- public read endpoint or admin-only demo endpoint

Example endpoint:

```text
GET /api/public/cosmos-demo/items
```

Acceptance criteria:

- feature is disabled when Cosmos is not configured
- app still runs with SQLite only
- Cosmos feature is documented as selective, not default

---

## Phase 9: Static Client Integration

**Goal:** Connect one Azure Static Web App client to MakeBoldSpark output.

Deliverables:

- sample static client or integration guide
- manifest loading logic
- IndexedDB or Cache API strategy
- fallback behavior
- CORS validation

Acceptance criteria:

- static client loads generated JSON
- client caches content
- client updates only when manifest version changes

---

## 18. AI Coding Agent Guide

This section is written specifically for AI coding agents working in the MakeBoldSpark repo.

### 18.1 Agent Operating Rules

1. Do not create microservices.
2. Do not create separate repos for each API.
3. Do not force Cosmos DB as the default database.
4. Do not store production SQLite files inside the deployable app folder.
5. Do not commit secrets.
6. Do not add unnecessary dependencies.
7. Prefer feature folders and route groups.
8. Keep public routes read-only unless explicitly instructed.
9. Protect admin, publishing, backup, and integration routes.
10. Add or update tests for meaningful behavior changes.
11. Update documentation when architecture changes.
12. Preserve .NET 10 LTS as the target unless explicitly changed.

### 18.2 Before Making Changes

An AI coding agent should inspect:

- current solution structure
- existing README
- docs/decisions
- existing route groups
- existing data configuration
- existing tests
- GitHub workflows

Then produce a short implementation plan before editing.

### 18.3 Feature Implementation Pattern

When adding a feature, use this pattern:

```text
Features/{FeatureName}/
  {FeatureName}Endpoints.cs
  {FeatureName}Service.cs
  {FeatureName}Models.cs
  {FeatureName}Mapping.cs, if needed

Domain/{FeatureName}/
  domain entities/value objects

Data/Repositories/
  repository implementation if persistence is needed

Tests/
  endpoint tests
  service tests
  repository tests where practical
```

### 18.4 Endpoint Pattern

```csharp
public static class ContentEndpoints
{
    public static RouteGroupBuilder MapContentApi(this RouteGroupBuilder group)
    {
        group.MapGet("/content/articles", GetArticles)
            .WithName("GetPublishedArticles")
            .WithOpenApi();

        group.MapGet("/content/articles/{slug}", GetArticleBySlug)
            .WithName("GetPublishedArticleBySlug")
            .WithOpenApi();

        return group;
    }
}
```

### 18.5 Data Access Pattern

Use repository interfaces for domain-specific access.

Do not inject `DbContext` directly into endpoint methods unless the feature is intentionally trivial.

Preferred:

```text
Endpoint → Service → Repository → DbContext
```

Acceptable for very small read-only demos:

```text
Endpoint → Query Service → DbContext
```

### 18.6 Testing Expectations

For each new feature:

- test route registration where practical
- test service behavior
- test repository behavior with temporary SQLite database
- test authorization boundary for admin routes
- test public route returns only published/safe data

### 18.7 Documentation Expectations

Update:

- README if feature changes repository purpose or setup
- architecture docs if pattern changes
- ADR if a meaningful architecture decision is introduced
- deployment docs if configuration changes

---

## 19. Initial Agent Prompt

Use this prompt with GitHub Copilot, Codex, or another AI coding agent when starting MakeBoldSpark.

```markdown
You are working in the MakeBoldSpark repository.

MakeBoldSpark is a modular ASP.NET Core / .NET 10 LTS backend API platform for very small personal and portfolio APIs. It will be hosted on Azure App Service Linux B1 and consumed by Azure Static Web App clients.

Core architectural direction:

- Single backend API application
- Multiple feature-based route groups
- EF Core + SQLite as the default data model
- SQLite database stored in persistent App Service storage under `/home/data/makeboldspark.db`
- Cosmos DB used only selectively for document-shaped demo/features
- Public static clients consume `/api/public/*` endpoints or generated static JSON
- Admin/CMS/publishing routes require authorization
- Generated JSON can be published to GitHub/static site repos or Blob Storage
- Do not create microservices or split every API into separate repos

Please inspect the current repository and propose a short implementation plan.

Then implement the first foundation increment:

1. Ensure the repo targets .NET 10 LTS.
2. Create or update the ASP.NET Core API project.
3. Add route groups for `/api/public`, `/api/admin`, `/api/publish`, `/api/integrations`, and `/api/health`.
4. Add a public health endpoint.
5. Add OpenAPI/Swagger for local development.
6. Add EF Core + SQLite configuration.
7. Add a simple SQLite-backed content feature with:
   - Article entity/model
   - DbContext
   - Repository/service
   - `GET /api/public/content/articles`
   - `GET /api/public/content/articles/{slug}`
8. Add seed data for local development only.
9. Add authorization policy placeholders for admin and publishing routes.
10. Add basic tests.
11. Add GitHub Actions build/test workflow.
12. Add README and initial ADRs.

Constraints:

- Keep the implementation simple and readable.
- Do not add unnecessary packages.
- Do not commit production `.db` files.
- Do not store secrets in source control.
- Do not make Cosmos DB required.
- Document all required app settings.
```

---

## 20. README Starter Text

```markdown
# MakeBoldSpark

MakeBoldSpark is a modular ASP.NET Core backend API platform for small personal and portfolio APIs. It is designed to consolidate low-volume APIs into a single Azure-hosted backend while keeping public websites static-first and inexpensive.

## Goals

- Host multiple small APIs in one ASP.NET Core application
- Use EF Core + SQLite as the default low-cost relational backend
- Use Cosmos DB selectively for document-oriented examples
- Support Azure Static Web App clients
- Provide browser-based CMS/admin capabilities
- Generate static JSON for public content websites
- Keep hosting simple, portable, and inexpensive

## Target Runtime

- .NET 10 LTS
- Azure App Service Linux B1
- SQLite under persistent App Service storage
- Azure Static Web Apps for public clients

## Route Areas

- `/api/public/*` - anonymous read-only APIs
- `/api/admin/*` - authenticated CMS/admin APIs
- `/api/publish/*` - authenticated publishing/export APIs
- `/api/integrations/*` - authenticated integration APIs
- `/api/health` - health check
```

---

## 21. Architecture Decision Records

### ADR 0001: Single Backend API

```markdown
# ADR 0001: Single Backend API

## Status
Accepted

## Context
MakeBoldSpark hosts several very small personal/project APIs. The workloads are low-volume and share common concerns such as authentication, logging, data access, content publishing, and deployment.

## Decision
Use one modular ASP.NET Core backend API rather than separate services or repositories for each API.

## Consequences
This reduces hosting cost, deployment complexity, and repository sprawl. It increases coupling, so features must maintain clear route and code boundaries. APIs may be split later if scale, security, reliability, or release independence requires it.
```

### ADR 0002: SQLite Default With Selective Cosmos

```markdown
# ADR 0002: SQLite Default With Selective Cosmos

## Status
Accepted

## Context
The platform needs a low-cost relational data model for CMS/content features. The maintainer prefers EF Core and SQLite for portability and simplicity. Cosmos DB experience is also valuable but should not be forced where relational storage is better.

## Decision
Use EF Core + SQLite as the default persistence model. Use Cosmos DB selectively for document-shaped features or portfolio demonstrations.

## Consequences
The app remains inexpensive and portable. SQLite requires careful handling of persistence, backup, and single-instance hosting. Cosmos features must be optional and configuration-driven.
```

### ADR 0003: Static Web App Clients

```markdown
# ADR 0003: Static Web App Clients

## Status
Accepted

## Context
Most public websites are content-focused and do not require live database access on every page load.

## Decision
Use Azure Static Web Apps for public clients. Clients should consume generated static JSON by default and call live public APIs only when dynamic behavior is required.

## Consequences
Public sites remain fast, inexpensive, and resilient. The backend API becomes the authoring, publishing, integration, and preview layer rather than the runtime dependency for every page view.
```

### ADR 0004: .NET 10 LTS

```markdown
# ADR 0004: .NET 10 LTS

## Status
Accepted

## Context
MakeBoldSpark should use a stable long-term support framework suitable for production and portfolio use.

## Decision
Target .NET 10 LTS for all projects.

## Consequences
The repository has a stable support baseline and should avoid short-term framework churn. Dependencies should be compatible with .NET 10.
```

### ADR 0005: Authentication Boundaries

```markdown
# ADR 0005: Authentication Boundaries

## Status
Accepted

## Context
Public content clients need frictionless read access, while CMS/admin/publishing operations require protection.

## Decision
Expose public read-only endpoints anonymously. Require authorization for admin, publishing, backup, and integration endpoints. Use Azure App Service Authentication and ASP.NET Core policy-based authorization initially.

## Consequences
The system remains simple for public clients while protecting state-changing operations. Additional identity complexity can be added later if needed.
```

---

## 22. Backlog

### Foundation

- [ ] Create .NET 10 solution
- [ ] Add ASP.NET Core API project
- [ ] Add test project
- [ ] Add GitHub Actions build/test workflow
- [ ] Add README
- [ ] Add initial ADRs

### API Platform

- [ ] Add route groups
- [ ] Add health endpoint
- [ ] Add Swagger/OpenAPI
- [ ] Add CORS configuration
- [ ] Add structured logging
- [ ] Add exception handling middleware

### Data

- [ ] Add EF Core SQLite provider
- [ ] Add DbContext
- [ ] Add initial migration
- [ ] Add seed data pattern
- [ ] Add database path configuration
- [ ] Add migration-on-startup toggle

### Content Feature

- [ ] Add Article entity/model
- [ ] Add Tag model
- [ ] Add ContentRepository
- [ ] Add public content endpoints
- [ ] Add admin content endpoints
- [ ] Add tests

### Publishing

- [ ] Add static JSON export service
- [ ] Add manifest generation
- [ ] Add search index generation
- [ ] Add publish endpoint
- [ ] Add GitHub publishing option

### Security

- [ ] Add authorization policies
- [ ] Add admin route protection
- [ ] Add publisher route protection
- [ ] Add service-token support for integrations
- [ ] Document authentication setup

### Backups

- [ ] Add backup service
- [ ] Add admin backup endpoint
- [ ] Add Blob Storage upload option
- [ ] Add restore documentation

### Azure Deployment

- [ ] Create Azure App Service Linux resource
- [ ] Configure app settings
- [ ] Configure persistent DB path
- [ ] Deploy via GitHub Actions
- [ ] Validate persistence across deployments

### Cosmos Demo

- [ ] Add optional Cosmos configuration
- [ ] Add Cosmos demo model
- [ ] Add Cosmos repository
- [ ] Add disabled-by-default route group
- [ ] Add documentation

---

## 23. Definition of Done

A feature is done when:

- code builds locally
- tests pass
- route behavior is documented or discoverable through OpenAPI
- public/admin authorization boundary is correct
- configuration is documented
- no secrets are committed
- no production `.db` files are committed
- relevant docs/ADRs are updated
- deployment impact is understood

---

## 24. First Milestone Recommendation

The first milestone should be small and useful:

> A deployable .NET 10 ASP.NET Core API with route groups, health endpoint, Swagger, EF Core + SQLite, one public content feature, basic admin authorization placeholders, GitHub Actions build/test workflow, and clear documentation.

This milestone proves the architecture without overbuilding the CMS, Cosmos integration, or publishing pipeline too early.

---

## 25. Final Architecture Statement

MakeBoldSpark is a pragmatic backend platform for consolidating small APIs under one low-cost Azure-hosted ASP.NET Core application. It favors relational simplicity with EF Core and SQLite, supports selective Cosmos DB examples, protects admin and publishing workflows, and enables many Azure Static Web App clients to remain static-first, cache-heavy, inexpensive, and resilient.
