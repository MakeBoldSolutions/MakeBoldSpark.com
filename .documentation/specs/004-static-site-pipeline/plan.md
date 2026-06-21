# Implementation Plan: Static Content Build Pipeline

**Branch**: `004-static-site-pipeline` | **Date**: 2026-06-20 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `.documentation/specs/004-static-site-pipeline/spec.md`

## Rationale Summary

### Core Problem

Adding a new article or system page today means hand-copying an existing HTML file and manually keeping duplicated boilerplate in sync — a real consistency bug (a stale GitHub URL, a removed documentation route) already resulted from this. See spec.md for full rationale; this plan covers the **HOW**.

### Decision Summary

Add `src/MakeBoldSpark.Web/`, an Eleventy-based content build that authors articles as Markdown + front matter and System metadata as a hand-maintained JSON data file, generating all of `wwwroot/insights/**`, `wwwroot/systems/**`, and `wwwroot/assets/makebold/catalog.json` on every build. The hosting application's runtime is untouched.

### Key Drivers

- Eliminate hand-copied HTML as the only way to add content (spec FR-001–FR-003).
- Make the hand-maintained/generated/dynamic ownership boundary explicit and discoverable (spec FR-006).
- Guarantee generated output can never silently go stale relative to authored content at publish time (spec FR-010).

### Source Inputs

