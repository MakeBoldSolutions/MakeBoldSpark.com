# Ship review — 0005-bold-api

## Product Owner TL;DR

All eight findings have been addressed in separate fix commits and validated locally. The full suite now passes 201 tests (22 added regressions). Provider content logging is disabled, SQLite date filters work, retry usage is accumulated, malformed messages return 400, OpenAPI includes request/response shapes, overrides select compatible models, body reads are bounded, and transient retries back off. The remediation is ready for a separate ship re-review; no PR or deployment was created. Family Memories remains unstarted.

Reviewed 2026-09-11 at commit `425a04c`, against freshly synchronized `origin/main`: 3 commits ahead, 0 behind. Tier: Feature. Review only; no implementation fixes applied.

## Original findings (resolved below)

### R1 — P1: Provider HTTP utility logs workspace content

Locations: `src/MakeBoldSpark.Api/Features/Bold/Providers/OpenAiProviderClient.cs:36`, `AnthropicProviderClient.cs:38`.

Both adapters construct `HttpRequestResultService` with the application's logger and pass the full request body through it. A mocked completion using the fictional prompt `Fictional probe` produced an Information-level `Created curl command` log containing that prompt and the complete outbound JSON. The inbound middleware's metadata-only logging does not protect this outbound path. This violates AC6/AC10 and the resolved privacy critic blocker (backbone IV/X). Disable or redact content logging at the actual HTTP utility boundary; capture all application logs in privacy regression tests, including failure paths. Existing privacy tests check a successful database row and a route helper, but never capture logs.

### R2 — P1: Date-filtered runs and usage return HTTP 500 on SQLite

Locations: `src/MakeBoldSpark.Api/Features/Bold/Runs/RunsEndpoints.cs:97`, `:145`.

The queries compare mapped `DateTimeOffset` columns inside EF predicates without a supported conversion. Both `GET /api/integrations/bold/v1/runs?since=2026-01-01T00:00:00Z` and `GET /api/integrations/bold/v1/usage?since=2026-01-01T00:00:00Z` returned 500 in the existing SQLite test host because EF could not translate the predicate. The `until` predicate has the same issue. AC6 requires these filters to work. Use a SQLite-compatible date representation/query strategy and test both range boundaries.

### R3 — P2: Retry accounting discards paid attempts

Location: `src/MakeBoldSpark.Api/Features/Bold/Completions/CompletionService.cs:100` and `:110`.

Only the last provider response's token counts reach the run record; a final provider exception records zero even after earlier completed attempts. A probe returning invalid JSON on three attempts, each reporting 100 input and 50 output tokens, returned 422 and recorded only 100/50 instead of 300/150. Usage and monthly cost enforcement therefore systematically undercount retries. This fails AC3/AC5/AC6. Accumulate known usage across all attempts and retain it on exhausted or failed runs.

### R4 — P2: Null message elements cause HTTP 500

Location: `src/MakeBoldSpark.Api/Features/Bold/Completions/CompletionRequestValidator.cs:40`.

The JSON deserializer permits null list elements, but validation dereferences `message.Role` without checking the element. A request containing `{"model_role":"router","messages":[null]}` reproduced HTTP 500. AC5 requires malformed input to return 400 with the error contract before provider execution. Validate each element before dereferencing it and cover the serialized request path.

### R5 — P2: Generated completion contract has no request body

Location: `src/MakeBoldSpark.Api/Features/Bold/Completions/CompletionsEndpoints.cs:17`.

The handler manually reads `HttpContext.Request.Body` and declares no request-body metadata. A probe of `/openapi/v1.json` confirmed that the completion POST operation has no `requestBody`. Generated clients cannot discover the required completion payload, contrary to AC1 and T021. Declare the request type/content type and expand contract tests to compare request and response schemas, rather than only paths and status codes. Status endpoints also use untyped `.Produces(200)` declarations that need schema coverage in that comparison.

### R6 — P2: Provider override retains the other provider's model

Location: `src/MakeBoldSpark.Api/Features/Bold/Completions/CompletionService.cs:47` and `:69`.

The public contract permits overriding the default provider, but the service changes only the client and keeps `mapping.Model`. With shipped defaults, `model_role: planner, provider: openai` sends `claude-sonnet-4-5` to OpenAI; the inverse sends an OpenAI model to Anthropic. This cannot implement the advertised override (AC1/AC3). Resolve a model appropriate to the selected provider, or explicitly revise the contract and validation rather than forwarding an incompatible pair. Confirmed by code inspection; no live provider request was made.

### R7 — P2: Request size cap is enforced after buffering and deserialization

