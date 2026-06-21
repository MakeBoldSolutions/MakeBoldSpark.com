```yaml
gate: critic
status: warn
blocking: false
severity: warning
summary: "The original critical build-outage path and six supporting findings are closed in the revised design. One HIGH finding remains: catalog.json is declared generated output but is not included in the staged swap, risking stale or inconsistent derived data."
```

## Technical Risk Assessment

**Analysis Date:** 2026-06-20T22:10:39.7183710Z
**Scope:** FULL (spec.md + plan.md + tasks.md)
**Detected Archetype:** web-service (ASP.NET Core hosting application; this feature is a Node/Eleventy build tool layered on it)
**Detected Stack:** Node.js (LTS) + Eleventy (`@11ty/eleventy`), layered on .NET 10 / ASP.NET Core
**Context Mode:** migration (the feature replaces 11 live hand-authored pages)
**Risk Profile:** internal
**Risk Posture:** YELLOW

### Executive Summary

The revised artifacts address the previous production-outage risk: builds stage output before replacing the generated trees, verify actual process exit codes for both FR-005 failure modes, and prove failed-build preservation before user-story work. Lockfile discipline, Node version pinning, publish-environment scope, explicit risk profile, and test sequencing are also covered.

One delivery-consistency gap remains. The design generates `assets/makebold/catalog.json`, but the staged-swap task replaces only `insights/` and `systems/`; without an explicit staged replacement for `catalog.json`, it can remain stale or diverge from the generated pages.

### Findings (source of truth)

```yaml
findings:
  - finding_id: critic-008
    category: deployment_rollback
    archetype_applicable: true
    location: tasks.md#T009, tasks.md#T020, plan.md#Decision-Summary
    description: "T020 generates assets/makebold/catalog.json, but T009's success path stages and replaces only wwwroot/insights/ and wwwroot/systems. The build therefore has no specified delivery step for catalog.json, and any ad-hoc copy after the directory swaps can leave catalog data stale or inconsistent with the newly generated pages if it fails."
    base_severity: high
    effective_severity: high
    recommended_action: "Extend build-and-swap.mjs and its T011/T033 verification to stage catalog.json and replace it only after a successful build, with failure preserving the prior catalog alongside the prior generated trees."
    execution_mode: selective
    status: open
    outcome: ""
```

### High

| ID | Category | Location | Issue | Impact | Suggestion |
| --- | --- | --- | --- | --- | --- |
| critic-008 | deployment_rollback | tasks.md#T009, T020 | `catalog.json` is generated but excluded from the staged delivery operation | Derived catalog data may be stale or disagree with the generated system/article pages | Stage and replace it with the generated trees; test both success and failed-build preservation |

### Questionable Assumptions

1. **Replacing only `insights/` and `systems/` is sufficient for all generated output.** → Failure mode: `catalog.json` remains from a previous build while the pages were regenerated from current content.

### Dependency Risk Assessment

| Dependency | Concern | Existing Control |
| --- | --- | --- |
| `@11ty/eleventy` | Reproducibility and API drift across developer/publish environments | Committed lockfile, `npm ci`, `.nvmrc`, and `package.json` Node engine constraint are planned in T002/T003/T012 |
| Node.js runtime | Future CI/Kudu publish environments may not match the validated workstation environment | T031 documents that the MSBuild hook must be re-validated before any deploy-path change |

### Estimated Technical Debt at Launch

- **Operational debt:** catalog delivery is not yet part of the build-and-swap unit (`critic-008`).
- **Residual process risk:** no CI exists to run the build tests automatically; the spec explicitly keeps CI setup out of scope, so this remains an accepted manual-workflow risk.

### Metrics

- Showstopper: 0 | Critical: 0 | High: 1
- Findings by category: deployment_rollback (1)
- Missing operational tasks: 1 (stage and verify catalog delivery)

**VERDICT:** CONDITIONAL

**Required Actions Before Implementation:**

1. Add `catalog.json` to the staged delivery and its failure-preservation verification before treating the build output as internally consistent.

**Recommended Risk Mitigations:**

- Retain the planned `npm ci` lockfile check and Node pinning.
- Re-run this critic gate after the catalog-delivery task is added, then proceed with implementation.
