# ADR 0014: `BoldInstallToken` as a Second, Isolated Authentication Scheme

## Status

Accepted

## Context

ADR 0005 (Authentication Boundaries) registers JWT Bearer as the default authentication scheme for
admin/publishing/backup/integration routes and anticipates "additional identity complexity...added
later." The Bold API needs per-install bearer tokens (spec.md O10): a desktop/CLI install
authenticates with a token issued out of a database lookup, not a signed JWT, and — critically —
that token must never be usable against `/api/admin/*`, and an admin JWT must never be usable
against Bold client endpoints (spec.md AC2, critic blocker fix 2026-07-21).

## Decision

Register `BoldInstallToken` as a second authentication scheme in
`src/MakeBoldSpark.Api/Infrastructure/Auth/AuthorizationSetup.cs`, fully isolated from the existing
`JwtBearer` scheme:

- `InstallTokenAuthenticationHandler` (`src/MakeBoldSpark.Api/Features/Bold/Auth/InstallTokenAuthenticationHandler.cs`)
  authenticates by SHA-256-hashing the bearer token and looking it up against
  `BoldInstallTokens.TokenHash` — it never parses or validates a JWT.
- A dedicated `BoldInstallToken` authorization policy requests only that scheme
  (`policy.AddAuthenticationSchemes(BoldInstallTokenDefaults.AuthenticationScheme)`), and every
  Bold client endpoint requires that policy.
- `AdminOnly`, `Publisher`, and `ServiceOrAdmin` are left unchanged — they still resolve against
  the default scheme (`JwtBearer`), so they never invoke `BoldInstallToken`'s lookup at all.

The isolation is structural, not incidental: an install token sent to an admin endpoint is simply
an invalid JWT to `JwtBearer` (401); an admin JWT sent to a Bold endpoint is simply a hash lookup
miss to `InstallTokenAuthenticationHandler` (401). `tests/MakeBoldSpark.Api.Tests/Features/Bold/BoldAuthTests.cs`
proves both directions, plus revoked-token-immediately-fails-auth and per-install run/usage
partitioning.

## Consequences

This changes the set of registered authentication schemes in `AuthorizationSetup.cs` (analyze note
2026-07-21) but does not touch ADR 0005's default-scheme behavior for any existing route. Adding a
third scheme in the future (e.g. an OIDC provider) follows the same pattern: register it, give it
its own policy, and never let existing policies request it implicitly.
