# Data Model: MakeBoldSpark Platform Foundation

**Phase**: 1 — Design & Contracts
**Branch**: `001-makeboldspark-foundation`
**Date**: 2026-05-07
**Prerequisite**: research.md complete ✅

---

## Entities

### Article

Represents a published or draft content piece. The primary entity for the public content feature.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `int` | PK, auto-increment | EF Core convention |
| `Slug` | `string` | Required, unique, max 200 chars | URL-safe identifier; uniqueness enforced at DB and service layer |
| `Title` | `string` | Required, max 300 chars | Display title |
| `Summary` | `string` | Required, max 1000 chars | Short description; returned in list responses; full body excluded from list |
| `Body` | `string` | Required | Full Markdown content; returned only in detail (`/articles/{slug}`) response |
| `PublishDate` | `DateTimeOffset?` | Nullable | Null = never published; set when status transitions to Published |
| `Status` | `ArticleStatus` | Required, enum | `Draft` (0) or `Published` (1); only Published articles appear on public endpoints |
| `CreatedAt` | `DateTimeOffset` | Required, set on insert | UTC timestamp |
| `UpdatedAt` | `DateTimeOffset` | Required, updated on save | UTC timestamp |
| `Tags` | `ICollection<Tag>` | Navigation | Many-to-many via `ArticleTag` join table |

**State transitions**:
```text
Draft ──→ Published   (publish action sets PublishDate = UtcNow)
Published ──→ Draft   (unpublish action; PublishDate retained for audit)
```

**Validation rules**:
- Slug must match `^[a-z0-9]+(?:-[a-z0-9]+)*$` (lowercase alphanumeric, hyphen-separated)
- Slug must be unique across all articles (enforced by unique index)
- Title and Summary must not be empty or whitespace
- Body must not be empty
- If Status is Published, PublishDate must be non-null

---

### Tag

Represents a content categorization label. Associated with zero or more articles.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `int` | PK, auto-increment | EF Core convention |
| `Name` | `string` | Required, unique, max 100 chars | Display name and lookup key; uniqueness enforced at DB level |
| `Articles` | `ICollection<Article>` | Navigation | Many-to-many via `ArticleTag` join table |

**Validation rules**:
- Name must not be empty or whitespace
- Name must be unique across all tags

---

### ArticleTag (join table)

Explicit join entity for the Article ↔ Tag many-to-many relationship. EF Core will manage this as a shadow join table unless admin CRUD requires explicit navigation — at foundation phase it is implicitly managed.

| Field | Type | Constraints |
|-------|------|-------------|
| `ArticleId` | `int` | FK → Article.Id, PK component |
| `TagId` | `int` | FK → Tag.Id, PK component |

---

## Enumerations

```csharp
public enum ArticleStatus
{
    Draft = 0,
    Published = 1
}
```

---

## EF Core Configuration

### DbContext

`MakeBoldSparkDbContext` inherits `DbContext`. Registered as scoped service.

```csharp
public class MakeBoldSparkDbContext : DbContext
{
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Tag> Tags => Set<Tag>();
}
```

### Key indexes

```csharp
// In OnModelCreating:
modelBuilder.Entity<Article>()
    .HasIndex(a => a.Slug)
    .IsUnique();

modelBuilder.Entity<Tag>()
    .HasIndex(t => t.Name)
    .IsUnique();
```

### SQLite connection string

Injected via `IConfiguration["ConnectionStrings:DefaultConnection"]`. Never hardcoded.

---

## Projected Response Models (API-facing)

These are the read models returned by endpoints — distinct from the EF entities to decouple persistence from contracts.

### HealthResponse

```csharp
public record HealthResponse(string Status, string Service, string Version);
```

### ArticleSummary (list response)

```csharp
public record ArticleSummary(
    string Slug,
    string Title,
    string Summary,
    DateTimeOffset? PublishDate,
    IReadOnlyList<string> Tags
);
```

Note: `Body` is deliberately excluded from the list response (spec FR-018).

### ArticleDetail (detail response)

```csharp
public record ArticleDetail(
    string Slug,
    string Title,
    string Summary,
    string Body,
    DateTimeOffset? PublishDate,
    IReadOnlyList<string> Tags
);
```

### TagResponse

```csharp
public record TagResponse(string Name);
```

---

## Seed Data Structure

Seed data is applied at startup only when the Articles table is empty (guard logic per spec FR-015). Seed source: inline C# seed class (or optionally JSON file in `data/seed/`).

**Sample seed articles** (minimum 2):

| Slug | Title | Status | Tags |
|------|-------|--------|------|
| `hello-world` | Hello World | Published | `general`, `intro` |
| `getting-started-with-makeboldspark` | Getting Started with MakeBoldSpark | Published | `makeboldspark`, `tutorial` |
| `draft-article` | Draft Article | Draft | `general` |

The draft article must not appear in public endpoint responses, verifying the status filter works.

---

## Repository Interface

```csharp
public interface IContentRepository
{
    Task<IReadOnlyList<ArticleSummary>> GetPublishedArticlesAsync(CancellationToken ct = default);
    Task<ArticleDetail?> GetPublishedArticleBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<TagResponse>> GetAllTagsAsync(CancellationToken ct = default);
}
```

Implementation: `ContentRepository` using EF Core + SQLite queries. Draft articles are filtered at query level (`WHERE Status = Published`), not in application code.

---

## Migration Strategy

1. Initial migration (`InitialCreate`) — creates `Articles`, `Tags`, `ArticleTag` tables with all indexes.
2. Future migrations added incrementally; production SQLite backed up before any schema migration.

---

## Constraints Summary

- No Cosmos DB entities in this phase
- No admin write models in this phase (admin CRUD is Phase 4 of Jumpstart Guide)
- No pagination (spec FR-018 explicitly excludes page/size parameters)
- Article body stored as raw Markdown string; rendering is client responsibility (spec FR-021)
- Slug uniqueness must be enforced at both the database index level and the service/repository layer
