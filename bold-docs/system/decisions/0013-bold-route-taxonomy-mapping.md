# ADR 0013: Bold Contract Path Mapping to the Internal Route Taxonomy

## Status

Accepted

## Context

`bold-api-openapi.json` (v0.1.0) declares its paths relative to `https://api.makeboldspark.com/v1`
(e.g. `/health`, `/completions`, `/runs/{runId}`) — a dedicated public host. Backbone VIII (Clear
Authorization Boundaries) defines this repository's route taxonomy: `/api/public/*` anonymous,
`/api/admin/*` admin-only, `/api/publish/*` publisher/admin, `/api/integrations/*` admin or
service token, `/api/health` anonymous shallow health. The Bold API is a service-token-authenticated
integration surface, and the dedicated `api.makeboldspark.com` host is not wired up until M3
deployment (spec.md Accepted risks: Base-URL gap until M3).

## Decision

Mount every Bold contract path under `/api/integrations/bold/v1/*` inside the existing app —
`/health` becomes `/api/integrations/bold/v1/health`, `/runs/{runId}` becomes
`/api/integrations/bold/v1/runs/{runId}`, and so on, with the path segment and HTTP method
otherwise unchanged. `/health` is additionally anonymous (`AllowAnonymous()`), matching both the
contract's empty `security: []` for that operation and the existing anonymous-health category.
Token issuance (`POST`/`DELETE /tokens`) is a separate admin operation and mounts under the
existing `/api/admin/bold/tokens` admin category, not under the `/v1` prefix — it is not part of
the desktop/CLI-facing contract. Final external URL wiring (rewriting `/api/integrations/bold/v1/*`
to `https://api.makeboldspark.com/v1/*`, or binding a dedicated host) is a hosting concern deferred
to M3 and does not block this feature; `tests/MakeBoldSpark.Api.Tests/Features/Bold/BoldContractFidelityTests.cs`
enforces that every contract path/operation exists at its mapped internal path today.

## Consequences

Bold Desktop/CLI clients built against the published contract must support a configurable base URL
until M3 rewires the public host — already called out as a required consumer behavior in spec.md's
Accepted risks. Internally, the mapping is mechanical (prefix + same path), so no route ever needs
to diverge from the contract's path/method shape, keeping the contract-fidelity test meaningful.
