# ADR 0012: Bold API Lives in the Single MakeBoldSpark Backend

## Status

Accepted

## Context

The Bold Desktop Hybrid brief (O12) specifies a standalone Bold API service — a server-side
gateway that holds provider (OpenAI/Anthropic) credentials so Bold Desktop and the Bold CLI never
do. The brief's proposal was a separate repository/service. Backbone VII (Single Backend Platform)
governs this repository and requires one modular ASP.NET Core application, with multiple small
APIs hosted under it via route groups and feature folders; splitting into a separate service/repo
requires documented justification (scale, security, release cadence, or reliability), none of
which apply at the M1a pilot stage.

## Decision

Implement the Bold API as a new feature area — `src/MakeBoldSpark.Api/Features/Bold/**` — inside
the existing `MakeBoldSpark.Api` project, following the same feature-folder + route-group pattern
already used by Recipe, Auth, and Health. The brief's real requirement — a standalone,
contract-first surface that is testable without the desktop/CLI — is preserved: nothing in the
Bold feature folder references Bold CLI/desktop code, and `bold-api-openapi.json` (committed as
the contract snapshot) is the interface boundary, not the process boundary.

## Consequences

One deployable, one database file, one auth pipeline to operate — consistent with backbone VII and
IX. If Bold API traffic, security isolation, or release cadence needs later diverge sharply from
the rest of MakeBoldSpark (e.g. it needs independent scaling or a separate compliance boundary),
extracting it into its own service remains possible because the feature folder has no inbound
dependencies from the rest of the app — only Program.cs, `AuthorizationSetup.cs`, and
`MakeBoldSparkDbContext` reference it, and all three are additive registrations that could be
lifted out. That extraction is deferred until a documented justification exists (spec.md M3+).
