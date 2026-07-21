# Critic report — 0005-bold-api

**Gate**: bold.plan critic
**Run**: 2026-07-21
**Spec**: ../spec.md (tier: feature, status: draft)

## Product Owner TL;DR

Before we build the Bold API, this is the "what will bite us in production" review.
Three things must be fixed in the spec before building: (1) the new install tokens and
the existing admin login use the same web app, and the spec doesn't yet say how we
guarantee an install token can never open an admin door; (2) the spec doesn't say that
the business content flowing through completions — Lesley's company plans — is never
written to our logs or database, and it must say so; (3) nothing limits how big a
single request can be, so one oversized prompt could burn real money before the
monthly cap notices. Nine smaller risks are noted with suggested handling — none block
the build, but each gets an explicit yes/no. Nothing here says the plan is wrong;
it says where the plan is silent in ways production won't forgive.

## Findings

### Blockers

- **[trust-boundaries] Install-token scheme isolation is unspecified** — resolved 2026-07-21 (spec fixed). Spec mounts
  Bold routes under `/api/integrations/bold/v1/*` (backbone VIII: "admin or service
  token") and adds admin-only issuance endpoints, but never states that install-token
  auth is a *distinct* authentication scheme + policy from the existing admin scheme.
  Ambient-authority risk: a mis-scoped default policy could let an install token reach
  admin surface or an admin cookie satisfy Bold endpoints. Backbone VIII is enforced →
  blocker. **Fix**: spec must require a dedicated scheme (e.g. `BoldInstallToken`) and
  policy, with a test proving an install token fails `/api/admin/*` and admin auth
  fails Bold endpoints. Traces to: Intent (per-install auth), acceptance criterion 2.
  **Resolved 2026-07-21**: spec now mandates the dedicated `BoldInstallToken`
  scheme/policy with bidirectional isolation tests (Intent + criterion 2).
- **[secrets/privacy] Prompt/response content retention is unspecified** — resolved 2026-07-21 (spec fixed). Completion
  requests carry the user's workspace content (the O6 privacy checkpoint promises
  "sent to Make Bold Spark, which forwards to the provider" — not "stored by Make Bold
  Spark"). The spec defines run records but never states that message bodies and model
  output are excluded from run records and from `RequestLoggingMiddleware`/app logs.
  Backbone IV enforced; a silent default here is a data breach in waiting. **Fix**:
  spec must state run records and logs persist metadata only (ids, role, model, token
  counts, cost, status, timing) — never message or response content, and never
  provider API keys. Traces to: Intent (accountability), acceptance criteria 6, 10.
  **Resolved 2026-07-21**: spec now has a Content Privacy clause (metadata-only
  persistence, logging middleware excluded from Bold bodies) and a persistence test
  requirement in criterion 6.
- **[input-validation] No request-size or schema-complexity bounds** — resolved 2026-07-21 (spec fixed). `messages` is
  unbounded in count/length, `response_schema` is arbitrary client-supplied JSON
  Schema, and `max_output_tokens` is nullable. 30 requests/min does not stop a single
  500k-character prompt; the monthly cap only reacts after spend. Malicious or buggy
  schema input can also DoS the validator (deep nesting, pathological patterns).
  Backbone IV enforced → blocker. **Fix**: spec adds explicit caps (max messages, max
  total request chars, server-side ceiling on `max_output_tokens`, max
  `response_schema` size/depth) returning `400` when exceeded. Traces to: acceptance
  criteria 3, 4, 5.
  **Resolved 2026-07-21**: spec now defines config-driven Request Bounds (50 messages,
  400 KB request, 16,384 output-token ceiling, 64 KB / depth-10 schema) enforced
  before any provider call, with `400` semantics folded into criterion 5.

## Notes (resolved 2026-07-21 — recommendations folded into spec.md: resilience
timeouts, cost-cap race acceptance, shared-DbContext/WAL convention, keyset
pagination in criterion 6, pinned `WebSpark.HttpClientUtility` + `JsonSchema.Net`,
and an "Accepted risks & deferrals" section covering backup, observability, and the
pre-M3 base-URL gap)

- **[error-handling] No provider timeout budget.** Retries are specified but no
  per-attempt timeout or total latency budget; a hung provider call holds the request
  open indefinitely. Suggest: per-attempt timeout (e.g. 120s), bounded total attempts
  (e.g. 2 retries), values in config. Traces to: Intent (structured output discipline).
- **[concurrency] Cost-cap check-then-execute race.** Two concurrent completions can
  both pass the cap check and overshoot the $50 ceiling by a request or two. Accept as
  MVP behavior (cap is a guardrail, not an invoice limit) — record the acceptance.
- **[concurrency] SQLite write contention.** Parallel completions all write run
  records to the shared SQLite file; ensure WAL mode / existing DbContext conventions
  are reused rather than a second context. Low risk given one pilot user.
- **[data-loss] Run history has no backup story.** Cost-cap accounting and the brief's
  cost-per-run kill criterion depend on run records in `/home/data/` SQLite; the spec
  is silent on backup. Acceptable for MVP (single pilot), but say so explicitly.
- **[scale] `/runs` cursor pagination.** Contract declares `next_cursor`; spec's
  criterion 6 lists filters but not cursor behavior. Small gap — cross-referenced for
  `bold.plan analyze`'s coverage lane; implement keyset pagination or return null
  cursor deliberately.
- **[observability] Failures are pull-only.** Provider outages/cap breaches are
  visible only via `/providers`, run records, and logs — no alerting. Acceptable for
  M1a (M3 owns the hardening milestone); record the deferral.
- **[deployment] Additive EF migration on a shared production DB.** New entities join
  the existing SQLite database used by recipe/CMS features; migration must be additive
  and rollback = redeploy prior build (schema left in place). Standard practice here —
  note only.
- **[supply-chain] Unpinned/unnamed dependencies.** `WebSpark.HttpClientUtility` is
  project-owned (low risk) but the spec names no version, and JSON-schema validation
  needs a library (e.g. `JsonSchema.Net`) the spec never names. Pin exact versions in
  the task pass.
- **[backward-compat] Contract base-URL gap until M3.** Generated clients from
  `bold-api-openapi.json` default to `https://api.makeboldspark.com/v1`, but until M3
  the service answers at the App Service host under `/api/integrations/bold/v1`.
  Already declared an M3 hosting concern in the spec — M1b clients must support a
  configurable base URL; carry this into the contract's consumer notes.
- **[privacy] Run-record retention unbounded.** Metadata-only records (per blocker 2's
  fix) still accumulate indefinitely with workspace ids. Acceptable for MVP; note that
  a retention decision belongs to M3 hardening.

## Inapplicable categories

- **Regulatory** (beyond the privacy items above): no regulated data classes (health,
  payments, minors) in scope for the pilot — evaluated, nothing further.

## Disposition

Human ratifies each blocker: fix the spec (recommended for all three), waive
(`## Waivers` in spec.md, format per `.bold/commands/WAIVERS.md`), or escalate.
Notes require acknowledgement only; accepted ones need no spec change unless stated.
