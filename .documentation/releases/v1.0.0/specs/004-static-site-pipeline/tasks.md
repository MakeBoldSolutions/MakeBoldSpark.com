# Tasks: Static Content Build Pipeline

**Input**: Design documents from `.documentation/specs/004-static-site-pipeline/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/content-frontmatter.md, quickstart.md

**Tests**: Included — the spec's FR-005 (fail loudly on bad input) and the constitution's Test-First principle both require a verifiable test task; T011 proves both invalid-reference and duplicate-output failure modes and that a failed build preserves its output target.

**Organization**: Tasks are grouped by user story (US1, US2, US3 from spec.md) to enable independent implementation and testing of each.

**Revision note**: This is the remediated version of tasks.md, rewritten after `/devspark.critic` (`gates/critic.md`) and `/devspark.analyze` (`gates/analyze.md`) findings. Task numbering changed from the original 31-task version to 34 tasks; see the "Findings closed by this revision" table below for what moved.

## Rationale Summary

### Core Problem

Every new article or system page today requires hand-copying an existing HTML file and manually keeping duplicated boilerplate in sync — a real consistency bug already resulted from this once.

### Decision Summary

Stand up an Eleventy build (`src/MakeBoldSpark.Web/`) that generates `wwwroot/insights/**`, `wwwroot/systems/**`, and `wwwroot/assets/makebold/catalog.json` from Markdown content files and a hand-maintained `systems.json`, building into a temporary directory and swapping it into place only on success — then migrate the existing 11 hand-authored pages into it as the first real content.

### Key Drivers

- Eliminate hand-copied HTML as the only authoring path (FR-001–FR-003).
- Prove the migration is lossless before declaring the old authoring path retired (FR-009, SC-002).
- Guarantee publish-time freshness without relying on memory (FR-010), and without a failed build ever causing a live outage (critic-001).

### Reviewer Guidance

Focus on: the Foundational phase (T005–T012) being genuinely complete and *proven* — not just implemented — before any User Story task starts. T011 proves both FR-005 failure modes and the failed-build no-op guarantee immediately after T008–T010, rather than after unrelated template work; this closes `critic-001`, `critic-003`, and `critic-006` at the task-design level.

### Findings closed by this revision

| Finding | Source | What changed |
| --- | --- | --- |
| critic-001 (CRITICAL) | critic | T009/T010 implement build-to-temp-then-swap; T011 proves a deliberately failed build leaves the isolated output set byte-for-byte unchanged before user-story work starts, while T033 re-confirms this against the production target |
| critic-002 (HIGH) | critic | T002 commits package-lock.json explicitly; T012 exercises `npm ci` before it's relied on |
| critic-003 (HIGH) | critic | T011 asserts real process exit codes for both unknown-system and duplicate-output fixtures, not just console output |
| critic-004 (MEDIUM) | critic | Caveat moved to plan.md Constraints; T031 references it |
| critic-005 (HIGH) | critic | `risk_profile: internal` added to spec.md frontmatter |
| critic-006 (MEDIUM) | critic | T011 moved to immediately follow T008 (was the old T019, after 9 unrelated tasks) |
| critic-007 (MEDIUM) | critic | T002/T003 add `engines` field + `.nvmrc` |
| COV-001 (HIGH) | analyze | T025 added: verifies FR-004 (non-owned paths byte-for-byte unchanged) |
| RAT-001 (HIGH) | analyze | research.md Topic 9 added (Nunjucks decision); T007 cross-references it |
| INC-001 (MEDIUM) | analyze | `docsUrl` removed from data-model.md; T014/T020 compute the address instead |
| AMB-001 (MEDIUM) | analyze | T024 now specifies an automated, whitespace-normalized HTML diff |
| UND-001 (MEDIUM) | analyze | SC-004 reworded in spec.md to a one-time checkable outcome; T032 maps to it cleanly |

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

All new files live under `src/MakeBoldSpark.Web/` (not part of `MakeBoldSpark.slnx`). The only change to an existing .NET project is one MSBuild `Target` in `src/MakeBoldSpark.Api/MakeBoldSpark.Api.csproj` (T031). Generated output paths are under `src/MakeBoldSpark.Api/wwwroot/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create `src/MakeBoldSpark.Web/` directory tree per plan.md's Project Structure (`src/_data/`, `src/_includes/layouts/`, `src/content/articles/`, `scripts/`, `test/fixtures/`, `test/baselines/`)
- [X] T002 Initialize `src/MakeBoldSpark.Web/package.json` (single dependency `@11ty/eleventy`, `"engines": { "node": ">=20" }`); run `npm install`; **commit the resulting `package-lock.json`** (closes `critic-002`'s lockfile-discipline half)
- [X] T003 [P] Add `src/MakeBoldSpark.Web/.nvmrc` pinning the Node major version used in development (closes `critic-007`)
- [X] T004 [P] Add `src/MakeBoldSpark.Web/.gitignore` excluding generated `node_modules/`, `_site/`, and `test/output/` build roots — explicitly confirm `package-lock.json` and `test/baselines/` are **not** excluded

**Checkpoint**: Phase complete — 2026-06-21

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core build machinery — including the safety-critical swap and validation behavior — that MUST exist and be **proven** before any user story can be implemented or tested

**⚠️ CRITICAL**: No user story work can begin until this phase is complete, including T011's proof that the fail-loud guarantee actually works

- [X] T005 Create `src/MakeBoldSpark.Web/src/_data/systems.json`, seeded from the 4 entries currently in `src/MakeBoldSpark.Api/wwwroot/assets/makebold/catalog.json`'s `systems` array (devspark, apitestspark, webspark, makeboldspark) — carry forward the already-corrected `makeboldspark` entry; per data-model.md, do **not** include a `docsUrl` field (the address is always computed from `id`, not hand-maintained — closes `INC-001`)
- [X] T006 [P] Create `src/MakeBoldSpark.Web/src/_includes/layouts/base.njk` reproducing the existing nav/hero/footer markup and `brand.css` classes shared by every page (source from the current `wwwroot/systems/index.html` and `wwwroot/insights/index.html` markup)
- [X] T007 Implement `src/MakeBoldSpark.Web/eleventy.config.js`: set `dir.input`/`dir.output`; register `systems.json` as Eleventy global data; use Nunjucks as the template language (decision recorded in research.md Topic 9 — closes `RAT-001`)
- [X] T008 Implement the system-reference validation hook in `eleventy.config.js` (an `eleventy.before` check): every Content Item's `system` field must resolve to an `id` in `systems.json`, else throw and fail the build (FR-005, research.md Topic 7) — depends on T005, T007
- [X] T009 [P] Implement `src/MakeBoldSpark.Web/scripts/build-and-swap.mjs`: run the Eleventy build into a temporary output directory; only after a successful (exit `0`) build, replace the complete generated output set — `src/MakeBoldSpark.Api/wwwroot/insights/`, `src/MakeBoldSpark.Api/wwwroot/systems/`, and `src/MakeBoldSpark.Api/wwwroot/assets/makebold/catalog.json` — from that staged output. On build failure, leave every prior target unchanged; never copy `catalog.json` separately after the page trees. This preserves no-orphan behavior without allowing derived catalog data to drift from generated pages (FR-011; closes `critic-001` and `critic-008`). Support an explicit isolated output-root argument for test builds; it must never be the default production output path.
- [X] T010 Wire `build-and-swap.mjs` as the npm `build` script's only default-production step (no separate `prebuild`-delete step exists) and add `build:test`, which passes the isolated output root used before migration — depends on T009
- [X] T011 Create `test/fixtures/unknown-system-reference.md`, two colliding content fixtures under `test/fixtures/duplicate-output/`, and `scripts/verify-build.mjs`, wired as `npm test`. It must run only against the isolated output root and: (1) assert a normal build exits `0`; (2) assert the unknown-system fixture exits non-zero; (3) assert the duplicate-output fixtures exit non-zero; and (4) after a successful build, hash the complete generated output set — `insights/`, `systems/`, and `assets/makebold/catalog.json` — deliberately run a failing fixture build, then assert all three targets are byte-for-byte unchanged. This proves both FR-005 failure modes and build-and-swap's no-op guarantee without touching pre-migration `wwwroot` content — depends on T008, T009, T010
- [X] T012 Run `npm ci` locally at least once (not just `npm install`) and confirm it succeeds against the committed `package-lock.json`, since the API build hook (T031) relies on `npm ci` specifically (closes `critic-002`'s remaining half) — depends on T002

**Checkpoint**: Foundation ready, AND T011 has proven the two fail-loud cases and the safe-swap failure path — user story implementation can now begin.

**Checkpoint**: Phase complete — 2026-06-21

---

## Phase 3: User Story 1 - Author a new article without hand-writing HTML (Priority: P1) 🎯 MVP

**Goal**: A content owner writes one Markdown file with front matter and gets a complete, navigable, correctly-linked page — no other file hand-edited.

**Independent Test**: Author one new content file, run `npm run build`, confirm a new page exists at its system-scoped address and appears in the relevant listing(s).

### Implementation for User Story 1

- [X] T013 [US1] Create `src/MakeBoldSpark.Web/src/_includes/layouts/article.njk` (extends `base.njk`), matching the existing migrated article's structure (eyebrow/title/tags/body/footer-nav)
- [X] T014 [US1] Create `src/MakeBoldSpark.Web/src/_includes/layouts/system.njk` (extends `base.njk`); its "Docs" link computes `/systems/{{ system.id }}/` directly — no stored `docsUrl` field is read (consistent with T005/INC-001)
- [X] T015 [P] [US1] Implement `src/MakeBoldSpark.Web/src/systems.njk` (paginated over `systems.json`) generating `/systems/{id}/index.html` for every System — depends on T014
- [X] T016 [P] [US1] Implement `src/MakeBoldSpark.Web/src/systems-index.njk` generating `/systems/index.html` (depends on T014)
- [X] T017 [US1] Configure the `articles` collection in `eleventy.config.js` (front matter fields per data-model.md, permalink `/insights/{{ system }}/{{ slug }}/index.html`) and apply `article.njk` — depends on T013
- [X] T018 [P] [US1] Implement `src/MakeBoldSpark.Web/src/insights-index.njk` generating `/insights/index.html` across the full article collection (depends on T017)
- [X] T019 [P] [US1] Implement `src/MakeBoldSpark.Web/src/insights-by-system.njk` generating `/insights/{system}/index.html` per System, preserving the existing "Insights are being prepared" empty-state copy when a System has zero articles (depends on T017)
- [X] T020 [US1] Implement `src/MakeBoldSpark.Web/src/catalog.njk` generating `assets/makebold/catalog.json` from `systems.json` + the article collection — each emitted System entry's address is computed from `id`, no `docsUrl` passthrough (depends on T005, T017)
- [X] T021 [US1] Author one throwaway sample content file (`src/content/articles/_sample.md`), run `npm run build:test` against the isolated output root, confirm the generated page and its listing entries, then delete the sample and rebuild. Do not run the production `npm run build` until T022 has preserved the baseline and the full migration is ready; this proves the end-to-end flow without replacing hand-authored `wwwroot` pages.

**Checkpoint**: User Story 1 is fully functional and independently testable.

**Checkpoint**: Phase complete — 2026-06-21

---

## Phase 4: User Story 2 - Migrate existing hand-authored content into the new model (Priority: P1)

**Goal**: The 11 pages migrated into `wwwroot/{insights,systems}/**` earlier this session become the first real content in the new model, with no visible content loss, no reintroduced stale references, and no changes anywhere outside the generated paths.

**Independent Test**: Convert each existing page, rebuild, and confirm every generated page is equivalent to what's live today — and that everything outside the generated paths is untouched.

### Implementation for User Story 2

- [X] T022 [US2] Before any migration build can replace output, copy every current hand-authored page in `wwwroot/{insights,systems}/**` into version-controlled `src/MakeBoldSpark.Web/test/baselines/pre-migration/`, preserving the source-relative paths; then migrate the real article (`wwwroot/insights/devspark/spec-driven-development-harness/index.html`) into `src/content/articles/spec-driven-development-harness.md` with front matter matching its current title/summary/tags/published date — depends on T017
- [X] T023 [US2] Cross-check `systems.json` (T005) against the live `wwwroot/systems/{devspark,apitestspark,webspark,makeboldspark}/index.html` pages field-by-field; reconcile any gaps against data-model.md's System schema
- [X] T024 [US2] Run `npm run build` and compare every generated page against its matching pre-build, version-controlled file in `test/baselines/pre-migration/`. The attempted whitespace-normalized raw HTML diff is inapplicable because the baseline pages are JavaScript-rendered shells while the new pages are intentionally fully baked HTML. **Accepted validation:** the user manually verified the generated pages in Visual Studio and confirmed they look correct on 2026-06-20. This confirms SC-002 for this migration; retain the baseline for future content-level comparison — depends on T022, T023
- [X] T025 [US2] Verify FR-004 explicitly: hash or diff `index.html`, `vision.html`, `ecosystem.html`, `subsites.json`, and `assets/makebold/{brand.css,logos/,fonts/}` before and after the T024 build, and confirm zero changes outside `insights/`, `systems/`, and `catalog.json` (closes `COV-001` — FR-004 previously had no dedicated task) — depends on T024
- [X] T026 [US2] Delete the superseded hand-authored pages and `wwwroot/assets/makebold/site.js` (and any remaining `<script src="/assets/makebold/site.js">` references), since `catalog.json` is now build output and nothing fetches it client-side for these pages (research.md Topic 5) — depends on T024, T025
- [X] T027 [US2] Run `dotnet build` and `dotnet test` for `MakeBoldSpark.Api`/`MakeBoldSpark.Api.Tests` to confirm the .NET app is unaffected by the `wwwroot` content changes (FR-008) — depends on T024, T025, T026

**Checkpoint**: User Stories 1 AND 2 both work; the migration is proven lossless, and non-owned areas are proven untouched.

**Checkpoint**: Phase complete — 2026-06-21

---

## Phase 5: User Story 3 - Regenerate content locally with fast feedback (Priority: P2)

**Goal**: A content owner previews in-progress changes within a few seconds, without running the .NET app.

**Independent Test**: Edit a content file while `npm run serve` is running; confirm the preview updates within ~5 seconds (SC-003).

### Implementation for User Story 3

- [X] T028 [US3] Add a `serve` npm script (`eleventy --serve`) watching `src/content`, `src/_data`, and `src/_includes` (depends on Phase 2 completion)
- [X] T029 [US3] Verify live-reload latency against the SC-003 5-second budget by editing a sample article while `npm run serve` is running. Automated preview verification completed in 643 ms on 2026-06-20.

**Checkpoint**: All three user stories are independently functional.

**Checkpoint**: Phase complete — 2026-06-21

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Publish-time safety net and ownership-boundary documentation that span all stories

- [X] T030 [P] Finalize `package.json` scripts section (`build`, `build:test`, `serve`, `test`) consolidating T010/T011/T028
- [X] T031 Add a `BeforeTargets="Build"` MSBuild `Target` to `src/MakeBoldSpark.Api/MakeBoldSpark.Api.csproj` that runs `npm ci` then `npm run build` in `src/MakeBoldSpark.Web` on every API build, failing loudly (non-zero exit) if Node/npm is unavailable or the static build fails (FR-010, user-directed build integration); note in a code comment that this hook is validated only against the current manual Windows-workstation-to-VM-zip publish flow and must be re-verified before any future CI/Kudu-based deploy path (plan.md Constraints — closes `critic-004`)
- [X] T032 [P] Create `src/MakeBoldSpark.Api/wwwroot/README.md` documenting the three-tier ownership boundary (hand-maintained / build-generated / dynamic-at-request-time), explicitly enumerating every path in each tier (FR-006; this document's existence and accuracy is what SC-004 now checks — closes `UND-001`)
- [X] T033 Run every step in `quickstart.md` end-to-end (author an article, add a System, delete a content file and confirm orphan removal, run `dotnet publish` and confirm it triggers the Eleventy build, confirm a deliberately-broken build leaves the live `insights/`, `systems/`, and `assets/makebold/catalog.json` output untouched per T009)
- [X] T034 Re-run `dotnet build`/`dotnet test` one final time and confirm `git status` shows only the expected new/changed files before handing off to a fresh `/devspark.critic` and `/devspark.analyze` pass

**Checkpoint**: Phase complete — 2026-06-21

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories. Includes proving the fail-loud (T011) and safe-swap (T009) guarantees, not just implementing them.
- **User Story 1 (Phase 3)**: Depends on Foundational only
- **User Story 2 (Phase 4)**: Depends on Foundational AND on User Story 1's templates/collection config (T013, T017) — not independent of US1's implementation, though it is independently *testable* once US1 exists
- **User Story 3 (Phase 5)**: Depends on Foundational only; independent of US1/US2 content (just needs the build to exist)
- **Polish (Phase 6)**: Depends on all three user stories being complete

### Parallel Opportunities

- T003/T004 can run alongside T002
- T006 and T009 (Foundational) can run in parallel — different files; T011 follows T008–T010 because it exercises both behaviors
- T015/T016 (system pages) and T018/T019 (insight listings) can run in parallel once their respective dependencies (T014, T017) are done
- T030 and T032 (Polish) can run in parallel

---

## Parallel Example: Foundational Phase

```bash
Task: "Create src/MakeBoldSpark.Web/src/_includes/layouts/base.njk"
Task: "Implement src/MakeBoldSpark.Web/scripts/build-and-swap.mjs"
```

---

## Gate Acknowledgements

None recorded. This revision closes all 12 findings from the first `/devspark.critic` + `/devspark.analyze` pass (see "Findings closed by this revision" above); a fresh pass (T034) should confirm a clean result before `/devspark.implement`.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (Setup) + Phase 2 (Foundational) — including T011's proof of both FR-005 failure modes and the T009 safe-swap guarantee, not just writing them
2. Complete Phase 3 (User Story 1) — author and verify one sample article end-to-end
3. **STOP and VALIDATE**: confirm T021's sample-content flow and T011's fail-loud fixture both pass before touching any real content

### Incremental Delivery

1. Setup + Foundational → Foundation ready, safety guarantees proven
2. User Story 1 → sample content proves the pipeline works (MVP)
3. User Story 2 → real migration, parity-checked AND non-owned-area-checked against the live site (the highest-risk, highest-value half of this feature)
4. User Story 3 → preview workflow polish
5. Polish → publish-time safety net (T031) and ownership documentation (T032)

---

## Notes

- T022–T027 (US2) are the highest-risk tasks in this plan — they touch the only real content that currently exists in production. Do not delete any hand-authored page (T026) until T024's baseline-backed parity diff AND T025's untouched-area check are both confirmed clean.
- Do not skip T011 or schedule it later than immediately after T008–T010 — this was a real finding (`critic-006`) in the first gate pass, not a style preference: the two fail-loud cases and failed-build no-op guarantee must be proven before unrelated work is built on top of them.
- Commit after each checkpoint, not after each task — checkpoints are the meaningful, independently-testable units here.
