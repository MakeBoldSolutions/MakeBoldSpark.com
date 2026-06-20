---
classification: full-spec
risk_level: medium
target_workflow: specify-full
required_artifacts: spec, plan, tasks
recommended_next_step: plan
required_gates: checklist, analyze, critic
---

# Feature Specification: Static Content Build Pipeline

**Feature Branch**: `004-static-site-pipeline`
**Created**: 2026-06-20
**Status**: Draft <!-- Valid: Draft | In Progress | Complete -->
**Input**: User description: "Add a new src/MakeBoldSpark.Web project: a static site generator that authors articles and content in Markdown with frontmatter, and builds output directly into src/MakeBoldSpark.Api/wwwroot so the existing app continues to serve everything with no runtime changes."

## Rationale Summary

### Core Problem

Adding a new article or system page today means hand-copying an existing HTML file, manually editing nav/hero/footer markup, and manually keeping the duplicated boilerplate (navigation links, footer links, brand markup) consistent across every page. This was just demonstrated directly: a single article and several system pages were authored by copy-pasting near-identical HTML shells, and a stale reference (an old GitHub URL, a removed documentation route) had already crept into one of the copies before anyone noticed. The site has no way to author content as content — every new page is a manual HTML-authoring exercise, and the cost of that grows linearly with every page added.

### Decision Summary

Introduce a content build step that lets articles and system pages be authored as Markdown with structured front matter, and that generates the corresponding HTML output for the hosting application to serve — eliminating hand-copied HTML as the authoring method for new content going forward.

### Key Drivers

- Authoring friction: every new article currently requires copying and hand-editing a full HTML page rather than writing content.
- Consistency risk: hand-copied boilerplate (navigation, footer, hero markup) has already drifted out of sync at least once in the current content set.
- Growth: the site is expected to gain articles and system pages over time; the current approach does not scale with content volume.

### Source Inputs

- Direct conversation: prior manual authoring of `insights/devspark/spec-driven-development-harness` and per-system pages, which surfaced the pain point.
- Existing site structure under the hosting application's web root (marketing pages, system pages, article/insight pages, and a structured data file that already separates "system" metadata from generated "article" listings).
- Repository governance: the project's documented architecture principles favor static content and generated data files for public-facing sites.

### Tradeoffs Considered

- Option A — Keep hand-authoring HTML for each new page: rejected. Already produced a real consistency bug; cost compounds as content grows.
- Option B — Move content into the existing database-backed CMS feature instead of static files: rejected for this scope. The marketing/article content is intentionally static and versioned in source control, separate from the database-backed content management feature; mixing the two would blur an existing, deliberate separation.
- Selected — Option C: a dedicated content build step that compiles Markdown + front matter into the static output already being served, keeping the runtime application completely unchanged.

### Architectural Impact

- A new, clearly separated content-authoring area is introduced alongside the existing source projects; it produces output but is not itself part of the running application.
- The hosting application's runtime behavior is unaffected — it continues serving its web root exactly as before. No new routes, services, or runtime dependencies are introduced.
- Ownership boundaries must be explicit: some parts of the existing web root remain hand-maintained as-is, some become build-generated, and some continue to be populated dynamically at request time by existing client-side code. This spec requires those boundaries to be documented so nobody edits generated output by hand by mistake.
- Introduces the repository's first content-build tooling dependency, scoped only to the new authoring area — it must not become a runtime dependency of the hosting application.

### Reviewer Guidance

Focus review on: (1) whether the ownership boundary between hand-maintained, generated, and dynamically-rendered content is unambiguous enough that a future contributor won't accidentally edit generated output, (2) whether the migration of the just-authored article and system pages into the new authoring format is lossless (no visual or content regression), and (3) whether the chosen build-trigger point (when content gets (re)generated relative to building/publishing the application) is practical for a solo maintainer's workflow.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Author a new article without hand-writing HTML (Priority: P1)

A content owner wants to publish a new article about one of the ecosystem's systems. Today they would have to copy an existing HTML file and hand-edit every section. Instead, they write a single content file with a title, summary, associated system, category, tags, and a publish date, plus the article body in plain prose formatting — and the corresponding page, plus any listing pages that reference it, appear automatically without further manual editing.

**Why this priority**: This is the entire reason for the feature. Without it, nothing changes for the content owner.

**Independent Test**: Can be fully tested by authoring one new content file with front matter and a body, running the build, and confirming a new page is generated and that it appears in the relevant listing/index page(s) without any other file being hand-edited.

**Acceptance Scenarios**:

1. **Given** a new content file with valid front matter (title, summary, associated system, category, tags, publish date) and a body, **When** the content build runs, **Then** a new page is generated reachable at a predictable, content-derived address, and it is visually consistent with existing pages (same navigation, footer, and page structure).
2. **Given** the new content file is associated with an existing system, **When** the content build runs, **Then** that system's listing of related content includes the new entry, and the site-wide content index includes it, without manually editing either listing page.

---

### User Story 2 - Migrate existing hand-authored content into the new model (Priority: P1)

The content owner wants the article and system pages that were just hand-authored (and which exposed the consistency-drift problem) to become the first real content migrated into the new authoring model, proving the new pipeline reproduces them faithfully before anything is deleted.

**Why this priority**: Without this, the new pipeline is untested against real content, and there is no proof that switching authoring models doesn't lose or visually regress existing pages.

**Independent Test**: Can be fully tested by converting each existing hand-authored page into the new content-file format, regenerating output, and confirming each generated page is visually and structurally equivalent to the page it replaces (same content, no broken links, no missing sections).

**Acceptance Scenarios**:

1. **Given** an existing hand-authored article, **When** it is re-authored as a content file and the build runs, **Then** the generated page matches the original page's visible content and structure, and the original hand-authored file is removed once parity is confirmed.
2. **Given** the existing system pages reference a documentation link that no longer exists (a previously-identified stale reference), **When** they are migrated, **Then** the generated pages do not reproduce the stale reference.

---

### User Story 3 - Regenerate content locally with fast feedback (Priority: P2)

The content owner wants to preview a new or edited article before it's considered final, with a short feedback loop, without needing to start the full backend application.

**Why this priority**: Authoring is more practical with a fast preview loop; this is a workflow-quality improvement rather than a hard blocker for publishing content at all.

**Independent Test**: Can be fully tested by editing a content file and confirming the previewed output updates within a few seconds, without manually re-running a multi-step process each time.

**Acceptance Scenarios**:

1. **Given** a content file is being edited, **When** the file is saved, **Then** an updated preview becomes available within a few seconds without the author re-typing a build command.

---

### Edge Cases

- What happens when a new content file references a system that doesn't exist in the structured system data? The build must fail loudly (not silently produce a broken link or an empty listing) so the mistake is caught before publishing.
- What happens when two content files would generate pages at the same address? The build must fail loudly rather than silently overwriting one with the other.
- What happens if someone hand-edits a generated output file directly? Their edit will be silently overwritten on the next build — this must be made obvious (e.g., clearly distinguishing generated areas from hand-maintained areas) so it doesn't happen by accident.
- What happens to existing site areas that are populated dynamically at request time (not generated at build time) — must they be left untouched by this feature? Yes; this spec is scoped only to the areas that are currently hand-authored HTML, not to areas already populated dynamically.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Content owners MUST be able to author a new article as a single content file containing structured metadata (title, summary, associated system, category, tags, publish date, optional "featured" flag) and a body, without writing HTML directly.
- **FR-002**: The build process MUST generate a complete, navigable page for each authored content file, reusing the site's existing visual structure (navigation, footer, hero, and listing/card patterns) so generated pages are visually indistinguishable in style from hand-authored pages.
- **FR-003**: The build process MUST generate or update any listing/index pages that are derived from the set of authored content (e.g., a full content index, and per-system content listings) without requiring manual edits to those listing pages.
- **FR-004**: The build process MUST leave untouched any site areas that are not part of its generated output, including site areas that are populated dynamically at request time and any assets explicitly outside its ownership.
- **FR-005**: The build process MUST fail with a clear error — rather than producing incorrect or silently broken output — when a content file references a system that does not exist, or when two content files would resolve to the same generated address.
- **FR-006**: The repository MUST document, in a form a future contributor can find, which parts of the site's served output are hand-maintained, which are build-generated, and which are populated dynamically at request time.
- **FR-007**: The content owner MUST be able to get a near-real-time preview of in-progress content changes without needing to start the full backend application.
- **FR-008**: The build process MUST NOT alter the behavior of the running application — no new runtime routes, services, or request-time dependencies may be introduced as part of this feature.
- **FR-009**: The existing hand-authored article and system pages (the ones that just exposed the consistency-drift problem) MUST be migrated into the new authoring format with no loss of visible content and with the previously-identified stale reference corrected, not reproduced.
- **FR-010**: The maintainer MUST have a documented, repeatable way to (re)generate the site's content output before it is published, so generated output is never stale relative to authored content at publish time.

### Key Entities

- **Content Item**: A single authored piece of content (e.g., an article). Has a title, summary, an associated system identifier, a category, tags, a publish date, an optional update date, and an optional "featured" flag, plus a body.
- **System**: An existing structured entry describing one of the ecosystem's systems (name, status, category, summary, capabilities, related links). Content Items reference a System by identifier; this spec does not change how Systems themselves are defined, only how Content Items are authored and linked to them.
- **Generated Page**: The HTML output produced from a Content Item or from a System, plus any listing page assembled from multiple Content Items or Systems.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A content owner can publish a new article — from writing the content file to it being visible and correctly linked from all relevant listing pages — without hand-editing any file other than the one content file.
- **SC-002**: 100% of the previously hand-authored articles and system pages are reproduced through the new authoring model with no visible content loss, and the one previously-identified stale reference does not reappear.
- **SC-003**: A content edit is visible in a local preview within 5 seconds of saving, without re-running a multi-step manual process.
- **SC-004**: Zero instances of generated output being hand-edited directly, verified by the documented ownership boundary being followed in subsequent content changes.

## Assumptions

- The structured system data (system name, status, capabilities, etc.) continues to be maintained as hand-edited structured data rather than authored as Markdown content, since systems are few, change rarely, and are not "articles." Only article/insight content moves to the new Markdown-authoring model. This is treated as a working assumption rather than a locked decision — `/devspark.plan` should confirm it.
- The areas of the site populated dynamically at request time today (the broader ecosystem inventory listing and its data file) are explicitly out of scope and are not affected by this feature.
- "Near-real-time preview" (SC-003) assumes a local preview mechanism exists that does not require the full backend application to be running; the specific mechanism is a planning-stage decision.
- No user-facing authentication, authorization, or data-persistence concerns are introduced by this feature — it produces static files only.

## Out of Scope

- The broader ecosystem inventory page and its dynamic, request-time rendering — unaffected by this feature.
- The backend application's API routes, authentication, and authorization — unaffected by this feature.
- Any database-backed content management functionality — unaffected; this feature concerns only static, source-controlled content.
- Deployment/CI automation beyond documenting the build trigger point — the specific automation mechanism (if any) is a planning-stage decision, not specified here.
