# ADR 0009: Embedded CMS SPA as an In-Solution Project

## Status

Accepted

## Context

The CMS needs a browser-based administration surface but has only one host
application. Publishing it as a reusable package would add distribution and
versioning work without serving another consumer.

## Decision

Create `MakeBoldSpark.Cms` as an in-solution project. Build its React SPA with
MSBuild, embed the assets as manifest resources, reference the project from
`MakeBoldSpark.Api`, and serve the CMS from the API host.

## Consequences

### Positive

- The CMS is deployed with its only consumer in one unit.
- Generated SPA artifacts stay out of source-controlled `wwwroot` content.

### Negative

- API builds depend on the pinned Node/npm toolchain.
- The CMS cannot be independently released without revisiting this boundary.

## Source

- **Spec**: 004-cms-admin-app
- **Release**: v1.1.0
- **Date**: 2026-06-23
