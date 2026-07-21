---
feature: 0005-bold-api
tier: feature
status: draft
created: 2026-07-21
branch: 0005-bold-api
sources:
  - C:\GitHub\MakeBoldSolutions\bold\bold-docs\bold-desktop-hybrid-brief.md
  - C:\GitHub\MakeBoldSolutions\bold\bold-docs\bold-api-openapi.json
---

# Bold API — server-side model gateway (M1a)

**Tier**: feature
**Status**: Complete

## Product Owner TL;DR

Bold Desktop and the Bold CLI need a server that makes the OpenAI/Anthropic calls for
them, so no desktop install ever holds a provider API key. This feature implements that
server — the "Bold API" from the Bold Desktop Hybrid brief — inside the existing
MakeBoldSpark backend. A desktop client authenticates with a per-install token, asks for
a completion by *role* (planner, reviewer, router) rather than by model name, and the
server picks the provider/model, executes the call, validates any required JSON output,
retries when appropriate, and records what it cost. The desktop app also gets read
endpoints for provider health, current model routing, run history, and usage/cost
summaries. When this ships, the published OpenAPI contract is satisfied end-to-end and
the M1b CLI prototype can be built against a live service instead of a stub.

## Intent

Implement the Bold API contract (`bold-api-openapi.json`, v0.1.0) as a new feature area
of `MakeBoldSpark.Api`:

- **Gateway, not passthrough**: clients send a `model_role` (`router` | `planner` |
  `reviewer` | `embedding`); server-side configuration maps each role to a
  provider/model pair. Routing changes are a server config change, never a client
  release. Launch defaults (ratified 2026-07-21): `router` → OpenAI small/fast tier
  (gpt-5-mini class), `planner` → Anthropic Claude Sonnet (current release),
  `reviewer` → OpenAI gpt-5.1, `embedding` → unmapped while the endpoint is stubbed.
  Mixed across providers deliberately so both paths are exercised from day one; exact
  model IDs are pinned against provider docs at build time.
- **Cost estimation** (ratified 2026-07-21): a static per-model USD price table in
  configuration (input/output per-million-token rates, manually updated) drives
  `estimated_cost_usd` on runs and the monthly cost cap — no external pricing API.
- **Two providers**: OpenAI (Responses API) and Anthropic (Messages API), keys held in
  server configuration (Azure App Service settings; locally via user-secrets), never in
  source control (backbone X) and never returned to clients.
- **Provider integration style** (ratified 2026-07-21): no official provider SDKs.
  Thin typed clients per provider built on the
  [`WebSpark.HttpClientUtility`](https://www.nuget.org/packages/WebSpark.HttpClientUtility)
  NuGet package (project-owned) for the HttpClient plumbing, keeping error mapping
  (`retryable` classification), retry policy, and token-usage extraction under this
  feature's control and the dependency surface minimal (backbone III).
- **Structured output discipline**: when a request carries `response_format: "json"`
  and a `response_schema`, the server validates the provider's output against that
  schema and retries on failure; exhausted retries return `422` with a clear halt
  reason. Transient provider failures retry with backoff; hard failures return `502`.
- **Accountability**: every completion writes a run record (provider, model, role,
  workflow, workspace, token counts, estimated cost, status) to the relational store
  (EF Core + SQLite, backbone IX). `workspace_id` and `workflow` come from the
  completion request's optional `ClientMetadata` (`workflow` is the closed
  `plan`/`build`/`ship` enum); when omitted, the run record carries a null workspace/
  workflow and is excluded from workspace/workflow-scoped filters (analyze gap fix,
  2026-07-21). `/runs`, `/runs/{runId}`, and `/usage` are reporting
  views over those records, scoped to the authenticated install.
