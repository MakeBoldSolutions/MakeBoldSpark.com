# Analyze report — 0005-bold-api

**Gate**: bold.plan analyze
**Run**: 2026-07-21
**Spec**: ../spec.md (tier: feature, status: draft)

## Product Owner TL;DR

This is a neutral alignment check, not a risk review (that's critic, already clean).
Three gaps were found and fixed in the spec 2026-07-21: the completions request now
states how a run gets tagged with `workspace_id`/`workflow`; the "provider is slow"
resilience behavior (120s timeout, 2 retries) is now a testable Acceptance Criterion
instead of just an Intent-level promise; and the 422 (schema-validation-exhausted)
response now names its `error.code`. One informational note remains open for a human
decision: whether adding the `BoldInstallToken` auth scheme should be recorded as a
third alignment decision alongside the spec's other two deviations — doesn't block the
build tier.

## Findings

- **[coverage-gap] `workspace_id`/`workflow` never specified as completion request input** — resolved 2026-07-21 (spec fixed). **AC3** (Completions, spec.md:163-166)
  only described the request as carrying `model_role` (and an optional provider
  override), while **AC6** (Run history & usage) and Intent's Accountability bullet
  required `workspace_id`/`workflow` on the run record with no stated request-side
  source. **Resolved 2026-07-21**: AC3 now states the request accepts an optional
  `ClientMetadata` object (`workspace_id`, `workflow`, `starter_id`, `run_label`),
  `workflow` is the closed `plan`/`build`/`ship` enum, and the Accountability bullet
  states the null-workspace/workflow behavior when the fields are omitted.
- **[coverage-gap] Provider-call resilience has no Acceptance Criterion** — resolved
  2026-07-21 (spec fixed). Intent's "Provider call resilience" bullet (per-attempt
  timeout, bounded retry budget) had no corresponding testable criterion. **Resolved
  2026-07-21**: AC5 now states a provider call exceeding its per-attempt timeout
  (default 120 s) is a retryable failure counted against the retry budget (default 2
  retries), exhausting to `502`.
- **[underspecification] 422 response has no `code` value** — resolved 2026-07-21
  (spec fixed). AC4 specified `422` on exhausted retries but never named the
  `error.code` value, unlike every other failure path in the spec. **Resolved
  2026-07-21**: AC4 now specifies `code: "schema_validation_failed"`.

## Notes (informational — not a blocking finding)

- **[system-consistency] Second auth scheme not recorded as an alignment decision** —
  resolved 2026-07-21 (spec fixed, human-ratified). ADR 0005 (Authentication
  Boundaries) registers JWT Bearer as the default scheme with room for "additional
  identity complexity...added later"; `BoldInstallToken` is that addition and was not a
  conflict, but the spec's alignment-decisions list omitted it. **Resolved 2026-07-21**:
  human chose to add a third alignment decision; spec.md now records it, landing as
  `system/decisions/0014-bold-install-token-auth-scheme.md` per T022.

## Checks run (no further findings)

- **Duplication** — no near-duplicate Acceptance Criteria found.
- **Ambiguity (wording)** — no unresolved TODO/TBD/placeholder text; the "Open questions"
  section shows all six items resolved with strikethrough. No vague adjective lacks a
  measurable criterion once contract cross-references are followed (`human-readable
  message` and `clear halt reason` both resolve to the contract's plain-string
  `ErrorResponse.error.message` field, so they're descriptive, not underspecified).
- **Backbone consistency** — no conflicts with `enforced` principles I–IV, VII–X;
  principle VIII's `/api/integrations/*` (admin or service token) and `/api/admin/*`
  categories are both used correctly for the Bold client routes and the token-issuance
  routes respectively.
- **System consistency (remaining)** — no conflicts against `system/architecture.md` or
  the other decision records; ADR 0001 (single backend) and the spec's own alignment
  decision #1 agree; ADR 0007/0008's stale `004-cms-admin-app/plan.md` reference is
  unrelated to this feature (already tracked in `stale_references`, owned by
  `bold.ship harvest`).

## Disposition

All three findings resolved in spec.md 2026-07-21. The informational note (auth-scheme
alignment decision) is pending a human decision — see gates/checklist.md CHK017 and the
question raised alongside this run.
