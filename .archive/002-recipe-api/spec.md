---
classification: retroactive-spec
risk_level: low
target_workflow: specify-retroactive
required_artifacts: spec, contracts
recommended_next_step: pr-review
required_gates: none
---

# Feature Specification: Recipe API

**Status**: Complete <!-- Valid: Draft | In Progress | Complete -->
**Branch**: main (retroactive — implemented before spec)
**Spec Date**: 2026-05-10
**Author**: Mark Hazleton
**Retroactive**: true — this spec documents an already-implemented feature

## Problem Statement

MakeBoldSpark needs to expose recipe data from the existing `MakeBoldSpark.Recipe` domain library so
that static client sites can display recipe content anonymously, and authorized publishers
can manage recipe and category records through a versioned API.

## User Stories

| ID | As a… | I want to… | So that… |
|----|-------|-----------|----------|
| US-1 | Anonymous visitor | List all approved recipes | I can browse the recipe catalog |
| US-2 | Anonymous visitor | Get a recipe by ID | I can view full recipe details |
| US-3 | Anonymous visitor | List recipe categories | I can filter recipes by category |
| US-4 | Publisher | Create a new recipe | New recipes appear in the catalog |
| US-5 | Publisher | Update an existing recipe | Content stays current |
| US-6 | Publisher | Delete a recipe | Stale recipes are removed |
| US-7 | Publisher | Manage recipe categories | Category taxonomy stays organized |

## Functional Requirements

| ID | Requirement |
|----|------------|
| FR-001 | `GET /api/public/recipes` returns a JSON array of all approved recipes |
| FR-002 | `GET /api/public/recipes/{id}` returns a single recipe or 404 |
| FR-003 | `GET /api/public/recipes/categories` returns all recipe categories |
| FR-004 | `POST /api/publish/recipes` creates a recipe (Publisher role required) |
| FR-005 | `PUT /api/publish/recipes/{id}` updates a recipe (Publisher role required) |
| FR-006 | `DELETE /api/publish/recipes/{id}` deletes a recipe (Publisher role required) |
| FR-007 | `POST /api/publish/recipes/categories` creates a category (Publisher role required) |
| FR-008 | `PUT /api/publish/recipes/categories/{id}` updates a category (Publisher role required) |
| FR-009 | `DELETE /api/publish/recipes/categories/{id}` deletes a category (Publisher role required) |
| FR-010 | Public routes MUST allow anonymous access (`AllowAnonymous`) |
| FR-011 | Publish routes MUST require the `Publisher` authorization policy |

## Non-Functional Requirements

- Data source: `MakeBoldSpark.Recipe` domain library via `RecipeDbContext` (EF Core + SQLite)
- Persistence: `RecipeConnection` SQLite database (follows Principle IX)
- Authorization: inherits from `/api/publish` route group policy (Constitution Principle VIII)
- No hardcoded connection strings or credentials (Constitution Principle X)

## Authorization Mapping

| Route Prefix | Policy | Constitution Category |
|---|---|---|
| `/api/public/recipes/*` | Anonymous | Public read-only |
| `/api/publish/recipes/*` | Publisher | Publisher or admin |

## Out of Scope

- Recipe search / filtering (future enhancement)
- Recipe image upload (served from existing `MakeBoldSpark.Recipe` storage)
- Rate limiting (platform-level concern)

## Implementation Notes

- `RecipeService` wraps `IRecipeService` (from `MakeBoldSpark.Recipe`) — no direct DbContext injection
- Route group `MapPublicRecipeApi` mounted on `/api/public`
- Route group `MapPublishRecipeApi` mounted on `/api/publish` (inherits Publisher policy)
- Feature folder: `src/MakeBoldSpark.Api/Features/Recipe/`