- **Per-install auth (O10)**: bearer tokens issued via admin-protected endpoints —
  `POST /api/admin/bold/tokens` (issue; plaintext token returned exactly once) and
  `DELETE /api/admin/bold/tokens/{id}` (revoke) — under the existing backbone VIII
  admin policy (ratified 2026-07-21: no local-script issuance, since production SQLite
  on Azure App Service is not reachable by ad-hoc scripts). Tokens validated on every
  non-health endpoint; run/usage data is partitioned per install.
  Install-token auth is a **dedicated authentication scheme + authorization policy**
  (`BoldInstallToken`), fully isolated from the existing admin scheme: an install
  token can never satisfy an admin policy, and admin credentials can never satisfy
  Bold client endpoints (critic blocker fix, 2026-07-21; backbone VIII).
- **Content privacy** (critic blocker fix, 2026-07-21): the server is a forwarder,
  not a store, of workspace content — matching the O6 privacy promise ("sent to Make
  Bold Spark, which forwards to the provider"). Run records and application logs
  persist **metadata only** (run/workspace ids, role, provider, model, token counts,
  estimated cost, status, retries, timing). Message bodies, model output, and
  provider API keys never reach the database or logs; the existing
  `RequestLoggingMiddleware` must not capture Bold request/response bodies.
- **Request bounds** (critic blocker fix, 2026-07-21; backbone IV): configuration-
  driven caps validated before any provider call, each returning `400` with the
  `ErrorResponse` shape when exceeded — max messages per request (default 50), max
  total request content size (default 400 KB), server-side ceiling on
  `max_output_tokens` (default 16,384; applied as the effective value when the client
  omits or exceeds it), and max `response_schema` size/nesting depth (default 64 KB /
  depth 10) to keep schema validation from being a DoS vector.
- **Provider call resilience** (critic note fix, 2026-07-21): every provider call has
  a config-driven per-attempt timeout (default 120 s) and a bounded retry budget
  (default 2 retries) covering both transient provider failures and JSON-schema
  validation failures; a hung provider can never hold a request open indefinitely.
- **Rate limiting & cost caps** (ratified 2026-07-21): two config-driven limits per
  install token — a request rate limit (default 30 completions/minute, `429` +
  `Retry-After`) and a monthly cost ceiling (default $50 USD/install, computed from
  recorded run costs; when exceeded, completions return `429` with
  `code: "cost_cap_exceeded"`, `retryable: false` until the month rolls over, while
  read endpoints keep working). Both values are configuration only — tightening them
  never requires a release. Accepted risk (critic note, 2026-07-21): the cap is a
  guardrail, not an invoice limit — concurrent in-flight completions may overshoot
  the ceiling by a request or two; no distributed locking is added for it.
- **Persistence conventions** (critic note fix, 2026-07-21): Bold entities join the
  existing `MakeBoldSparkDbContext` and its SQLite conventions (WAL journal mode) —
  no second context, no separate database file. The schema migration is additive
  only; rollback is redeploying the prior build with the new tables left in place.
- **Dependencies pinned** (critic note fix, 2026-07-21): `WebSpark.HttpClientUtility`
  (provider HTTP plumbing) and `JsonSchema.Net` (response-schema validation) are the
  only new packages, each pinned to an exact version in the csproj.

Alignment decisions this spec records (each also lands as a `system/decisions/` entry
during build):

1. **Repo placement — deviation from brief O12.** The brief proposed a separate repo;
   backbone VII (Single Backend Platform) governs this repo and says one modular
   ASP.NET Core app. The Bold API is implemented here as a feature folder +
   route group in `MakeBoldSpark.Api`. The brief's real requirement — standalone,
   contract-first, testable without the desktop/CLI — is preserved: nothing in the
   feature references Bold CLI/desktop code, and the contract file is the interface.
2. **Route taxonomy — backbone VIII.** The contract's paths are `/v1/*` on
   `api.makeboldspark.com`. Internally the endpoints mount under
   `/api/integrations/bold/v1/*` (service-token category). Public exposure as
   `https://api.makeboldspark.com/v1/*` is a hosting concern (host binding + rewrite),
   not a code concern; `/health` (shallow, anonymous) additionally maps onto the
   existing anonymous-health category. Final external URL wiring is deferred to M3
   deployment and does not block this feature.
3. **Second authentication scheme — extends ADR 0005.** ADR 0005 (Authentication
   Boundaries) registers JWT Bearer as the default scheme and anticipates "additional
   identity complexity...added later." `BoldInstallToken` (hashed-token lookup, not
   JWT) is that addition: a dedicated scheme + authorization policy, fully isolated
   from the existing admin JWT scheme (see Intent, Per-install auth). This is not a
   conflict with ADR 0005, but is recorded here because it changes the set of
   registered authentication schemes in `AuthorizationSetup.cs` (analyze note fix,
   2026-07-21).