- `spec.md` — User Stories 1–3, FR-001 through FR-011, Clarifications session 2026-06-20
- `research.md` — Phase 0 decisions (this plan's technical foundation)
- Existing `src/MakeBoldSpark.Api/wwwroot/assets/makebold/catalog.json`, `site.js`, and the 11 files migrated into `wwwroot/{insights,systems}/**` earlier this session — read directly to confirm the front-matter field set and URL convention already in production use
- `.documentation/memory/constitution.md` — Principles I–X, Platform Architecture > Static-First Client Model

### Tradeoffs Considered

- Option A — Keep `catalog.json` as the hand-maintained source of truth inside `wwwroot`, with Eleventy reading it cross-project: rejected (research.md Topic 3) — blurs generated-vs-hand-maintained boundary that FR-006 requires to be explicit.
- Option B — Keep `site.js`'s client-side rendering for systems/insights pages and have Eleventy generate only static shells: rejected (research.md Topic 5) — defeats FR-002's "complete, navigable page" requirement and keeps a live dependency on the very file format being retired.
- Selected — Eleventy fully bakes systems/insights pages at build time; `systems.json` (new, hand-maintained, lives in `MakeBoldSpark.Web`) is the System source of truth; `catalog.json` and `site.js`'s render functions are retired as generated/dead respectively.

### Architectural Impact

- New project `src/MakeBoldSpark.Web/` introduced — a Node/Eleventy content-authoring tool, not a runtime component of the hosting application. Not added to `MakeBoldSpark.slnx` (it is not a .NET project).
- `wwwroot/assets/makebold/catalog.json` changes from hand-maintained to fully build-generated; `wwwroot/assets/makebold/site.js` is deleted (dead after migration).
- `MakeBoldSpark.Api.csproj` gains one new MSBuild `Target` (`BeforeTargets="Build"`) that shells out to `npm` — the only change to the existing .NET project.
- No new runtime routes, services, dependencies, or database changes (FR-008).

### Reviewer Guidance

Focus review on: (1) that the MSBuild build hook fails loudly (non-zero exit) rather than silently skipping when Node/npm is missing, (2) that a deliberately failed build leaves the complete generated output set unchanged, (3) that both FR-005 failure modes are asserted with fixtures, and (4) that migrated article/system pages match a preserved pre-build baseline.

## Summary

Add a new, non-.NET project (`src/MakeBoldSpark.Web/`) containing an Eleventy site that reads Markdown content files and a hand-maintained `systems.json` data file, and writes generated HTML plus a derived `catalog.json` directly into `src/MakeBoldSpark.Api/wwwroot`. The build runs into a temporary directory and only swaps it into place after a successful build, guaranteeing both no orphaned pages and no live-output outage from a failed build. The existing hand-authored article and system pages are migrated into this model as the first real content, retiring `site.js`'s client-side rendering functions and the old hand-maintained `catalog.json` in the same change. Build-time freshness is guaranteed by an MSBuild target that runs the Eleventy build before every API `dotnet build`.

## Technical Context

**Language/Version**: Node.js (LTS, currently v25.4.0 on this workstation) + Eleventy's native JS config — no TypeScript, no transpilation step
**Primary Dependencies**: `@11ty/eleventy` (only npm dependency; ships markdown-it + front-matter parsing + dev server built in)
**Storage**: N/A — file-based content (`.md` files + one `systems.json`), no database
**Testing**: A `npm test` script runs only against an isolated output root before migration: (a) it runs a full Eleventy build and asserts a zero exit code, (b) separately runs unknown-system and duplicate-output fixture builds and asserts a non-zero exit code for each (validates both FR-005 failure modes), and (c) hashes the complete isolated generated output set (`insights/`, `systems/`, and `assets/makebold/catalog.json`) before a deliberately failed build and asserts it is byte-for-byte unchanged afterward (validates build-then-swap). `npm run build` remains the only production-output command and is not run until the baseline is preserved and the full migration is ready. The migration parity check compares generated pages with a version-controlled pre-build baseline. No new .NET tests are required since no .NET code changes beyond the MSBuild target.
**Target Platform**: Build-time only — runs on the maintainer's workstation and, via the MSBuild hook, wherever `dotnet build` runs (currently a Windows workstation; no CI runner yet)
**Project Type**: Static site generator / content build tool (not a service, not a library consumed at runtime)
**Performance Goals**: Full clean-then-rebuild completes within the SC-003 5-second local-preview budget at current and near-term content volume (single digit systems, low double-digit articles)
**Constraints**: Must not introduce a runtime dependency for `MakeBoldSpark.Api` (FR-008); must not be added to `MakeBoldSpark.slnx`; every `dotnet build` for `MakeBoldSpark.Api` requires the pinned Node/npm toolchain and rebuilds the static content; Node version is pinned via `.nvmrc` + `package.json` `engines` (closes `critic-007`); the `BeforeTargets="Build"` hook is validated only against the current manual Windows-workstation-to-VM-zip publish flow and MUST be re-verified before any future CI or Azure App Service Kudu/Oryx build-from-source deploy path is introduced (closes `critic-004`)
**Scale/Scope**: Solo maintainer, low-volume content; this plan covers the build pipeline plus migrating the 11 already-existing hand-authored pages — it does not cover authoring large volumes of new content

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Status |
|-----------|-------|--------|
| I. API-First | No API surface is added or changed by this feature; N/A | ✅ PASS (N/A) |
| II. Test-First | `npm test` (build succeeds + fail-loud fixture) is written alongside the Eleventy config, before content migration is considered done | ✅ PASS |
| III. Simplicity | Exactly one new dependency (`@11ty/eleventy`); no bundler, no CSS preprocessor, no JS framework; clean-then-rebuild chosen over incremental diffing specifically because it's simpler and sufficient at this scale | ✅ PASS |
| IV. Security by Default | No new input surface, no auth/authz surface; build runs locally/at publish-time only, never accepts untrusted input | ✅ PASS |
| V. Spec-Driven | spec.md → clarify → plan (this document); required gates (`checklist`, `analyze`, `critic`) tracked in `gates/` | ✅ PASS |
| VI. Ownership Boundary | All new artifacts live under `.documentation/specs/004-static-site-pipeline/` and `src/MakeBoldSpark.Web/`; `.devspark/` is untouched | ✅ PASS |
| VII. Single Backend Platform | Not implicated — `MakeBoldSpark.Web` is a build-time content tool, not a deployed service; there is still exactly one running backend (`MakeBoldSpark.Api`). No microservice or per-API split is introduced. | ✅ PASS (N/A) |
| VIII. Clear Authorization Boundaries | Not implicated — no routes added or changed | ✅ PASS (N/A) |
| IX. Relational-First Data Strategy | Not implicated — no database changes; content is file-based by design (static site) | ✅ PASS (N/A) |
| X. Zero Secrets in Source Control | No secrets in `systems.json`, Eleventy config, or `package.json`; nothing in this feature touches credentials | ✅ PASS |

**Post-Phase-1 Re-check**: No violations introduced by the data model (Content Item / System / Generated Page) or by the front-matter contract — all principles continue to hold.

## Project Structure

### Documentation (this feature)

```text
.documentation/specs/004-static-site-pipeline/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md         # Phase 1 output
├── quickstart.md         # Phase 1 output
├── contracts/             # Phase 1 output — front-matter schema + CLI contract
│   └── content-frontmatter.md
├── gates/                 # Persisted gate artifacts from analyze/critic/checklist
└── tasks.md               # Phase 2 output (/devspark.tasks command)
```

### Source Code (repository root)

```text
src/
  MakeBoldSpark.Web/                       (NEW — not part of MakeBoldSpark.slnx)
    package.json                           (single dependency: @11ty/eleventy; "engines": { "node": ">=20" })
    package-lock.json                       (committed — required by `npm ci` in the API build hook)
    .nvmrc                                  (pins the Node major version; critic-007)
    .gitignore                              (excludes node_modules/ and generated test/output/ only)
    eleventy.config.js                     (input/output dirs, permalinks, validation hook, Nunjucks as template language — research.md Topic 9)
    scripts/
      build-and-swap.mjs                    (builds into a temp dir; only on a successful build does it replace the complete generated output set — wwwroot/insights/, wwwroot/systems/, and wwwroot/assets/makebold/catalog.json — while failures preserve the prior set; accepts an isolated test output root so pre-migration validation cannot replace hand-authored pages)
      verify-build.mjs                     (npm test: success, unknown-system failure, duplicate-output failure, and failed-build safe-swap assertions)
    src/
      _data/
        systems.json                      (hand-maintained System source of truth — moved from wwwroot/assets/makebold/catalog.json)
      _includes/
        layouts/
          base.njk                        (nav + footer + brand markup shared by every generated page)
          system.njk                       (system detail page layout)
          article.njk                      (article/insight page layout)
      content/
        articles/
          spec-driven-development-harness.md   (migrated from insights/devspark/spec-driven-development-harness/index.html)
      systems.njk                          (generates /systems/{id}/ for every entry in systems.json)
      systems-index.njk                     (generates /systems/index.html)
      insights-index.njk                    (generates /insights/index.html)
      insights-by-system.njk                (generates /insights/{system}/index.html per system)
      catalog.njk                           (generates assets/makebold/catalog.json from systems.json + the article collection)
    test/
      fixtures/
        unknown-system-reference.md         (used by verify-build.mjs to assert an unresolved reference fails loudly)
        duplicate-output/                   (two content fixtures intentionally resolving to the same output address)
      baselines/
        pre-migration/                      (version-controlled copy of every hand-authored page, captured before the first migration build; used for SC-002 parity comparison)
      output/                               (ignored isolated output root used by `npm test` and `npm run build:test` before migration)

  MakeBoldSpark.Api/
    MakeBoldSpark.Api.csproj                (MODIFIED — new BeforeTargets="Build" MSBuild Target)
    wwwroot/
      index.html, vision.html, ecosystem.html, subsites.json   (UNCHANGED — hand-maintained, outside Eleventy's output)
      assets/makebold/
        brand.css, logos/, fonts/                              (UNCHANGED)
        catalog.json                                            (CHANGED — now build-generated, not hand-edited)
        site.js                                                 (DELETED — render functions retired per research.md Topic 5)
      insights/                                                 (REGENERATED on every successful build; left untouched on a failed build)
      systems/                                                  (REGENERATED on every successful build; left untouched on a failed build)
```

**Structure Decision**: Single new top-level source-code-sibling project (`src/MakeBoldSpark.Web/`) alongside the existing three .NET projects, intentionally **not** added to `MakeBoldSpark.slnx` since it contains no .NET code and isn't built by `dotnet build`. The only change inside an existing .NET project is the one MSBuild `Target` added to `MakeBoldSpark.Api.csproj`. This keeps Principle VII intact (still one backend platform) while giving the content tooling its own clearly-bounded home next to the project whose `wwwroot` it populates.

## Complexity Tracking

*No Constitution Check violations — this section is intentionally empty.*

## Implementation Notes

- **2026-06-20 (T031):** By explicit user direction, the MSBuild hook runs before every `dotnet build` rather than only before publish. This accepts the Node/npm install-and-build cost on routine API builds in exchange for continuously fresh generated output.
