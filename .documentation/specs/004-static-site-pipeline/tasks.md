# Tasks: Static Content Build Pipeline

**Input**: Design documents from `.documentation/specs/004-static-site-pipeline/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/content-frontmatter.md, quickstart.md

**Tests**: Included — the spec's FR-005 (fail loudly on bad input) and the constitution's Test-First principle both require a verifiable test task; see T019.

**Organization**: Tasks are grouped by user story (US1, US2, US3 from spec.md) to enable independent implementation and testing of each.

## Rationale Summary

### Core Problem

Every new article or system page today requires hand-copying an existing HTML file and manually keeping duplicated boilerplate in sync — a real consistency bug already resulted from this once.

### Decision Summary

Stand up an Eleventy build (`src/MakeBoldSpark.Web/`) that generates `wwwroot/insights/**`, `wwwroot/systems/**`, and `wwwroot/assets/makebold/catalog.json` from Markdown content files and a hand-maintained `systems.json`, then migrate the existing 11 hand-authored pages into it as the first real content.

### Key Drivers

- Eliminate hand-copied HTML as the only authoring path (FR-001–FR-003).
- Prove the migration is lossless before declaring the old authoring path retired (FR-009, SC-002).
- Guarantee publish-time freshness without relying on memory (FR-010, via the MSBuild hook).

### Reviewer Guidance

Focus on: the Foundational phase (T004–T009) being genuinely complete before any User Story task starts: confirm the system-reference validation (T007) and orphan-cleanup script (T008/T009) both work before any content migration begins, since US2's parity check (T022) is only meaningful once orphan-cleanup and validation are trustworthy.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

All new files live under `src/MakeBoldSpark.Web/` (not part of `MakeBoldSpark.slnx`). The only change to an existing .NET project is one MSBuild `Target` in `src/MakeBoldSpark.Api/MakeBoldSpark.Api.csproj` (T028). Generated output paths are under `src/MakeBoldSpark.Api/wwwroot/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Create `src/MakeBoldSpark.Web/` directory tree per plan.md's Project Structure (`src/_data/`, `src/_includes/layouts/`, `src/content/articles/`, `scripts/`, `test/fixtures/`)
- [ ] T002 Initialize `src/MakeBoldSpark.Web/package.json` and run `npm install @11ty/eleventy` (the only dependency)
- [ ] T003 [P] Add `src/MakeBoldSpark.Web/.gitignore` entry for `node_modules/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core build machinery that MUST exist before any user story can be implemented or tested

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T004 Create `src/MakeBoldSpark.Web/src/_data/systems.json`, seeded from the 4 entries currently in `src/MakeBoldSpark.Api/wwwroot/assets/makebold/catalog.json`'s `systems` array (devspark, apitestspark, webspark, makeboldspark) — carry forward the already-corrected `makeboldspark` entry (no Scalar reference, correct GitHub URL) per data-model.md's System schema
- [ ] T005 [P] Create `src/MakeBoldSpark.Web/src/_includes/layouts/base.njk` reproducing the existing nav/hero/footer markup and `brand.css` classes shared by every page (source from the current `wwwroot/systems/index.html` and `wwwroot/insights/index.html` markup)
- [ ] T006 Implement `src/MakeBoldSpark.Web/eleventy.config.js`: set `dir.input`/`dir.output` so generated paths land at `src/MakeBoldSpark.Api/wwwroot/{insights,systems}` and `wwwroot/assets/makebold/catalog.json`; register `systems.json` as Eleventy global data
- [ ] T007 Implement the system-reference validation hook in `eleventy.config.js` (an `eleventy.before` check): every Content Item's `system` field must resolve to an `id` in `systems.json`, else throw and fail the build (FR-005, research.md Topic 7) — depends on T004, T006
- [ ] T008 [P] Implement `src/MakeBoldSpark.Web/scripts/clean-output.mjs`: recursively delete `src/MakeBoldSpark.Api/wwwroot/insights/` and `src/MakeBoldSpark.Api/wwwroot/systems/` before each build (FR-011, research.md Topic 4)
- [ ] T009 Wire `clean-output.mjs` as a `prebuild` npm script in `package.json` that always runs before `build`/`serve` (depends on T008)

**Checkpoint**: Foundation ready — user story implementation can now begin.

---

## Phase 3: User Story 1 - Author a new article without hand-writing HTML (Priority: P1) 🎯 MVP

**Goal**: A content owner writes one Markdown file with front matter and gets a complete, navigable, correctly-linked page — no other file hand-edited.

**Independent Test**: Author one new content file, run `npm run build`, confirm a new page exists at its system-scoped address and appears in the relevant listing(s).

### Implementation for User Story 1

- [ ] T010 [US1] Create `src/MakeBoldSpark.Web/src/_includes/layouts/article.njk` (extends `base.njk`), matching the existing migrated article's structure (eyebrow/title/tags/body/footer-nav)
- [ ] T011 [US1] Create `src/MakeBoldSpark.Web/src/_includes/layouts/system.njk` (extends `base.njk`), matching the existing system detail page structure (hero-actions: GitHub / Docs / Related Insights)
- [ ] T012 [P] [US1] Implement `src/MakeBoldSpark.Web/src/systems.njk` (paginated over `systems.json`) generating `/systems/{id}/index.html` for every System — depends on T011
- [ ] T013 [P] [US1] Implement `src/MakeBoldSpark.Web/src/systems-index.njk` generating `/systems/index.html` (depends on T011)
- [ ] T014 [US1] Configure the `articles` collection in `eleventy.config.js` (front matter fields per data-model.md, permalink `/insights/{{ system }}/{{ slug }}/index.html`) and apply `article.njk` — depends on T010
- [ ] T015 [P] [US1] Implement `src/MakeBoldSpark.Web/src/insights-index.njk` generating `/insights/index.html` across the full article collection (depends on T014)
- [ ] T016 [P] [US1] Implement `src/MakeBoldSpark.Web/src/insights-by-system.njk` generating `/insights/{system}/index.html` per System, preserving the existing "Insights are being prepared" empty-state copy when a System has zero articles (depends on T014)
- [ ] T017 [US1] Implement `src/MakeBoldSpark.Web/src/catalog.njk` generating `assets/makebold/catalog.json` from `systems.json` + the article collection (depends on T004, T014)
- [ ] T018 [US1] Author one throwaway sample content file (`src/content/articles/_sample.md`), run `npm run build`, confirm the generated page and its listing entries, then delete the sample and rebuild — proves the end-to-end flow before real migration begins
- [ ] T019 [US1] Create `test/fixtures/unknown-system-reference.md` and `scripts/verify-build.mjs`, wired as `npm test`: asserts a normal build exits 0, and a build including the fixture exits non-zero — validates FR-005 / Test-First (depends on T007)

**Checkpoint**: User Story 1 is fully functional and independently testable.

---

## Phase 4: User Story 2 - Migrate existing hand-authored content into the new model (Priority: P1)

**Goal**: The 11 pages migrated into `wwwroot/{insights,systems}/**` earlier this session become the first real content in the new model, with no visible content loss and no reintroduction of the already-corrected stale references.

**Independent Test**: Convert each existing page, rebuild, and confirm every generated page is visually/structurally equivalent to what's live today.

### Implementation for User Story 2

- [ ] T020 [US2] Migrate the one real article (`wwwroot/insights/devspark/spec-driven-development-harness/index.html`) into `src/MakeBoldSpark.Web/src/content/articles/spec-driven-development-harness.md` with front matter matching its current title/summary/tags/published date — depends on T014
- [ ] T021 [US2] Cross-check `systems.json` (T004) against the live `wwwroot/systems/{devspark,apitestspark,webspark,makeboldspark}/index.html` pages field-by-field; reconcile any gaps against data-model.md's System schema
- [ ] T022 [US2] Run `npm run build` and diff every generated page against the current hand-authored HTML in `wwwroot/{insights,systems}/**` for parity — confirms SC-002 (no content loss, no reintroduced stale references) — depends on T020, T021
- [ ] T023 [US2] Delete the superseded hand-authored pages and `wwwroot/assets/makebold/site.js` (and any remaining `<script src="/assets/makebold/site.js">` references), since `catalog.json` is now build output and nothing fetches it client-side for these pages (research.md Topic 5) — depends on T022
- [ ] T024 [US2] Run `dotnet build` and `dotnet test` for `MakeBoldSpark.Api`/`MakeBoldSpark.Api.Tests` to confirm the .NET app is unaffected by the `wwwroot` content changes (FR-008) — depends on T022, T023

**Checkpoint**: User Stories 1 AND 2 both work; the migration is proven lossless.

---

## Phase 5: User Story 3 - Regenerate content locally with fast feedback (Priority: P2)

**Goal**: A content owner previews in-progress changes within a few seconds, without running the .NET app.

**Independent Test**: Edit a content file while `npm run serve` is running; confirm the preview updates within ~5 seconds (SC-003).

### Implementation for User Story 3

- [ ] T025 [US3] Add a `serve` npm script (`eleventy --serve`) watching `src/content`, `src/_data`, and `src/_includes` (depends on Phase 2 completion)
- [ ] T026 [US3] Manually verify live-reload latency against the SC-003 5-second budget by editing a sample article while `npm run serve` is running

**Checkpoint**: All three user stories are independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Publish-time safety net and ownership-boundary documentation that span all stories

- [ ] T027 [P] Finalize `package.json` scripts section (`build`, `prebuild`, `serve`, `test`) consolidating T009/T019/T025
- [ ] T028 Add a `BeforeTargets="Publish"` MSBuild `Target` to `src/MakeBoldSpark.Api/MakeBoldSpark.Api.csproj` that runs `npm ci && npm run build` in `src/MakeBoldSpark.Web`, failing the publish loudly (non-zero exit) if Node/npm is unavailable or the build fails (FR-010, research.md Topic 6)
- [ ] T029 [P] Create `src/MakeBoldSpark.Api/wwwroot/README.md` documenting the three-tier ownership boundary (hand-maintained / build-generated / dynamic-at-request-time) per FR-006
- [ ] T030 Run every step in `quickstart.md` end-to-end (author an article, add a System, delete a content file and confirm orphan removal, run `dotnet publish` and confirm it triggers the Eleventy build)
- [ ] T031 Re-run `dotnet build`/`dotnet test` one final time and confirm `git status` shows only the expected new/changed files before handing off to `/devspark.critic` and `/devspark.analyze`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational only
- **User Story 2 (Phase 4)**: Depends on Foundational AND on User Story 1's templates/collection config (T010, T014) — not independent of US1's implementation, though it is independently *testable* once US1 exists
- **User Story 3 (Phase 5)**: Depends on Foundational only; independent of US1/US2 content (just needs the build to exist)
- **Polish (Phase 6)**: Depends on all three user stories being complete

### Parallel Opportunities

- T003 can run alongside T001/T002
- T005 and T008 (Foundational) can run in parallel — different files
- T012/T013 (system pages) and T015/T016 (insight listings) can run in parallel once their respective dependencies (T011, T014) are done
- T027 and T029 (Polish) can run in parallel

---

## Parallel Example: Foundational Phase

```bash
Task: "Create src/MakeBoldSpark.Web/src/_includes/layouts/base.njk"
Task: "Implement src/MakeBoldSpark.Web/scripts/clean-output.mjs"
```

---

## Gate Acknowledgements

None recorded — `checklists/requirements.md` (the only gate artifact that exists so far) passes in full. `/devspark.critic` and `/devspark.analyze` have not yet run against this plan/tasks pair.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (Setup) + Phase 2 (Foundational)
2. Complete Phase 3 (User Story 1) — author and verify one sample article end-to-end
3. **STOP and VALIDATE**: confirm T018/T019 pass before touching any real content

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. User Story 1 → sample content proves the pipeline works (MVP)
3. User Story 2 → real migration, parity-checked against the live site (the higher-risk, higher-value half of this feature)
4. User Story 3 → preview workflow polish
5. Polish → publish-time safety net (T028) and ownership documentation (T029)

---

## Notes

- T020–T024 (US2) are the highest-risk tasks in this plan — they touch the only real content that currently exists in production. Do not delete any hand-authored page (T023) until T022's parity diff is confirmed clean.
- Commit after each checkpoint, not after each task — checkpoints are the meaningful, independently-testable units here.
- Avoid skipping T007/T019 (the fail-loud validation and its test) — this is the one place a future, less careful migration could silently produce a broken link.