## Out of scope

- `/embeddings` execution (ratified 2026-07-21) — contract-specified for Phase 4; this
  feature ships the endpoint returning a stable error (`code: "not_implemented"`,
  `retryable: false`) so the contract needs no breaking change later. No consumer
  exists before the reference-pack phase, which is gated behind the M3 exit.
- Self-service token issuance UI/endpoint — issuance is an admin operation.
- Desktop/CLI client code, local providers (Ollama/LM Studio), reference packs.
- Production deployment/DNS for `api.makeboldspark.com` (M3).

## Accepted risks & deferrals (critic notes, ratified 2026-07-21)

- **No run-history backup for MVP.** Cost accounting and the brief's cost-per-run
  kill criterion live in the `/home/data/` SQLite file with no backup; acceptable at
  one pilot install. Backup and run-record retention policy are M3 hardening
  decisions.
- **Observability is pull-only.** Provider outages and cap breaches surface via
  `GET /providers`, run records, and app logs — no alerting/push monitoring until M3.
- **Base-URL gap until M3.** Clients generated from `bold-api-openapi.json` default
  to `https://api.makeboldspark.com/v1`; until M3 hosting wires that host, the
  service answers at the App Service host under `/api/integrations/bold/v1`. M1b/M2
  clients must therefore support a configurable base URL — carried into the
  contract's consumer notes.

## Acceptance criteria

1. **Contract fidelity**: every path/operation in `bold-api-openapi.json` (except the
   deferred `/embeddings` execution) is implemented with request/response shapes that
   validate against the contract's schemas; the app's generated OpenAPI document for
   the Bold route group is diffable against the contract and has no breaking drift.
2. **Auth**: requests without a valid install token get `401` with the `ErrorResponse`
   shape on every endpoint except `GET /health`. Tokens are stored hashed;
   `POST /api/admin/bold/tokens` issues (plaintext returned once) and
   `DELETE /api/admin/bold/tokens/{id}` revokes, both behind the existing admin policy
   (backbone VIII); a revoked token immediately fails auth. Run and usage queries only
   ever return the authenticated install's data. Scheme isolation is proven by tests:
   a valid install token gets `401`/`403` on `/api/admin/*`, and admin credentials get
   `401` on Bold client endpoints.
3. **Completions**: `POST /completions` with a `model_role` resolves via server config
   to a provider/model, executes against the real provider SDK/HTTP API, and returns
   `run_id`, content, usage (input/output tokens, estimated cost), and `retries` (count
   of retry attempts made, not attempts remaining). Provider override honored when the
   named provider is configured. The request accepts an optional `ClientMetadata`
   object (`workspace_id`, `workflow`, `starter_id`, `run_label`) per the contract;
   `workflow` is the closed `plan`/`build`/`ship` enum. `workspace_id` and `workflow`
   are persisted onto the run record and are what `GET /runs`/`GET /usage` filter and
   group by (criterion 6) (analyze gap fix, 2026-07-21).
4. **JSON validation + retry**: with `response_format: "json"` + `response_schema`,
   invalid provider output triggers bounded retries; exhausted retries return `422`
   with `code: "schema_validation_failed"`; the run record shows
   `status: "retried"`/`"failed"` accordingly (analyze gap fix, 2026-07-21: `code`
   value made explicit).
