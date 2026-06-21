```yaml
gate: critic
status: warn
blocking: false
severity: error
summary: "No showstoppers or constitution violations. One CRITICAL finding (clean-before-validate ordering can turn a single bad content edit into a site-wide outage of /insights and /systems) should be fixed before T008/T028 are implemented for real."
```

## Technical Risk Assessment

**Analysis Date:** 2026-06-20T00:00:00Z
**Scope:** FULL (spec.md + plan.md + tasks.md)
**Detected Archetype:** web-service (from constitution Technology Stack / Platform Architecture — no frontmatter override; not a blind default, so no `archetype-ambiguity` finding)
**Detected Stack:** Node.js (LTS) + Eleventy (`@11ty/eleventy`), layered on a .NET 10 / ASP.NET Core repo
**Context Mode:** migration (User Story 2 explicitly migrates 11 already-live production pages; User Story 1's tooling itself is greenfield — mixed, migration lens applied as primary since it's where real production content is at risk)
**Risk Profile:** internal (defaulted — not declared in spec.md frontmatter or constitution; see critic-005)
**Risk Posture:** YELLOW

### Executive Summary

The architecture is sound and well-justified (single new dependency, clear ownership-boundary split, fail-loud validation). The one real production-failure mode the artifacts missed: the "delete-then-rebuild" approach to `wwwroot/insights/` and `wwwroot/systems/` (T008) deletes those trees *before* the build that's supposed to repopulate them — so any build failure (a bad `systems.json` edit, an Eleventy config bug, a transient `npm ci` failure) leaves those paths 404ing in production, not merely "unchanged." Six other findings (lockfile discipline, validation-hook trust, Node version drift, missing risk profile, test-ordering, dependency pinning) are real but lower-impact. No constitution violations.

### Findings (source of truth)

```yaml
findings:
  - finding_id: critic-001
    category: deployment_rollback
    archetype_applicable: true
    location: tasks.md#T008, tasks.md#T028, research.md#4
    description: "clean-output.mjs deletes wwwroot/insights/ and wwwroot/systems/ before Eleventy runs, with no staging/atomic-swap step. If the build fails after the clean step — a bad systems.json edit, a config bug, a transient npm failure, including inside the BeforeTargets=\"Publish\" hook (T028) — those two trees are left deleted with nothing to repopulate them, turning a build failure into a live 404 outage for every system/insight page rather than a no-op."
    base_severity: critical
    effective_severity: critical
    recommended_action: "Build into a temporary output directory first (e.g. wwwroot/.eleventy-tmp); only delete-and-replace the real insights/ and systems/ trees after Eleventy exits 0. Update T008/T009 and T028 accordingly before implementation."
    execution_mode: selective
    status: open
    outcome: ""

  - finding_id: critic-002
    category: dependency_supply_chain
    archetype_applicable: true
    location: tasks.md#T002, tasks.md#T028
    description: "T002 runs `npm install` (which can update package-lock.json) while T028's publish hook runs `npm ci` (which requires an exact, committed, in-sync lock file and fails otherwise). No task explicitly commits package-lock.json or verifies it's in sync before the publish hook is wired up, so the first `dotnet publish` after setup risks failing on a lockfile mismatch that was never tested locally."
    base_severity: high
    effective_severity: high
    recommended_action: "Add an explicit task: commit package-lock.json in the same change as package.json, and run `npm ci` locally at least once (not just `npm install`) before T028 is considered done, so the exact command the publish hook will run has been exercised."
    execution_mode: auto
    status: open
    outcome: ""

  - finding_id: critic-003
    category: error_handling_resilience
    archetype_applicable: true
    location: tasks.md#T007, research.md#7
    description: "The system-reference validation hook is described as throwing inside an `eleventy.before` event. If the hook is async and its rejection/throw isn't correctly surfaced to Eleventy's build process (a common mistake with event-emitter-style hooks), the error can be swallowed and the build will exit 0 with a broken/missing page instead of failing loudly — directly undermining FR-005, which this hook exists to satisfy."
    base_severity: high
    effective_severity: high
    recommended_action: "Before wiring T007 into the publish-time path (T028), confirm via T019's fixture test that a real non-zero exit code is produced end-to-end (not just that an Error object is constructed). Add an explicit assertion on process exit code, not just on console output, in verify-build.mjs."
    execution_mode: auto
    status: open
    outcome: ""

  - finding_id: critic-004
    category: deployment_rollback
    archetype_applicable: true
    location: plan.md#Technical-Context, research.md#6
    description: "The plan assumes Node/npm is available wherever `dotnet publish` runs, validated only against the current manual Windows-workstation-to-VM-zip flow. The constitution's stated production target is Azure App Service Linux B1; if publishing ever moves to a Kudu/Oryx build-from-source or CI-based flow on that target, Node may not be preinstalled in that environment, and the MSBuild Exec hook would fail there in a way never tested today."
    base_severity: medium
    effective_severity: medium
    recommended_action: "Add a one-line note to quickstart.md or plan.md's Constraints stating this hook is validated for the current manual-workstation publish flow only, and must be re-verified before any future CI/Kudu-based deploy path is introduced."
    execution_mode: manual
    status: open
    outcome: ""

  - finding_id: critic-005
    category: documentation
    archetype_applicable: true
    location: spec.md#frontmatter
    description: "spec.md frontmatter declares no `risk_profile`. Per the critic backward-compatibility rule, this defaults to `internal` rather than being an explicit, reviewed decision."
    base_severity: high
    effective_severity: high
    recommended_action: "Add `risk_profile: internal` explicitly to spec.md frontmatter so the default is a recorded decision, not a silent gap, for this and future gate runs."
    execution_mode: auto
    status: open
    outcome: ""

  - finding_id: critic-006
    category: testing_strategy
    archetype_applicable: true
    location: tasks.md#T019
    description: "T019 (the FR-005 regression test) is scheduled after T010–T018 rather than immediately after T007, which it directly validates. tasks.md's own Notes section already flags T007/T019 as the highest-value pair not to skip, but the current ordering means up to 9 tasks of template/collection work happen before the validation hook is proven to actually fail loudly."
    base_severity: medium
    effective_severity: medium
    recommended_action: "Move T019 to run immediately after T007 (end of Phase 2 or start of Phase 3), before T010–T018, so the fail-loud guarantee is proven before anything depends on it."
    execution_mode: selective
    status: open
    outcome: ""

  - finding_id: critic-007
    category: dependency_supply_chain
    archetype_applicable: true
    location: plan.md#Technical-Context
    description: "No Node version is pinned (no `.nvmrc`, no `package.json` `engines` field). plan.md records the workstation's current Node version (v25.4.0) descriptively but nothing enforces it, so a future machine running an incompatible Node major version could produce silently different Eleventy output."
    base_severity: medium
    effective_severity: medium
    recommended_action: "Add an `engines` field to package.json (e.g. `\"node\": \">=20\"`) and/or a `.nvmrc`, and have T002 set it explicitly."
    execution_mode: auto
    status: open
    outcome: ""