Location: `src/MakeBoldSpark.Api/Features/Bold/Completions/CompletionsEndpoints.cs:46`.

The handler enables buffering, reads the entire request into a string, and deserializes it before enforcing the configured 400 KB limit. An authenticated caller can force substantially larger allocations and temporary-file buffering up to the host's separate limit, defeating the bounded-input protection recorded in the critic remediation and backbone IV. Enforce a bounded read, including requests without Content-Length, and map rejection to the specified 400 error envelope. Confirmed by control-flow inspection.

### R8 — P2: Transient retries have no backoff

Location: `src/MakeBoldSpark.Api/Features/Bold/Completions/CompletionService.cs:78`.

The transient-error branch immediately continues the loop. The spec explicitly requires backoff; immediate retries against a rate-limited or unavailable provider can exhaust the entire budget before recovery is possible. Add bounded, cancellation-aware backoff and verify it with a controllable clock/delay abstraction. Confirmed by control-flow inspection.

## Original review validation and coverage

- `dotnet test tests/MakeBoldSpark.Api.Tests/MakeBoldSpark.Api.Tests.csproj --verbosity minimal`: 179 passed, 0 failed, 0 skipped; tests reported 11 seconds. Build and static-site generation succeeded.
- Independent temporary console probes reused the repository's test host, SQLite in-memory databases, and mocked provider handlers. Reproduced R1–R5 without live provider calls or production database access. Probe files live outside the repository and are not committed tests.
- Fully inspected the changed auth/token lifecycle, policy registration, request validation, completion/provider execution, rate/cost enforcement, run persistence/scoping, entities, migration and generated model changes, logging, and host wiring. Reviewed remaining business logic for correctness and spot-checked contract, tests, configuration, package changes, and feature documentation.
- The build's npm audit reported six high-severity findings in the existing static-site dependency tree. This review did not investigate their provenance or attribute them to this branch.
- No production migration/deployment or live-provider smoke test was performed.

## Resolution record — 2026-09-11

| Finding | Fix commit | Regression evidence | Status |
|---|---|---|---|
| R1 | `7296b57` | Six privacy tests pass; new cases capture actual application logs and stored rows for success/failure on both providers. Four new cases failed before the fix. | Resolved |
| R2 | `4cfefc8` | Five runs/usage tests pass, including equal UTC instants with different offsets, inclusive since/until, keyset pagination, empty results, and install isolation. | Resolved |
| R3 | `b61fea3` | Three accounting cases verify accumulated usage on success, exhausted schema retries, and a later hard provider failure; accumulated cost blocks subsequent calls. | Resolved |
| R4 | `e495def` | Two serialized null-element cases return 400/invalid_request with no provider call. | Resolved |
| R5 | `df5f0d0` | Three contract tests pass, now checking request and response field names, nested DTO/array shapes, types, and date formats across non-deferred operations. | Resolved |
| R6 | `8f48fd4` | Four override cases pass; new cases inspect outbound model IDs for all three configured roles, not just the selected handler. | Resolved |
| R7 | `7dfc9da` | Five cases prove the reader stops at limit+1, rejects oversized bodies with/without Content-Length, and accepts exact-limit UTF-8 bodies. | Resolved |
| R8 | `cc319cc` | Three controlled-delay tests prove waiting between attempts, cancellation, five-second capping, and no retry for hard errors. | Resolved |

Final command: `dotnet test tests/MakeBoldSpark.Api.Tests/MakeBoldSpark.Api.Tests.csproj --verbosity minimal` — **201 passed, 0 failed, 0 skipped**, test duration 19 seconds. Build and static-site generation succeeded. `git diff --check` passed. No new database migration or package dependency is needed for these fixes.

Code fixes and this tracking update use disjoint file paths. All eight selected findings are resolved; none was deferred. This resolution record is validation of the fixes, not a replacement for an independent ship re-review. The schema test compares DTO shapes; arbitrary `response_schema` JSON is intentionally not treated as a fixed DTO, and this is not a complete OpenAPI semantic-equivalence proof.

## Handoff

Next command: `bold.ship review`, reviewing the remediation since `425a04c`, then `bold.ship` for the PR. No PR was present, so this work addressed the already-provided local feedback directly; no external review replies were posted.

The spec's YAML status now agrees with its displayed Complete status. The pre-existing uncommitted deletion of `.claude/worktrees/agent-a434ff3cafc80bc4a` and collector-generated run-log changes were preserved and excluded from fix/tracking commits. Include the intended worktree cleanup when packaging the branch. No branch switch occurred.
