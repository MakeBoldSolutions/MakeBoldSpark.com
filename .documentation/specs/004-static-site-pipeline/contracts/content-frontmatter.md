# Contract: Content Authoring Interface

**Phase**: 1 — Design & Contracts
**Branch**: `004-static-site-pipeline`

This is the interface a content owner uses to add or change content. There is no programmatic API in this feature (no HTTP endpoints, no library exports) — the contract is the file format and the CLI commands.

## 1. Adding a new article

Create `src/MakeBoldSpark.Web/src/content/articles/{slug}.md`:

```markdown
---
title: "Article Title"
summary: "One or two sentence summary used in listing cards."
system: devspark
category: architecture
tags: ["ai-assisted-development", "architecture"]
published: 2026-06-20
featured: false
---

Article body in standard Markdown.
```

**Guarantees** (enforced by the build, not by convention):

- If `system` does not match an `id` in `src/_data/systems.json`, the build fails with a non-zero exit and an error naming the offending file and the unknown id (FR-005, research.md Topic 7).
- The file's name (without extension) becomes the slug; it is never read from front matter.
- Removing or renaming this file and rebuilding removes the previously-generated page for it (FR-011).

## 2. Adding or changing a System

Edit `src/MakeBoldSpark.Web/src/_data/systems.json` directly — this file is hand-maintained, not generated. See `data-model.md` for the full required field set. A duplicate `id` fails the build (Eleventy's native duplicate-output-path error).

## 3. CLI commands (run from `src/MakeBoldSpark.Web/`)

| Command | Effect |
|---|---|
| `npm install` | One-time setup; installs `@11ty/eleventy` |
| `npm run build` | Builds to a temporary directory, then replaces `wwwroot/insights/**`, `wwwroot/systems/**`, and `wwwroot/assets/makebold/catalog.json` only after success; a failed build leaves all three targets unchanged |
| `npm run serve` | Same build, plus a local dev server with live reload (FR-007 / SC-003) |
| `npm test` | Asserts a normal build succeeds; unknown-system and duplicate-output fixtures fail; and a failed build preserves the complete generated output set — regression test for FR-005 and safe delivery |

## 4. What this contract does NOT cover

- Editing `index.html`, `vision.html`, `ecosystem.html`, `subsites.json`, or anything under `assets/makebold/` except `catalog.json` — these stay hand-maintained exactly as today, outside this build's ownership (FR-004).
- Any change to `MakeBoldSpark.Api`'s routes, services, or runtime behavior (FR-008).
