# Gate Remediation Plan: Recipe Maintenance Application

**Created**: 2026-06-23
**Source gates**: [analyze.md](gates/analyze.md), [critic.md](gates/critic.md)
**Current gate state**: Analyze = pass; Critic = pass

## Objective

Resolve every persisted analyze and critic finding before implementation begins. Artifact-level controls are incorporated into the specification, plan, contract, and task list; the pre-existing public-detail authorization defect is corrected in source and covered by a regression test.

## Execution Record

- Added `archetype: web-service`, `risk_profile: internal`, and `change_type: brownfield` to the specification metadata.
- Defined a protected configured-domain inventory, input/body bounds, atomic category-delete conflict semantics, readiness/telemetry, migration rehearsal, contract validation, and dependency-audit controls.
- Removed duplicate hosting ownership and conflicting parallel task markers.
- Fixed anonymous access to unapproved recipe details in `RecipeService.GetRecipeById` and added a passing regression test.
- Re-ran the analyze and critic design gates; both now pass. Remaining controls are explicit implementation tasks and must be re-gated after implementation.

## Resolution Order

1. Close the public-content confidentiality defect and lock down all write boundaries.
2. Make data mutation and migration behavior safe under concurrency and deployment failure.
3. Define domain discovery and runtime-contract validation so the client can be built deterministically.
4. Add operational, supply-chain, and measurable acceptance controls.
5. Repair task ownership/parallelism and re-run the gates.

## Remediation Work

### R1 — Security and Public-Content Boundary (blocking)

**Addresses**: `critic-public-unapproved-recipe-exposure`, `critic-unbounded-maintenance-input`

1. Update `plan.md`, `data-model.md`, and `contracts/recipe-maintenance.yaml` to state that anonymous recipe detail reads apply the same approved-only filter as anonymous recipe lists; publisher detail reads remain separate and domain-scoped.
2. Add explicit server request-body, ingredients, and instructions size limits to the contract and plan. Define consistent validation-problem responses for oversized or invalid fields.
3. Revise `tasks.md` to add API tests that prove:
   - anonymous requests receive 404 for an unapproved recipe;
   - a publisher receives 200 for that record only in its active domain;
   - oversized JSON/body and text fields are rejected without persistence.
4. Add implementation tasks to filter the existing public detail service, add validated API write DTOs, configure request limits, and verify logs do not record rejected payload contents.

**Exit criteria**: Public endpoint behavior is documented and regression-tested; all publisher inputs have defined body/field limits and tested error responses.

### R2 — Atomic Concurrency and Data Integrity (blocking)

**Addresses**: `critic-category-delete-race`

1. Update `research.md` and `data-model.md` to require the category in-use check and deletion to occur atomically, including the database-restrict violation fallback.
2. Update `contracts/recipe-maintenance.yaml` to define a stable `409` problem response for both an observed in-use category and a concurrent reference created during deletion.
3. Revise `tasks.md` to require a transaction or database-exception mapping in the Recipe service/repository layer and an integration test that races category deletion against recipe assignment.

**Exit criteria**: A category delete either completes with its matching version or returns 409/412; it never returns an unhandled 500 or leaves partial data.

### R3 — Domain Discovery and Contract Determinism

**Addresses**: `analyze-domain-discovery-contract`, `critic-contract-drift-coverage`

1. Choose and document the configured-domain source. Preferred: add a publisher-authorized domain inventory endpoint to `recipe-maintenance.yaml` that returns only the domain ID and display data required for selection, rather than relying on an undocumented public CMS dependency.
2. Specify its Publisher authorization, ordering, empty-state response, and relation to the selected-domain query parameter.
3. Add contract-validation tasks that compare the generated runtime OpenAPI document and documented publisher operations, parameters, security requirements, request schemas, and every documented 4xx/412 response.
4. Add client and endpoint tests that prove a selected domain is loaded from the documented source and sent to every maintenance request.

**Exit criteria**: T018 has an explicit, protected source contract; all Recipe Maintenance contract operations have runtime/status coverage.

### R4 — Operations, Release Continuity, and Supply Chain

**Addresses**: `critic-observability-readiness-gap`, `critic-migration-release-continuity`, `critic-dependency-audit-gap`, `analyze-performance-success-coverage`