5. **Failure semantics**: provider outage/invalid key/model unavailable returns `502`
   with `code`, human-readable `message`, and correct `retryable`; a provider call that
   exceeds its per-attempt timeout (default 120 s) is treated as a retryable failure and
   counted against the bounded retry budget (default 2 retries) — exhausting the budget
   returns `502` (analyze gap fix, 2026-07-21: Intent's provider-call-resilience bullet
   now has a testable criterion); malformed requests and requests exceeding the
   configured bounds (max messages, max request size, `max_output_tokens` ceiling, max
   schema size/depth) return `400` before any provider call is made; requests over the
   per-install rate limit (default 30/min) return `429` with `Retry-After`; completions
   past the monthly cost ceiling (default $50/install) return `429` with
   `code: "cost_cap_exceeded"`, `retryable: false`, while read endpoints remain
   available.
6. **Run history & usage**: every completion (success or failure) persists a run
   record; `GET /runs` supports `workspace_id`/`workflow`/`since`/`limit` filtering,
   `GET /runs/{runId}` returns one record or `404`; `GET /usage` aggregates totals and
   supports `group_by` day/provider/model_role/workflow. `GET /runs` implements keyset
   (cursor) pagination per the contract's `next_cursor`: pages beyond `limit` are
   reachable via the cursor, and the final page returns `next_cursor: null` (critic
   note fix, 2026-07-21). The cursor is an opaque token — clients pass it back
   unmodified and never parse or construct it (analyze gap fix, 2026-07-21). Run
   records contain metadata
   only — a test asserts that no message body or model output text is persisted to the
   database or written to logs during a completion (including the request-logging
   middleware path).
7. **Providers & routing visibility**: `GET /providers` reports live reachability +
   latency per configured provider without exposing credentials; each provider's
   reachability check itself is bounded by the same per-attempt timeout as a completion
   call (default 120 s), so a slow/unreachable provider cannot make `GET /providers`
   hang (analyze gap fix, 2026-07-21). `GET /model-roles` reflects current routing
   config; changing config changes the response without code changes.
8. **Health**: `GET /health` is anonymous, shallow, and reports `status`/`version`/
   `time` per the contract.
9. **Test-first (backbone II)**: integration tests cover all criteria above with
   provider HTTP mocked (no live keys in CI); at least one contract test asserts
   response-shape conformance per endpoint. All tests pass in CI.
10. **Zero secrets (backbone X)**: no provider key, token, or connection string with
    credentials appears in the repo; configuration binds from environment/app settings;
    SQLite files stay out of source control.

## Open questions (for `bold-plan-clarify`)

1. ~~Token issuance mechanics~~ — resolved 2026-07-21: admin-protected endpoints
   (see Intent / acceptance criterion 2).
2. ~~Rate-limit / cost-cap policy~~ — resolved 2026-07-21: 30 completions/min +
   $50/month per install, config-driven (see Intent / acceptance criterion 5).
3. ~~Embeddings~~ — resolved 2026-07-21: stub with stable `not_implemented` error
   (see Out of scope).
4. ~~Provider integration style~~ — resolved 2026-07-21 (human override): thin
   `HttpClient` clients built on the project-owned `WebSpark.HttpClientUtility`
   NuGet package (see Intent).
5. ~~Model-role → model defaults~~ — resolved 2026-07-21: mixed-provider config
   defaults (see Intent); exact model IDs pinned at build time.
6. ~~Estimated cost source~~ — resolved 2026-07-21: static per-model price table in
   configuration (see Intent).

## Tasks

Tests precede or accompany the implementation they verify (backbone II). AC = the
acceptance criterion each task traces to.

