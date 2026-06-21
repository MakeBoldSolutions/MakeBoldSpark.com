# Quickstart: MakeBoldSpark Local Developer Setup

**Target**: Developer with .NET 10 SDK installed  
**Goal**: Running API with seeded data in under 10 minutes  
**Branch**: `001-makeboldspark-foundation`

---

## Prerequisites

| Tool | Version | Check |
|------|---------|-------|
| .NET SDK | 10.x LTS | `dotnet --version` |
| Git | Any recent | `git --version` |

No Azure account, no Cosmos connection, no Docker required for local development.

---

## 1. Clone and Build

```bash
git clone https://github.com/MarkHazleton/MakeBoldSpark.git
cd MakeBoldSpark
dotnet restore
dotnet build --configuration Debug
```

Expected: build completes with zero errors, zero warnings.

---

## 2. Run the API

```bash
dotnet run --project src/MakeBoldSpark.Api/MakeBoldSpark.Api.csproj
```

On first start, the API will automatically:

1. Create `./data/makeboldspark.local.db` (SQLite file, relative to project output)
2. Apply EF Core migrations
3. Seed sample articles and tags (only if the Articles table is empty)

Look for log output confirming:
```
[INFO] Database migrations applied.
[INFO] Seed data loaded: 2 articles, 3 tags.
[INFO] Now listening on: http://localhost:5000
```

---

## 3. Verify the Health Endpoint

```bash
curl http://localhost:5000/api/health
```

Expected response:
```json
{
  "status": "Healthy",
  "service": "MakeBoldSpark",
  "version": "0.1.0"
}
```

---

## 4. Browse Public Content

List all published articles:
```bash
curl http://localhost:5000/api/public/content/articles
```

Get a specific article:
```bash
curl http://localhost:5000/api/public/content/articles/hello-world
```

List tags:
```bash
curl http://localhost:5000/api/public/content/tags
```

---

## 5. Browse API Documentation (API Test Spark)

Open in browser: `http://localhost:5000/api-test-spark/`

API Test Spark is the embedded interactive test UI used by this project (a Swagger UI alternative).
All registered endpoints appear with interactive try-it-out, schema browsing, and curl command generation.
The raw OpenAPI 3.1 JSON spec is available at `http://localhost:5000/openapi/v1.json`.

Both endpoints are available in all environments for this project.

---

## 6. Verify Admin Route Protection

The admin route group is protected. Unauthenticated callers receive `401 Unauthorized`:

```bash
curl -i http://localhost:5000/api/admin/health/deep
```

Expected: `HTTP/1.1 401 Unauthorized`

---

## 7. Run Tests

```bash
dotnet test --configuration Debug --logger "console;verbosity=normal"
```

Expected: all tests pass. Test output includes coverage for:
- Health endpoint returns 200 with correct body
- Published articles are returned; draft articles are excluded
- Admin routes return 401 for unauthenticated callers
- Slug lookup returns 404 for missing or draft slugs

---

## Configuration Reference

### appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./data/makeboldspark.local.db"
  },
  "Database": {
    "ApplyMigrationsOnStartup": true,
    "SeedOnStartup": true
  },
  "AllowedOrigins": [
    "http://localhost:5173",
    "http://localhost:3000"
  ],
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### What NOT to configure locally

- No Cosmos DB connection string needed (Cosmos disabled by default)
- No Azure App Service settings needed
- No identity provider credentials needed (admin routes return 401 without auth middleware in Phase 0)

---

## File Layout After First Run

```text
MakeBoldSpark/
  data/
    makeboldspark.local.db     ← created automatically, .gitignored
  src/MakeBoldSpark.Api/
    ...
```

The `data/` directory is in `.gitignore`. Do not commit the `.db` file.

---

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| `Cannot open database` | `./data/` directory missing | The app creates it; ensure write permission on the project directory |
| `Table already exists` error | Migrations ran against a pre-existing schema | Delete `./data/makeboldspark.local.db` and restart |
| API Test Spark UI not visible (`/api-test-spark/` returns 404) | App not started or wrong port | Check console output for the listening URL |
| All endpoints return 404 | Project not started or wrong port | Check console output for the listening URL |
| Admin routes return 200 (unexpected) | Auth middleware not configured | Verify `AuthorizationSetup.cs` is registered in `Program.cs` |

---

## Azure App Service Settings Reference

For production deployment, configure these Azure App Service application settings:

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Data Source=/home/data/makeboldspark.db
Database__ApplyMigrationsOnStartup=true
Database__SeedOnStartup=false
AllowedOrigins__0=https://markhazleton.com
AllowedOrigins__1=https://promptspark.markhazleton.com
```

Secrets (Cosmos keys, GitHub tokens, publish tokens) go into App Service settings, never in source control.
