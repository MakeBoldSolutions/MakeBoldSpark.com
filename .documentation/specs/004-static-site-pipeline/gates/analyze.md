```yaml
gate: analyze
status: warn
blocking: false
severity: warning
summary: "No constitution violations, no Core-Problem drift. 5 findings: 2 HIGH (FR-004 has zero task coverage; the Nunjucks/.njk templating choice has no recorded Decision in research.md), 3 MEDIUM (docsUrl redundancy unenforced, T022's diff method unspecified, SC-004 is a continuous-process claim not a one-time check)."
```

## Specification Analysis Report

| ID | Category | Severity | Location(s) | Summary | Recommendation |
| --- | --- | --- | --- | --- | --- |
| COV-001 | Coverage Gap | HIGH | spec.md FR-004; tasks.md (no match) | FR-004 ("leave untouched any site areas that are not part of its generated output") has zero dedicated task. T022 diffs *generated* output for parity but nothing explicitly verifies that `index.html`, `vision.html`, `ecosystem.html`, `subsites.json`, and `assets/makebold/{brand.css,logos,fonts}` are byte-for-byte unchanged after a build. | Add a task (e.g. in Phase 4 or Phase 6) that hashes/diffs the hand-maintained tier before and after `npm run build` and fails if anything outside `insights/`, `systems/`, and `catalog.json` changed. |
| RAT-001 | Rationale & Traceability | HIGH | plan.md Project Structure (`.njk` files); research.md (no matching topic) | plan.md's Project Structure tree commits to Nunjucks (`.njk` extension) for every layout/template, but research.md never records this as a Decision — Eleventy supports several template languages (Nunjucks, Liquid, EJS, 11ty.js) and no alternatives-considered/rationale exists for picking Nunjucks specifically. | Add a Topic 9 to research.md: "Template Language Choice" with Decision: Nunjucks, Rationale, and Alternatives considered (at minimum: plain `.11ty.js` templates, which would need zero new file-extension/syntax learning beyond JS already in use). |
| INC-001 | Inconsistency | MEDIUM | data-model.md System.docsUrl | `docsUrl` is defined as a hand-maintained field in `systems.json` that "should equal" the build-derived address `/systems/{id}/`, but no task validates or enforces that equality — the two can drift silently (e.g. a typo in `docsUrl` after a System's `id` changes). | Either remove `docsUrl` from `systems.json` and compute it everywhere from `id` (simplest — also reduces the hand-maintained field count), or add a build-time assertion (in the same hook as T007) that `docsUrl === "/systems/" + id + "/"`. |
| AMB-001 | Ambiguity | MEDIUM | tasks.md T022 | "diff every generated page against the current hand-authored HTML ... for parity" does not state whether this is an automated diff (e.g. normalized-HTML comparison script) or a manual visual review. SC-002's "100%" claim is only as rigorous as whichever method is actually used. | Specify the method explicitly in T022 — e.g. "manually review every generated page side-by-side with its current live equivalent" (acceptable given the 11-page scope) or "run an automated whitespace-normalized HTML diff." Either is fine; leaving it unstated is the issue. |
| UND-001 | Underspecification | MEDIUM | spec.md SC-004 | "Zero instances of generated output being hand-edited directly, verified by the documented ownership boundary being followed in subsequent content changes" describes an ongoing behavioral outcome, not a single point-in-time check — unlike SC-001/SC-002/SC-003, no task can "complete" this the way T018/T022/T026 complete the others. | Either reframe SC-004 as a one-time check ("the README.md ownership boundary doc exists and explicitly calls out every generated path," which T029 *does* satisfy) or accept it explicitly as a non-task-mapped, continuously-monitored criterion in tasks.md's Notes section. |

**Coverage Summary Table:**

| Requirement Key | Has Task? | Task IDs | Notes |
| --- | --- | --- | --- |
| fr-001-author-without-html | Yes | T014, T018 | |
| fr-002-generate-complete-page | Yes | T010, T011, T012, T013 | |
| fr-003-generate-listing-pages | Yes | T015, T016, T017 | |
| fr-004-leave-untouched-areas | **No** | — | COV-001 |
| fr-005-fail-loud-validation | Yes | T007, T019 | |
| fr-006-document-ownership-boundary | Yes | T029 | |
| fr-007-near-real-time-preview | Yes | T025, T026 | |
| fr-008-no-runtime-change | Yes | T024 | |
| fr-009-migrate-existing-content | Yes | T020, T021, T022, T023 | |
| fr-010-repeatable-publish-time-build | Yes | T009, T028, T030 | |
| fr-011-remove-orphaned-pages | Yes | T008, T009, T030 | |
| sc-001-publish-without-other-edits | Yes (via story) | T018 | |
| sc-002-lossless-migration | Yes | T022 | AMB-001 affects rigor |
| sc-003-preview-latency | Yes | T026 | |
| sc-004-no-hand-edits-of-output | Partial | T029 | UND-001 — process claim, not a single check |

**Constitution Alignment Issues:** None. plan.md's own Constitution Check (all 10 principles PASS/PASS-N/A) holds up under review — no MUST-principle conflicts found in spec/plan/tasks.

**Unmapped Tasks:** None — every task maps to a requirement, a Setup/Foundational prerequisite, or a Polish cross-cutting concern (T027, T029, T030, T031).

**Cross-reference (owned by `/devspark.critic`, not duplicated here):** tasks.md's T019 (the FR-005 regression test) is sequenced after nine unrelated US1 tasks rather than immediately after T007. This is a sequencing-quality/testing-strategy risk, already raised as `critic-006` in `gates/critic.md` — see that gate, not a new analyze finding.

**Metrics:**

- Total Requirements: 11 FR + 4 SC = 15
- Total Tasks: 31 (T001–T031)
- Coverage %: 10/11 FR have ≥1 task (91%); 3/4 SC fully covered, 1 partial (SC-004)
- Ambiguity Count: 1 (AMB-001)
- Duplication Count: 0
- Critical Issues Count: 0

## Next Actions

No CRITICAL issues — proceeding to `/devspark.implement` is not blocked. Recommended before implementation, in priority order:

1. **COV-001** (HIGH) — add the untouched-areas verification task; this is the cheapest insurance against an Eleventy config bug accidentally clobbering `index.html`/`vision.html`/`ecosystem.html`.
2. **RAT-001** (HIGH) — add the templating-language Decision to research.md; five-minute fix, closes a real traceability gap.
3. **INC-001, AMB-001, UND-001** (MEDIUM) — cheap clarifications; fold into the same editing pass as the critic findings.

Suggested command after remediation: re-run `/devspark.analyze` once (to confirm COV-001/RAT-001 are closed), then proceed to `/devspark.implement`.

Would you like concrete remediation edits for these 5 findings now, alongside the 7 critic findings?
