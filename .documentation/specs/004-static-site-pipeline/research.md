# Research: Static Content Build Pipeline

**Phase**: 0 — Outline & Research
**Branch**: `004-static-site-pipeline`
**Date**: 2026-06-20
**Status**: Complete — all NEEDS CLARIFICATION resolved

---

## Research Topics

## 1. Static Site Generator Choice

**Decision**: Eleventy (`@11ty/eleventy`), confirmed by the user before `/devspark.specify` ran.

**Rationale**: Eleventy ships with markdown-it and front-matter parsing built in — no extra templating framework, no JS bundler, no CSS preprocessor needed. It is unopinionated about output structure, which matters here because output must land in exact, pre-existing paths inside `src/MakeBoldSpark.Api/wwwroot` (not a generic `_site/` folder). Its built-in dev server (`eleventy --serve`) covers the FR-007 fast-preview requirement with zero extra tooling.

**Alternatives considered**:
- Hugo — single static binary, but Go template syntax is a bigger departure from the existing hand-authored HTML structure; rejected to minimize the learning/maintenance surface for a solo maintainer.
- Astro — component-island architecture is more powerful than this site needs; would pull in a heavier build/runtime model than the constitution's Simplicity principle justifies for mostly-static marketing/article pages.
- A hand-rolled Node script (markdown-it + gray-matter + custom templating) — rejected; would require re-implementing what Eleventy already provides (collections, pagination-free listing pages, dev server, file watching) for no real benefit at this scale.

## 2. Dependency Footprint

**Decision**: Exactly one npm dependency: `@11ty/eleventy`. No CSS preprocessor, no JS bundler, no UI framework runtime.

**Rationale**: Eleventy's default template engine for `.md` files is markdown-it with front-matter support built in — both ship inside the `@11ty/eleventy` package already. Adding either separately would be an unjustified dependency under the constitution's Simplicity principle ("no unnecessary packages or dependencies MUST be added without justification").

**Alternatives considered**: Adding `markdown-it` and `gray-matter` directly — rejected as redundant; Eleventy already vendors and exposes both.

## 3. Output Ownership Boundary (the spec's deferred catalog.json decision)

**Decision**: Split ownership into three tiers, each documented explicitly in a new `src/MakeBoldSpark.Api/wwwroot/README.md`:

| Tier | Paths | Who owns it |
|---|---|---|
| Hand-maintained, static | `index.html`, `vision.html`, `ecosystem.html`, `subsites.json`, `assets/makebold/brand.css`, `assets/makebold/logos/*`, `assets/makebold/fonts/*` | Edited directly; Eleventy never touches these. |
| Build-generated | `insights/**`, `systems/**`, `assets/makebold/catalog.json` | Produced entirely by the Eleventy build; never hand-edited. Overwritten on every build. |
| Dynamic at request time | `ecosystem.html`'s client-side fetch of `subsites.json` | Unaffected by this feature (Out of Scope) — continues to render in the browser exactly as today. |

The System catalog's **source of truth** moves from `wwwroot/assets/makebold/catalog.json` to a new hand-maintained data file inside the authoring project: `src/MakeBoldSpark.Web/src/_data/systems.json` (same shape as today's `catalog.json.systems` array). `wwwroot/assets/makebold/catalog.json` becomes a **build output** — Eleventy regenerates it on every build from `systems.json` plus the computed article collection, so any consumer that still fetches `/assets/makebold/catalog.json` (there are none after migration, see Topic 5) keeps working without a contract change.

**Rationale**: This directly resolves the spec's Assumption ("System catalog stays hand-maintained as structured data, not Markdown") while also resolving FR-006's ownership-documentation requirement and removing the only file that was previously both hand-maintained *and* sitting inside generated output's directory tree — a prior source of exactly the kind of drift this feature exists to eliminate.

**Alternatives considered**:
- Leave `catalog.json` in place inside `wwwroot` as the hand-maintained source, with Eleventy reading it cross-project: rejected — a build tool writing derived output back into the same file it also reads as a source invites accidental data loss and makes "what's generated vs. hand-maintained" ambiguous, the opposite of what FR-006 requires.
- Move System data into Markdown front matter too (one `.md` "stub" file per system): rejected per the spec's own Assumption — systems are few, structured, and not prose articles; Markdown adds no value for them.

## 4. Orphaned Output Removal (FR-011)

**Decision**: Before every build, delete the entire `wwwroot/insights/` and `wwwroot/systems/` directory trees, then let Eleventy regenerate them fully from current content. No incremental diffing.

**Rationale**: Eleventy does not delete pre-existing output files that a given build run no longer produces (it only ever adds/overwrites). At this site's scale (a handful of systems, low double-digit articles expected), a full clean-then-rebuild is well under the SC-003 5-second preview budget and is trivially correct — there is no stale-file bookkeeping to get wrong. This is implemented as a `prebuild` npm script (`rimraf` is unnecessary; Node's built-in `fs.rmSync(path, { recursive: true, force: true })` via a tiny script is sufficient and avoids adding a second dependency).

**Alternatives considered**: Eleventy's `--incremental` flag — rejected for this use case; it optimizes rebuild speed during `--watch`/`--serve` sessions but does not solve cross-build orphan cleanup, which is what FR-011 actually requires.