- [X] T001 [P] Pin `WebSpark.HttpClientUtility` and `JsonSchema.Net` at exact versions in `src/MakeBoldSpark.Api/MakeBoldSpark.Api.csproj` (AC 9, 10)
- [X] T002 [P] Add `BoldOptions` configuration (role→provider/model map with launch defaults, per-model price table, rate limit 30/min, cost cap $50/mo, request bounds, 120 s timeout / 2 retries) in `src/MakeBoldSpark.Api/Features/Bold/BoldOptions.cs`, bound from app settings with no secrets in repo (AC 3, 5, 7, 10)
- [X] T003 Add `BoldInstallToken` and `BoldRun` entities (metadata-only columns) in `src/MakeBoldSpark.Api/Infrastructure/Data/Entities/` and register them in `src/MakeBoldSpark.Api/Infrastructure/Data/MakeBoldSparkDbContext.cs` (AC 2, 6)
- [X] T004 Add additive EF migration for Bold tables in `src/MakeBoldSpark.Api/Migrations/` (AC 6)
- [X] T005 Write auth tests — missing/invalid/revoked token → `401` `ErrorResponse` on every Bold endpoint except health; install token → `401`/`403` on `/api/admin/*`; admin credentials → `401` on Bold endpoints; run/usage partitioning per install — in `tests/MakeBoldSpark.Api.Tests/Features/Bold/BoldAuthTests.cs` (AC 2)
- [X] T006 Implement `BoldInstallToken` authentication scheme handler (hashed-token lookup) and authorization policy in `src/MakeBoldSpark.Api/Features/Bold/Auth/InstallTokenAuthenticationHandler.cs`, registered in `src/MakeBoldSpark.Api/Infrastructure/Auth/AuthorizationSetup.cs` (AC 2)
- [X] T007 Implement admin issuance endpoints `POST /api/admin/bold/tokens` (plaintext once) and `DELETE /api/admin/bold/tokens/{id}` in `src/MakeBoldSpark.Api/Features/Bold/Auth/BoldTokenAdminEndpoints.cs` (AC 2)
- [X] T008 [P] Write contract-shape tests for `GET /health`, `GET /providers`, `GET /model-roles` in `tests/MakeBoldSpark.Api.Tests/Features/Bold/BoldStatusEndpointTests.cs` (AC 1, 7, 8)
- [X] T009 Implement health, providers (live reachability + latency, no credentials), and model-roles (config echo) endpoints in `src/MakeBoldSpark.Api/Features/Bold/Status/BoldStatusEndpoints.cs` (AC 7, 8)
- [X] T010 Implement thin provider clients on `WebSpark.HttpClientUtility` — OpenAI Responses in `src/MakeBoldSpark.Api/Features/Bold/Providers/OpenAiProviderClient.cs`, Anthropic Messages in `src/MakeBoldSpark.Api/Features/Bold/Providers/AnthropicProviderClient.cs` — with per-attempt timeout, `retryable` error classification, and token-usage extraction (AC 3, 5)
- [X] T011 Write completion tests with mocked provider HTTP (`HttpMessageHandler` fakes) — role routing, provider override, JSON-schema validation/retry/`422`, `502` mapping, request-bounds `400`, rate-limit `429` + `Retry-After`, cost-cap `429` `cost_cap_exceeded` — in `tests/MakeBoldSpark.Api.Tests/Features/Bold/BoldCompletionsTests.cs` (AC 3, 4, 5)
- [X] T012 Implement request-bounds validation (max messages, max request size, output-token ceiling, schema size/depth) in `src/MakeBoldSpark.Api/Features/Bold/Completions/CompletionRequestValidator.cs` (AC 5)
- [X] T013 Implement role routing + completion orchestrator (provider resolution, `JsonSchema.Net` output validation, bounded retry, error mapping) in `src/MakeBoldSpark.Api/Features/Bold/Completions/CompletionService.cs` (AC 3, 4, 5)
- [X] T014 Implement run recording (metadata only) and price-table cost estimation in `src/MakeBoldSpark.Api/Features/Bold/Runs/RunRecordingService.cs` (AC 6)
- [X] T015 Implement per-install rate limiter and monthly cost-cap check (reads recorded run costs; read endpoints unaffected) in `src/MakeBoldSpark.Api/Features/Bold/Completions/BoldLimitsFilter.cs` (AC 5)
- [X] T016 Implement `POST /completions` endpoint wiring validator → limits → orchestrator → run recording in `src/MakeBoldSpark.Api/Features/Bold/Completions/CompletionsEndpoints.cs` (AC 3, 4, 5)
- [X] T017 [P] Implement `POST /embeddings` stub returning stable `not_implemented`/`retryable: false` error in `src/MakeBoldSpark.Api/Features/Bold/Embeddings/EmbeddingsEndpoints.cs`, with test in `tests/MakeBoldSpark.Api.Tests/Features/Bold/BoldEmbeddingsTests.cs` (AC 1)
- [X] T018 Implement `GET /runs` (filters + keyset `next_cursor` pagination), `GET /runs/{runId}` (`404` on miss), `GET /usage` (`group_by` day/provider/model_role/workflow) in `src/MakeBoldSpark.Api/Features/Bold/Runs/RunsEndpoints.cs`, with tests in `tests/MakeBoldSpark.Api.Tests/Features/Bold/BoldRunsUsageTests.cs` (AC 6)
- [X] T019 Exclude Bold request/response bodies from `src/MakeBoldSpark.Api/Infrastructure/Observability/RequestLoggingMiddleware.cs` and add a test asserting no message/output text reaches the database or logs during a completion, in `tests/MakeBoldSpark.Api.Tests/Features/Bold/BoldContentPrivacyTests.cs` (AC 6, 10)
- [X] T020 Mount the Bold route group at `/api/integrations/bold/v1` with OpenAPI tags in `src/MakeBoldSpark.Api/Program.cs` and `src/MakeBoldSpark.Api/Infrastructure/OpenApi/MakeBoldSparkOpenApiTags.cs` (AC 1)
- [X] T021 Add a contract-fidelity test diffing the generated OpenAPI document for the Bold group against the contract (paths, operations, schema shapes; embeddings noted as stubbed) in `tests/MakeBoldSpark.Api.Tests/Features/Bold/BoldContractFidelityTests.cs`, with the contract snapshot committed at `bold-docs/features/0005-bold-api/contract/bold-api-openapi.json` (AC 1)
- [X] T022 [P] Write decision records `bold-docs/system/decisions/0012-bold-api-in-single-backend.md` (placement vs. brief O12), `bold-docs/system/decisions/0013-bold-route-taxonomy-mapping.md` (`/v1` contract ↔ `/api/integrations/bold/v1` internal, M3 host wiring), and `bold-docs/system/decisions/0014-bold-install-token-auth-scheme.md` (`BoldInstallToken` as a second, non-JWT authentication scheme extending ADR 0005) (AC 1, 2; backbone VII, VIII)
- [X] T023 Verify full suite green in CI (`dotnet test`) with providers mocked and no secrets required; confirm SQLite files and keys absent from the repo (AC 9, 10)

## Affected surface (expected)

- `src/MakeBoldSpark.Api/Features/Bold/**` — new: endpoints, provider adapters,
  role routing, schema validation/retry, token auth handler, run/usage services.
- `src/MakeBoldSpark.Api/Infrastructure/Data/**` — new entities
  (`BoldInstallToken`, `BoldRun`), DbContext registration, migration.
- `src/MakeBoldSpark.Api/Infrastructure/Auth/AuthorizationSetup.cs` — new policy for
  install-token bearer scheme.
- `src/MakeBoldSpark.Api/Program.cs` — route group + service registration.
- `tests/MakeBoldSpark.Api.Tests/Features/Bold/**` — new test suite.
- `bold-docs/system/decisions/0012-*.md`, `0013-*.md` — placement + route-taxonomy
  decisions (written during build).
