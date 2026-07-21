# ADR 0011: Shared Brand Assets for CMS

## Status

Accepted

## Context

The API host already serves MakeBold brand CSS, fonts, and logos used by its
public pages. Duplicating or re-deriving those assets for the CMS would create
a second source of truth.

## Decision

Have the same-origin CMS SPA reference the existing shared brand assets via
absolute paths rather than copying them into the CMS project.

## Consequences

### Positive

- The administrative surface remains visually consistent with the platform.
- Brand asset maintenance has one source of truth.

### Negative

- The CMS depends on the host retaining those asset paths.

## Source

- **Spec**: 004-cms-admin-app
- **Release**: v1.1.0
- **Date**: 2026-06-23
