# Requirements checklist — 0005-bold-api

**Gate**: bold.plan checklist
**Run**: 2026-07-21
**Spec**: ../spec.md (tier: feature, status: draft)

Tests whether the spec's requirements are well-written — complete, unambiguous,
consistent, measurable — not whether the implementation works. Run `bold.plan checklist
--verify` after spec edits to check items off against the updated text.

## Completeness

- [X] CHK001 Is every field the completion request accepts explicitly listed, including
      correlation/metadata fields (`workspace_id`, `workflow`, `starter_id`,
      `run_label`) that downstream Acceptance Criteria (AC6) depend on? (AC3, spec.md;
      resolved 2026-07-21 — `ClientMetadata` fields now named)
- [X] CHK002 Is the `error.code` value specified for every distinct failure path,
      including the 422 schema-validation-exhausted case? (AC4, spec.md; resolved
      2026-07-21 — `schema_validation_failed`)
- [X] CHK003 Is the behavior of a provider call that exceeds its per-attempt timeout
      specified as an acceptance-testable outcome, not just an Intent-level commitment?
      (AC5, spec.md; resolved 2026-07-21)
- [X] CHK004 Are the exact model IDs behind each `model_role` default documented
      somewhere before build (even if pinned "at build time"), so the routing table
      isn't reverse-engineered from provider docs mid-implementation? (Intent,
      spec.md:38-42 — deliberate, documented deferral to build time; satisfied as a
      stated decision, not a gap)
- [X] CHK005 Is the `retries` field's semantics defined (count of retry attempts made,
      vs. attempts remaining) for the `POST /completions` response? (AC3, spec.md;
      resolved 2026-07-21)
- [X] CHK006 Is the authentication failure behavior specified for every route category
      (Bold client routes, admin token-issuance routes, health)? (AC2, spec.md:155-162)
- [X] CHK007 Is the token issuance/revocation contract (who calls it, what's returned,
      hashing) fully specified? (Intent "Per-install auth", spec.md:63-72)
- [X] CHK008 Are all out-of-scope items for this milestone explicitly listed, with a
      stated reason each is deferred rather than dropped? (Out of scope, spec.md:125-133)
- [X] CHK009 Are accepted risks/deferrals recorded with an explicit "who owns closing
      this" milestone (M3, etc.) rather than left open-ended? (Accepted risks &
      deferrals, spec.md:135-147)

## Clarity

- [X] CHK010 Are all numeric thresholds (rate limit, cost cap, request bounds, timeout,
      retry budget) given concrete default values rather than qualitative terms? (Intent,
      spec.md:80-99)
- [X] CHK011 Is "Gateway, not passthrough" backed by a concrete rule (role→provider/model
      config mapping) rather than left as a slogan? (Intent, spec.md:35-42)
- [X] CHK012 Is the pagination cursor's opacity/format contract stated precisely enough
      that a client implementer knows not to parse it? (AC6, spec.md; resolved
      2026-07-21 — cursor stated opaque)
- [X] CHK013 Are the request-bounds defaults (50 messages, 400 KB, 16,384 tokens, 64 KB /
      depth 10) each independently overridable via configuration, or is that ambiguous?
      (Intent "Request bounds", spec.md:80-86)

## Consistency

- [X] CHK014 Do the Acceptance Criteria and the Intent section agree on which status
      codes map to which failure conditions (`400`/`401`/`402`/`422`/`429`/`502`)? (AC5,
      spec.md:170-177; Intent, spec.md:55-99)
- [X] CHK015 Does every Intent bullet trace to at least one Acceptance Criterion, and
      vice versa? (Intent vs. Acceptance criteria; resolved 2026-07-21 — see CHK003)
- [X] CHK016 Do the alignment decisions recorded in the spec (repo placement, route
      taxonomy) agree with the backbone principles they cite (VII, VIII)? (spec.md:108-124)
- [X] CHK017 Does the spec state whether the new `BoldInstallToken` authentication
      scheme is recorded as a system decision, consistent with how the other two
      deviations are handled? (spec.md; resolved 2026-07-21 — added as alignment
      decision #3, landing as `system/decisions/0014-*.md` per T022)

## Measurability

- [X] CHK018 Can "contract fidelity" (AC1) be objectively verified — is there a
      concrete mechanism named (diffable generated OpenAPI doc) rather than a subjective
      judgment call? (AC1, spec.md:151-154)
- [X] CHK019 Can "content privacy" (no message body/output persisted) be objectively
      verified — is a specific test assertion named? (AC6, spec.md:184-187)
- [X] CHK020 Are the cost-cap and rate-limit thresholds stated as exact numbers with
      exact response codes/fields, rather than "reasonable limits"? (AC5, spec.md:170-177)

## Coverage (edge cases & non-functional attributes)

- [X] CHK021 Is the revoked-token-immediately-fails-auth edge case covered? (AC2,
      spec.md:159)
- [X] CHK022 Is the cost-cap-exceeded-while-read-endpoints-still-work edge case covered?
      (AC5, spec.md:175-177)
- [X] CHK023 Is the final-page-of-pagination edge case (`next_cursor: null`) covered?
      (AC6, spec.md:181-183)
- [X] CHK024 Is the concurrent-completions-overshooting-the-cost-cap race condition
      addressed, with an explicit accepted-risk statement rather than silence? (Intent,
      spec.md:97-99)
- [X] CHK025 Is there a stated non-functional requirement for the `/providers`
      reachability check's own latency/timeout, so a slow/unreachable provider doesn't
      make `GET /providers` itself hang? (AC7, spec.md; resolved 2026-07-21 — bounded
      by the same per-attempt timeout)
- [X] CHK026 Is the zero-secrets requirement covered for both source control and
      runtime logs (not just config files)? (AC10, backbone X; spec.md:73-79, 197-199)

## Disposition

All items resolved 2026-07-21 in spec.md. Human ratified: add a third alignment
decision for `BoldInstallToken` (CHK017), landing as
`system/decisions/0014-bold-install-token-auth-scheme.md` per T022.