1. Extend `plan.md` with distinct liveness and readiness expectations. Readiness must verify the Recipe database/migration dependency without exposing sensitive connection details.
2. Define endpoint-level request count, error count, and latency telemetry for publisher operations plus alert thresholds. Include correlation IDs and redaction requirements in the validation plan.
3. Add an executable release sequence: verify a current persistent-data backup, rehearse migration against a database copy, apply the migration, run integrity checks, smoke-test `/recipes` and protected endpoints, and use a documented forward-fix procedure if validation fails.
4. Add a performance acceptance harness that records page-load and valid-save duration against the 5-second success criterion.
5. Add repeatable NuGet and npm dependency vulnerability audits to CI/final validation, using lockfile-based restores.

**Exit criteria**: The plan/tasks contain actionable readiness, telemetry, alert, backup, migration rehearsal, performance, and supply-chain validation steps—not documentation-only placeholders.

### R5 — Artifact Metadata and Task-Plan Repair

**Addresses**: `critic-missing-archetype-metadata`, `critic-missing-risk-profile-metadata`, `critic-missing-change-type-metadata`, `analyze-hosting-task-duplication`, `analyze-parallel-file-contention`

1. Add authoritative feature frontmatter metadata to `spec.md`:

   ```yaml
   archetype: web-service
   risk_profile: internal
   change_type: brownfield
   ```

2. Update `plan.md` to retain those risk decisions and their consequences for regression, release, and operational checks.
3. Split T002 into reference/mapping setup only and retain all extension/Program implementation in T022, or merge them into one ordered task. Do not leave duplicate ownership.
4. Remove `[P]` from T005/T006 and T031/T032, or split each into separate test files so parallel workers do not edit the same file.
5. Add the new security, atomicity, contract, operations, release, supply-chain, and performance tasks with sequential IDs; update the dependency graph and final validation phase.

**Exit criteria**: Frontmatter drives stable risk classification; every task has one owner/file set; all `[P]` tasks are genuinely conflict-free.

## Finding-to-Remediation Matrix

| Finding ID | Severity | Remediation | Completion evidence |
| --- | --- | --- | --- |
| analyze-domain-discovery-contract | High | R3 | Protected domain source and client/endpoint tests in contract/tasks. |
| analyze-hosting-task-duplication | Medium | R5 | One non-overlapping hosting implementation path. |
| analyze-parallel-file-contention | Medium | R5 | Removed markers or independently owned test files. |
| analyze-performance-success-coverage | Medium | R4 | Timed acceptance harness for the five-second outcome. |
| critic-public-unapproved-recipe-exposure | Showstopper | R1 | Anonymous-unapproved 404 regression test passes. |
| critic-unbounded-maintenance-input | Critical | R1 | Enforced bounds and oversized-payload tests pass. |
| critic-category-delete-race | Critical | R2 | Concurrent deletion/assignment test returns 409/412, never 500. |
| critic-observability-readiness-gap | Critical | R4 | Readiness, metrics, correlation, and alert validation documented/tested. |
| critic-migration-release-continuity | Critical | R4 | Backup/rehearsal/integrity/forward-fix release gate passes. |
| critic-contract-drift-coverage | High | R3 | Runtime OpenAPI/status contract test passes. |
| critic-dependency-audit-gap | High | R4 | NuGet/npm audit commands run in CI/final validation. |
| critic-missing-archetype-metadata | High | R5 | `archetype: web-service` in spec frontmatter. |
| critic-missing-risk-profile-metadata | High | R5 | `risk_profile: internal` in spec frontmatter. |
| critic-missing-change-type-metadata | High | R5 | `change_type: brownfield` in spec frontmatter. |

## Suggested Execution Sequence

1. Run a focused `/devspark.plan` revision for R1-R5 and update the OpenAPI contract and model documents.
2. Run `/devspark.tasks` to regenerate the task list with the added test-first and release-safety work.
3. Re-run `/devspark.analyze`; target zero High/Critical coverage or task-order findings.
4. Re-run `/devspark.critic`; target no Showstoppers or Critical findings. High findings require either remediation or an explicit gate acknowledgement.
5. Only then start `/devspark.implement`.
