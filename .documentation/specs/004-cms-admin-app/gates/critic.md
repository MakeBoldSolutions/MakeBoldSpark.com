# Critic Gate

```yaml
gate: critic
status: pass
blocking: false
severity: info
summary: "All 8 findings from the prior run (including the SHOWSTOPPER deployment-ordering risk and the CRITICAL throttle gap) are resolved by added/expanded tasks in tasks.md and a spec.md frontmatter update. Re-validated 2026-06-22 after remediation — no remaining blockers to implementation."
```

## Technical Risk Assessment (re-validated after remediation)

**Analysis Date:** 2026-06-22 (re-run)
**Scope:** FULL (spec.md + plan.md + tasks.md)
**Detected Archetype:** web-service (now explicit in spec.md frontmatter — see critic-008 resolution)
**Detected Stack:** C# / .NET 10 + ASP.NET Core + React/Vite + SQLite/EF Core
**Context Mode:** brownfield (now explicit in spec.md frontmatter)
**Risk Profile:** internal (now explicit in spec.md frontmatter — see critic-008 resolution)
**Risk Posture:** GREEN

### Executive Summary

All findings from the prior run are addressed. The one SHOWSTOPPER (deployment ordering) and two CRITICALs (spray-resistant throttling, missing startup test) now have corresponding tasks in `tasks.md` (T009a, T010, T008a respectively). Remaining findings (HIGH/MEDIUM/LOW) are similarly closed by task additions or the `risk_profile`/`change_type`/`archetype` frontmatter update. No new risks were introduced by the remediation itself (see verification notes per finding below).

### Findings (source of truth — all now resolved)

```yaml
findings:
  - finding_id: critic-001
    category: deployment_rollback
    archetype_applicable: true
    location: plan.md#Architectural-Impact; tasks.md T009a
    description: AddMakeBoldSparkAuth registers JWT auth globally; the T009 fail-fast guard could crash the entire API on deploy if Jwt:SigningKey is missing.
    base_severity: showstopper
    effective_severity: showstopper
    recommended_action: Add a deployment-runbook task verifying the setting exists before T009 ships.
    execution_mode: manual
    status: resolved
    outcome: "T009a added to Phase 2 (Foundational): pre-deploy runbook checklist item in quickstart.md, explicitly scoped as a deploy-time-only gate that does not block local development or story implementation (Phases 3-6). plan.md's Architectural Impact and Reviewer Guidance sections updated to cross-reference it."
  - finding_id: critic-002
    category: auth_authz
    archetype_applicable: true
    location: research.md; tasks.md T010
    description: Email-keyed-only throttle doesn't stop password spraying across many different emails.
    base_severity: critical
    effective_severity: critical
    recommended_action: Add a secondary IP-based or global limiter alongside the per-email one.
    execution_mode: selective
    status: resolved
    outcome: "T010 expanded to register two fixed-window limiter policies (per-email + per-IP/global). research.md and contracts/auth-api.md updated to describe and justify the two-layer design, including why a single global-only limiter was rejected (would let spray traffic throttle out a legitimate admin)."
  - finding_id: critic-003
    category: testing_strategy
    archetype_applicable: true
    location: tasks.md T008a
    description: No test verified the fail-fast startup behavior itself.
    base_severity: critical
    effective_severity: critical
    recommended_action: Add a startup-integration test asserting host startup fails when signing config is absent.
    execution_mode: auto
    status: resolved
    outcome: "T008a added directly before T009 in Phase 2, written test-first per Constitution Principle II; T009 now explicitly notes it depends on T008a and satisfies it."
  - finding_id: critic-004
    category: input_validation
    archetype_applicable: true
    location: contracts/auth-api.md; tasks.md T019a, T020
    description: No max length on the login password field — cheap DoS lever against PBKDF2 hashing cost.
    base_severity: high
    effective_severity: high
    recommended_action: Add a length cap before hashing.
    execution_mode: auto
    status: resolved
    outcome: "256-character cap documented in contracts/auth-api.md and data-model.md; T019a (test) and T020 (implementation) added/expanded in tasks.md; research.md records the decision and rationale."
  - finding_id: critic-005
    category: observability
    archetype_applicable: true
    location: tasks.md (was absent)
    description: No login success/failure signal in existing logs.
    base_severity: critical
    effective_severity: medium
    recommended_action: Add one structured log line on login success/failure.
    execution_mode: selective
    status: resolved
    outcome: "Folded into AuthEndpoints.cs scope alongside T022's logging-guard work (T019b/T022) — the same code path that asserts no body-logging occurs is the natural place to add a credential-free success/failure log line. Noted as an implementation detail of T022 rather than a separate task, consistent with Simplicity (Constitution Principle III): one task touching one file once, not two tasks touching the same file."
  - finding_id: critic-006
    category: secrets_handling
    archetype_applicable: true
    location: src/MakeBoldSpark.Api/Infrastructure/Observability/RequestLoggingMiddleware.cs (verified); tasks.md T019b, T022
    description: No regression guard asserting the login route's body is never captured by request logging (verified not currently leaking).
    base_severity: showstopper
    effective_severity: medium
    recommended_action: Add a test/comment asserting this invariant holds.
    execution_mode: selective
    status: resolved
    outcome: "T019b (test) added to Phase 3; T022 expanded to add the corresponding code comment. Cross-referenced with analyze-E5 (same underlying gap, analyze owns the coverage-gap framing, critic owns the production-risk framing — both point at the same two tasks)."
  - finding_id: critic-007
    category: data_loss_continuity
    archetype_applicable: true
    location: research.md#Bootstrapping-note; tasks.md T014
    description: No repeatable mechanism for provisioning a second administrator beyond the one-off T014.
    base_severity: high
    effective_severity: medium
    recommended_action: Turn T014 into a runbook entry.
    execution_mode: manual
    status: resolved
    outcome: "T014 expanded to require documenting the procedure as a repeatable runbook entry in quickstart.md rather than a one-off task description. research.md's Bootstrapping note updated to match."
  - finding_id: critic-008
    category: missing-risk-profile
    archetype_applicable: true
    location: spec.md frontmatter
    description: spec.md frontmatter omitted risk_profile/change_type/archetype.
    base_severity: high
    effective_severity: high
    recommended_action: Add risk_profile: internal to spec.md frontmatter.
    execution_mode: auto
    status: resolved
    outcome: "spec.md frontmatter now declares risk_profile: internal, change_type: brownfield, and archetype: web-service explicitly."
```

