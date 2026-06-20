# MakeBoldSpark

**Live Site**: [makeboldspark.com](https://makeboldspark.com)

MakeBoldSpark is a modular ASP.NET Core backend API platform that consolidates personal and portfolio APIs into a single Azure-hosted backend. It is the API home for Make Bold Solutions and Mark Hazleton projects, keeping public websites static-first while centralizing all API services.

## About

MakeBoldSpark demonstrates how to host multiple low-volume APIs in a single, cost-effective ASP.NET Core application backed by EF Core and SQLite. The platform serves as the API layer for static sites and SPAs in the [MakeBoldSpark](https://makeboldspark.com) portfolio.

> Built by [Mark Hazleton](https://markhazleton.com) — Technical Solutions Architect
> Part of the [MakeBoldSpark](https://makeboldspark.com) portfolio of technical demonstrations.

## Goals

- Host multiple small APIs in one ASP.NET Core application
- Use EF Core + SQLite as the default low-cost relational backend
- Use Cosmos DB selectively for document-oriented examples
- Support Azure Static Web Apps clients
- Provide browser-based CMS/admin capabilities
- Generate static JSON for public content websites
- Keep hosting simple, portable, and inexpensive

## Target Runtime

- .NET 10 LTS
- Azure App Service Linux B1
- SQLite under persistent App Service storage (`/home/data/makeboldspark.db`)
- Azure Static Web Apps for public clients

## Route Areas

- `/api/public/*` — anonymous read-only APIs
- `/api/admin/*` — authenticated CMS/admin APIs (requires Admin role)
- `/api/publish/*` — authenticated publishing/export APIs
- `/api/integrations/*` — authenticated integration APIs
- `/api/health` — shallow health check (anonymous)
- `/api/admin/health/deep` — deep health check (Admin only)

## Quick Start

```bash
git clone https://github.com/MarkHazleton/MakeBoldSpark.git
cd MakeBoldSpark
dotnet run --project src/MakeBoldSpark.Api
```

See [quickstart.md](.documentation/specs/001-makeboldspark-foundation/quickstart.md) for full local setup instructions.

## Architecture

See [docs/decisions/](docs/decisions/) for Architecture Decision Records.

## Constitution

See [.documentation/memory/constitution.md](.documentation/memory/constitution.md) for non-negotiable project principles.