## 5. Replacing Client-Side Rendering for Systems/Insights Pages

**Decision**: Eleventy generates fully-baked HTML for `/systems/`, `/systems/{id}/`, `/insights/`, `/insights/{system}/`, and `/insights/{system}/{slug}/` — no page in these trees ships an empty `<main>` waiting on a browser-side `fetch("/assets/makebold/catalog.json")`. `assets/makebold/site.js` (the file exposing `renderSystems()`, `renderSystemPage()`, `renderInsightsIndex()`, `renderSystemInsights()`) is deleted as part of the FR-009 migration; its `<script src="/assets/makebold/site.js">` tags are removed from every migrated page.

**Rationale**: FR-002 requires the build to generate a "complete, navigable page" — not a shell that depends on a runtime fetch succeeding. Keeping both a build-time path and a runtime-fetch path for the same content would reintroduce exactly the dual-maintenance/drift risk this feature exists to remove (per the Core Problem statement), and `assets/makebold/catalog.json` becomes generated output (Topic 3), so nothing should still be fetching it client-side for these pages.

**Alternatives considered**: Keep `site.js` and only have Eleventy generate the static shell (nav/hero/footer) while content stays runtime-rendered — rejected; defeats the purpose of FR-002 and keeps a live dependency on `catalog.json`'s shape that the new pipeline is supposed to retire.

**Scope confirmation**: `ecosystem.html`'s own inline script and its fetch of `subsites.json` is a separate, already-out-of-scope code path (per spec) and is untouched by this decision.

## 6. Build-Trigger Mechanism Relative to `dotnet build`/`publish`

**Decision**: Two complementary triggers, both documented in `quickstart.md`:
1. **Manual, for authoring**: `npm run build` (or `npm run serve` for live preview) from `src/MakeBoldSpark.Web`, run by the content owner whenever content changes — this is the FR-010 "documented, repeatable way."
2. **Automatic, for publish safety**: an MSBuild `Target` in `src/MakeBoldSpark.Api/MakeBoldSpark.Api.csproj` with `BeforeTargets="Publish"` that runs `npm ci && npm run build` in `src/MakeBoldSpark.Web`, failing the publish loudly (non-zero exit) if Node/npm is unavailable or the Eleventy build fails — so a maintainer can never publish a build with stale generated content (closing the loop on FR-010's "never stale ... at publish time").

The target is scoped to `BeforeTargets="Publish"` only, **not** `Build` or `Test` — routine `dotnet build`/`dotnet test` cycles during day-to-day .NET development stay fast and fully decoupled from Node tooling. Node only becomes a required tool at publish time, which is also the only time it's truly needed.

**Rationale**: No GitHub Actions workflow exists yet in this repository (deployment today is the manual `dotnet publish` + zip-artifact flow established earlier this session for the Windows VM target), so a CI-level trigger isn't available without first standing up CI/CD — explicitly Out of Scope for this spec. An MSBuild publish-hook gives the same "can't forget" guarantee a CI gate would, without requiring new CI infrastructure.

**Alternatives considered**:
- Manual-only (no MSBuild hook): rejected — leaves FR-010's "never stale at publish time" guarantee resting entirely on the maintainer's memory, which is the exact failure mode (forgotten manual step → drift) this feature exists to eliminate.
- Hook into `BeforeTargets="Build"` instead of `Publish`: rejected — would force Node/npm to run on every local `dotnet build`/`dotnet test`, slowing the inner dev loop for changes that have nothing to do with content.
- Standing up a new GitHub Actions workflow as part of this feature: rejected — explicitly Out of Scope per spec; a larger, separate undertaking.

## 7. Validation / Fail-Loud Behavior (FR-005)

**Decision**:
- **Unknown system reference**: a small Eleventy `eleventy.before` (or per-collection computed-data check) validates every Content Item's `system` field against the `systems.json` collection at build start and throws an `Error` (non-zero exit, build fails) listing the offending file and the unknown system id if any reference doesn't resolve.
- **Duplicate output address**: handled natively by Eleventy — when two templates compute the same output path, Eleventy's own build fails with a "duplicate output file" error. No custom code needed.

**Rationale**: Matches the spec's edge-case requirement that mistakes fail loudly rather than silently producing a broken link or overwriting a page, using the least custom code possible (one validation hook; everything else is Eleventy's existing behavior).

**Alternatives considered**: JSON Schema validation of front matter via `ajv` — rejected as an unjustified extra dependency; the single system-reference check needed is simple enough to write as ~10 lines of plain JS in the Eleventy config file.

## 8. Front Matter Schema

**Decision**: Confirmed field set per the spec's Key Entities section — `title`, `summary`, `system` (id, references `systems.json`), `category`, `tags` (array), `published` (date), `updated` (optional date), `featured` (optional boolean, default `false`). The page's URL slug is derived from the content file's name (kebab-case filename = slug), matching the one real existing article's already-established naming.

**Rationale**: Reuses the field names already established in the current `catalog.json.articles[]` shape (read during the duplicate-fix work earlier this session), so no renaming/migration mapping is needed for the one real existing article.

**Alternatives considered**: An explicit `slug` front-matter field instead of filename-derived: rejected as redundant — the filename already uniquely identifies the file in its directory; requiring both invites them to drift apart.
