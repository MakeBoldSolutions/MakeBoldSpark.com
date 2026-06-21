# Data Model: Static Content Build Pipeline

**Phase**: 1 — Design & Contracts
**Branch**: `004-static-site-pipeline`
**Date**: 2026-06-20

## Content Item

A single authored article, read from one Markdown file under `src/MakeBoldSpark.Web/src/content/articles/*.md`.

| Field | Type | Required | Notes |
|---|---|---|---|
| `title` | string | yes | Page `<title>` and `<h1>` |
| `summary` | string | yes | Used in listing cards and `<meta description>`/og tags |
| `system` | string | yes | Must match an `id` in the System collection (validated at build start — research.md Topic 7) |
| `category` | string | yes | Free-text grouping label (e.g. `architecture`) |
| `tags` | string[] | yes | May be empty array |
| `published` | date (`YYYY-MM-DD`) | yes | Drives sort order and the date shown on the page |
| `updated` | date (`YYYY-MM-DD`) | no | Defaults to `published` if omitted |
| `featured` | boolean | no | Defaults to `false` |
| body | Markdown | yes | The article content |

**Identity / slug**: the filename (without `.md`) is the slug. Two files with the same name in the same directory cannot coexist on a filesystem, so filename collisions are structurally impossible; cross-system slug collisions are not possible either, since the output path is `/insights/{system}/{slug}/` and `system` is part of the path.

**Lifecycle**: none. Per Clarification 1, there is no draft/published status field — every file present at build time is included in the next build's output (FR-001).

**Derived address**: `/insights/{system}/{slug}/index.html` (Clarification 3).

## System

A structured entry describing one of the ecosystem's systems, read from `src/MakeBoldSpark.Web/src/_data/systems.json` (an array, same shape as today's `catalog.json.systems[]`).

| Field | Type | Required | Notes |
|---|---|---|---|
| `id` | string | yes | Stable identifier; referenced by Content Items' `system` field and used in generated paths |
| `name` | string | yes | Display name |
| `status` | string | yes | e.g. `active`, `evolving` |
| `category` | string | yes | e.g. `AI-assisted development` |
| `role` | string | yes | e.g. `Working reference system` |
| `purpose` | string | yes | One-sentence purpose statement |
| `summary` | string | yes | Card/listing summary |
| `valueProposition` | string | yes | |
| `githubUrl` | string (URL) | yes | |
| `capabilities` | string[] | yes | |
| `philosophy` | string | yes | |
| `whatItIs` | string | yes | |
| `whyItExists` | string | yes | |

**Identity**: `id` is unique within `systems.json`; uniqueness is enforced the same way Eleventy enforces unique output paths (research.md Topic 7) — a duplicate `id` would produce a duplicate `/systems/{id}/` output path and fail the build natively.

**Lifecycle**: none — hand-edited directly in source control, not authored as Markdown (per spec Assumption, confirmed in research.md Topic 3).

**Derived address**: `/systems/{id}/index.html` — always computed from `id` at build time (in templates and in any "Docs" link), never stored as a separate hand-maintained field. An earlier draft of this schema included a `docsUrl` field meant to hold this same address; it was removed after `/devspark.analyze` (finding `INC-001`) flagged it as a redundant, driftable duplicate of the computed value.

## Generated Page

Not an authored entity — the build-time output produced from a Content Item, a System, or a listing computed across either collection. Generated Pages are never hand-edited; every build fully regenerates them (research.md Topic 4).

| Generated Page | Source | Output path |
|---|---|---|
| System detail | One System entry | `/systems/{id}/index.html` |
| Systems index | All System entries | `/systems/index.html` |
| Article detail | One Content Item | `/insights/{system}/{slug}/index.html` |
| Insights index | All Content Items | `/insights/index.html` |
| Per-system insights listing | Content Items filtered by `system` | `/insights/{system}/index.html` |
| Derived catalog data | All System entries + all Content Items | `/assets/makebold/catalog.json` |

## Relationships

```text
System (1) ──── (0..N) Content Item        [Content Item.system → System.id]
System (1) ──── (1) Generated Page (system detail)
Content Item (1) ──── (1) Generated Page (article detail)
(All Systems) ──── (1) Generated Page (systems index)
(All Content Items) ──── (1) Generated Page (insights index)
(System, filtered Content Items) ──── (1) Generated Page (per-system insights listing)
```

No relationship is optional in the direction that matters for validation: every Content Item MUST resolve to exactly one System (FR-005); a System MAY have zero Content Items (the "Articles coming soon" / "Insights are being prepared" empty-state copy already established in the pre-migration hand-authored pages is preserved in the `article.njk`/listing templates for this case).