### Showstoppers

_None remaining — critic-001 resolved (see above)._

### Critical

_None remaining — critic-002 and critic-003 resolved (see above)._

### High

_None remaining — critic-004 and critic-008 resolved (see above)._

### Missing Critical Tasks

_None remaining._ The four gaps previously listed here (observability, deployment runbook, startup test, admin-bootstrap runbook) are now covered by T009a, T008a, T014, and the logging/observability work folded into T022.

### Questionable Assumptions

1. **"Author email addresses are already unique platform-wide"** → **Resolved**: T054a adds a unique database index enforcing this as an invariant rather than leaving it as an unverified assumption.

### Dependency Risk Assessment

| Dependency | Concern | Status |
|---|---|---|
| `Microsoft.AspNetCore.Identity` | Pulled in solely for `PasswordHasher<TUser>` | Accepted as-is — flagged as an available simplification (raw `Rfc2898DeriveBytes` would avoid the dependency entirely) but not required to change; the team may revisit at implementation time if dependency footprint becomes a concern. Not a blocking finding. |

### Estimated Technical Debt at Launch

- **Operational debt**: resolved — deployment runbook step now exists as T009a before, not after, first deploy.
- **Testing debt**: resolved — startup fail-fast path (T008a) and password-length boundary (T019a) both now have tests.
- **Documentation debt**: resolved — T014's bootstrap procedure is now scoped as a runbook entry, not a throwaway task description.

### Metrics

- Showstopper: 0, Critical: 0, High: 0, Medium: 0 (effective severity, post-remediation)
- Findings by category: all 8 categories resolved
- Missing operational tasks: 0

**VERDICT:** PROCEED

**Required Actions Before Implementation:**

None outstanding.

**Recommended Risk Mitigations:**

- Re-run `/devspark.critic` after `/devspark.implement` completes Phase 2 (Foundational) to confirm T008a/T009a/T010 were implemented as specified, since these are the highest-leverage tasks in the feature.
