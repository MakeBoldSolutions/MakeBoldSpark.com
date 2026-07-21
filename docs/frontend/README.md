# MakeBoldSpark Frontend Developer Guides

This folder contains integration guides for frontend developers (and AI coding agents) building
clients that consume the MakeBoldSpark backend.

These guides go beyond the raw OpenAPI spec. They document real JSON shapes, gotchas, auth
patterns, TypeScript types, and working `fetch` examples derived directly from the production
source code.

## Base URL

| Environment | URL |
|---|---|
| Production | `https://api.markhazleton.com` |
| Local dev | `http://localhost:5000` (or whatever port `launchSettings.json` assigns) |
| API explorer | `https://api.markhazleton.com/api-test-spark/` (all environments) |

## CORS

Public (`/api/public/*`) routes are callable cross-origin from any allowed origin.
Allowed origins are configured in Azure App Service application settings (`AllowedOrigins__0`,
`AllowedOrigins__1`, …). In local dev, `http://localhost:5173` and `http://localhost:3000`
are pre-allowed.

If you are adding a new static site origin, request that `AllowedOrigins__N` be added to the
Azure App Service settings — source code does not need to change.

## Authentication

Protected routes require a JWT Bearer token:

```text
Authorization: Bearer <your-jwt-token>
```

| Route group | Policy | Required role/claim |
|---|---|---|
| `/api/public/*` | Anonymous | None |
| `/api/admin/*` | AdminOnly | `Admin` role |
| `/api/publish/*` | Publisher | `Admin` OR `Publisher` role |
| `/api/integrations/*` | ServiceOrAdmin | `Admin` role OR `scope: makeboldspark.publish` claim |

**401** — missing or invalid token.  
**403** — valid token but wrong role.

## Guides

| Guide | APIs covered |
|---|---|
| [recipe-api-guide.md](recipe-api-guide.md) | `/api/public/recipes`, `/api/publish/recipes` |
| [makeboldspark-api-guide.md](makeboldspark-api-guide.md) | `/api/public/makeboldspark/*`, `/api/admin/makeboldspark/*` |

## Static-First Client Pattern

MakeBoldSpark is designed for static-site consumption. For high-traffic read paths, prefer
pre-generating JSON artifacts (`/data/{collection}.v{version}.json`) served from Azure Static
Web Apps with long cache TTLs. Use live API calls only for:

- Data that changes more than once per deploy (user-specific, real-time)
- Write operations (publish, admin)
- Low-traffic pages where pre-generation isn't worth the complexity

A versioned manifest pattern is used when static generation is in play:

```text
GET /data/manifest.json          → { "recipesVersion": 42 }   (short TTL)
GET /data/recipes.v42.json       → [ ... ]                     (long TTL, immutable)
```

This guide covers the **live API** calls only. Static artifact generation is a separate concern.