```

### Critical

| ID | Category | Location | Risk | Likely Impact | Action |
| --- | --- | --- | --- | --- | --- |
| critic-001 | deployment_rollback | tasks.md#T008, T028 | Clean-before-validate ordering deletes generated trees before confirming the rebuild will succeed | Any failed build (bad data edit, config bug, transient npm failure) turns into a live 404 outage on every system/insight page, not a no-op | Build to a temp dir, swap in only after a successful build |
| critic-002 | dependency_supply_chain | tasks.md#T002, T028 | `npm install` (T002) vs `npm ci` (T028) lockfile mismatch never explicitly tested | First `dotnet publish` after setup can fail on an unverified lockfile | Commit package-lock.json; exercise `npm ci` locally before relying on it in T028 |
| critic-003 | error_handling_resilience | tasks.md#T007 | Validation hook's fail-loud guarantee unverified against actual process exit code | FR-005 silently unenforced if the hook's error is swallowed | Assert real exit code in T019, before wiring T007 into T028 |
| critic-005 | documentation | spec.md frontmatter | `risk_profile` undeclared, silently defaulted | Future gate runs repeat the same silent default instead of a reviewed decision | Add `risk_profile: internal` explicitly to spec.md |

### High

_(No findings classified as base High outside the Critical table above — critic-002/003/005 are HIGH base severity and are listed in the Critical-and-High consolidated table for readability.)_

### Missing Critical Tasks

- **Operations:** No staged/atomic-swap step for the build output (drives critic-001); no rollback procedure documented for a failed publish-time content build beyond "fix and re-run."
- **Testing:** No CI exists to run `npm test` automatically — explicitly accepted as Out of Scope by the spec, but worth restating here as a standing residual risk for a solo maintainer who may forget to run it locally.

### Questionable Assumptions

1. **"Node/npm will be available wherever `dotnet publish` runs"** → Failure mode: true today on the manual Windows-workstation flow; not yet validated for any future CI or Azure-Linux-Kudu publish path (critic-004).
2. **"`npm install` now, `npm ci` later, will just work"** → Failure mode: lockfile drift between setup-time and publish-time invocation styles (critic-002).
3. **"Throwing inside the validation hook is sufficient to fail the build"** → Failure mode: async error-swallowing inside Eleventy's event hooks is a known footgun across static-site generators; needs an explicit exit-code assertion, not just a thrown Error (critic-003).

### Dependency Risk Assessment

| Dependency | Concern | Alternative |
| --- | --- | --- |
| `@11ty/eleventy` | No version pinned beyond an implied lockfile; a future `npm install` (vs. `npm ci`) could pull a new major version with a breaking config API | Pin an exact or caret-with-lockfile-discipline version; rely on `npm ci` everywhere, including local setup |
| Node.js runtime | No `.nvmrc`/`engines` pin; version drift across machines/time could change output silently | Add `engines` field + `.nvmrc` (critic-007) |

### Estimated Technical Debt at Launch

- **Operational Debt:** No staged-build/rollback mechanism for generated content (critic-001) — should be closed before T008 is implemented, not after.
- **Documentation Debt:** Missing `risk_profile` frontmatter (critic-005); missing publish-environment caveat (critic-004) — both single-line fixes.

### Metrics

- Showstopper: 0 | Critical: 1 | High: 3 | Medium: 3 (effective severity)
- Findings by category: deployment_rollback (2), dependency_supply_chain (2), error_handling_resilience (1), documentation (1), testing_strategy (1)
- Missing operational tasks: 1 (staged/atomic build-output swap)

**VERDICT:** CONDITIONAL

**Required Actions Before Implementation:**

1. Resolve critic-001 (build-to-temp-then-swap) before implementing T008/T009/T028 — this is the one finding with genuine production-outage potential.
2. Resolve critic-002 and critic-003 before T028 is considered done — both are cheap, concrete fixes that close real gaps in the publish-time safety net the feature exists to provide.

**Recommended Risk Mitigations:**

- Add `risk_profile: internal` to spec.md frontmatter (critic-005) — trivial, closes a process gap.
- Add Node version pinning (critic-007) and the future-deploy-path caveat (critic-004) — both single-line documentation additions.
- Reorder T019 to immediately follow T007 (critic-006) so the fail-loud guarantee is proven before nine tasks of template work are built on top of it.
