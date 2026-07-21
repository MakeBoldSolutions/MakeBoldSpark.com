---
classification: retroactive-spec
risk_level: low
target_workflow: specify-retroactive
required_artifacts: spec, contracts
recommended_next_step: pr-review
required_gates: none
---

# Feature Specification: MakeBoldSpark API

**Status**: Complete <!-- Valid: Draft | In Progress | Complete -->
**Branch**: main (retroactive — implemented before spec)
**Spec Date**: 2026-05-10
**Author**: Mark Hazleton
**Retroactive**: true — this spec documents an already-implemented feature

## Problem Statement

MakeBoldSpark needs to expose content management data from the existing `MakeBoldSpark.Core` domain
library so that static client sites can consume CMS content anonymously, and authenticated
administrators can manage all CMS entities (domains, blogs, authors, posts, categories,
menus, keywords, content-parts, subscribers, newsletters, mail-settings) through a unified
API without requiring a separate CMS backend.

## User Stories

| ID | As a… | I want to… | So that… |
|----|-------|-----------|----------|
| US-1 | Anonymous visitor | List and read domains, blogs, authors, posts, categories, menus, keywords, content-parts | Static sites can consume CMS data |
| US-2 | Admin | Create, update, and delete all CMS entities | Content stays current |
| US-3 | Admin | Manage subscribers and newsletters | Email marketing is manageable via API |
| US-4 | Admin | Read and update mail settings | Email configuration is centralized |

## Functional Requirements

### Public (Anonymous) Endpoints

| ID | Requirement |
|----|------------|
| FR-001 | `GET /api/public/makeboldspark/domains` — list all domains |
| FR-002 | `GET /api/public/makeboldspark/domains/{id}` — get domain by id or 404 |
| FR-003 | `GET /api/public/makeboldspark/blogs` — list all blogs |
| FR-004 | `GET /api/public/makeboldspark/blogs/{id}` — get blog by id or 404 |
| FR-005 | `GET /api/public/makeboldspark/authors` — list all authors |
| FR-006 | `GET /api/public/makeboldspark/authors/{id}` — get author by id or 404 |
| FR-007 | `GET /api/public/makeboldspark/posts[?blogId=N]` — list posts, optional blog filter |
| FR-008 | `GET /api/public/makeboldspark/posts/{id}` — get post by id or 404 |
| FR-009 | `GET /api/public/makeboldspark/categories` — list all categories |
| FR-010 | `GET /api/public/makeboldspark/categories/{id}` — get category by id or 404 |
| FR-011 | `GET /api/public/makeboldspark/menus[?domainId=N]` — list menus, optional domain filter |
| FR-012 | `GET /api/public/makeboldspark/menus/{id}` — get menu by id or 404 |
| FR-013 | `GET /api/public/makeboldspark/keywords` — list all keywords |
| FR-014 | `GET /api/public/makeboldspark/keywords/{id}` — get keyword by id or 404 |
| FR-015 | `GET /api/public/makeboldspark/content-parts` — list all content parts |
| FR-016 | `GET /api/public/makeboldspark/content-parts/{id}` — get content part by id or 404 |

### Admin (Authenticated) Endpoints

| ID | Requirement |
|----|------------|
| FR-017 | Full CRUD (POST/PUT/DELETE) for domains, blogs, authors, posts, categories, menus, keywords, content-parts |
| FR-018 | Full CRUD for subscribers (GET list, GET by id, POST, PUT, DELETE) |
| FR-019 | Newsletters: GET list, GET by id, POST, DELETE |
| FR-020 | Mail-settings: GET list, GET by id, POST, PUT, DELETE |
| FR-021 | All admin routes MUST require the `AdminOnly` authorization policy |

## Non-Functional Requirements

- Data source: `MakeBoldSpark.Core` domain library via `MakeBoldSparkCoreDbContext` (EF Core + SQLite)
- Persistence: `MakeBoldSparkConnection` SQLite database (follows Principle IX)
- Authorization: inherits from `/api/admin` and `/api/public` route group policies (Principle VIII)
- No hardcoded credentials or connection strings (Principle X)

## Authorization Mapping

| Route Prefix | Policy | Constitution Category |
|---|---|---|
| `/api/public/makeboldspark/*` | Anonymous | Public read-only |
| `/api/admin/makeboldspark/*` | AdminOnly | Authenticated admin only |

## Out of Scope

- Full-text search across CMS content (future)
- Bulk import/export (future)
- Audit logging of CMS changes (tracked via `RequestLoggingMiddleware`)

## Implementation Notes

- `MakeBoldSparkService` injects `MakeBoldSparkCoreDbContext` directly (acceptable — admin-only mutation surface is explicit)
- Route group `MapPublicMakeBoldSparkApi` mounted on `/api/public/makeboldspark`
- Route group `MapAdminMakeBoldSparkApi` mounted on `/api/admin/makeboldspark` (inherits AdminOnly policy)
- Feature folder: `src/MakeBoldSpark.Api/Features/MakeBoldSpark/`
- `PendingModelChangesWarning` suppressed — MakeBoldSpark.Core schema managed by its own migrations
