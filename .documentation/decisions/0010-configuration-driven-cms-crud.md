# ADR 0010: Configuration-Driven CMS CRUD Interface

## Status

Accepted

## Context

The CMS manages eleven resources with the same list, view, create, edit, and
delete interaction pattern. Building separate screens for each resource would
duplicate behavior without addressing a distinct user need.

## Decision

Use one generic entity CRUD page configured by per-resource field and column
definitions. Add focused overrides only where resource semantics differ: menu
trees, append-only newsletters, and masked credential fields.

## Consequences

### Positive

- Common behavior is consistent across all CMS resources.
- New resource support is primarily declarative rather than a new screen.

### Negative

- The generic component must remain understandable as resource-specific
  exceptions accumulate.

## Source

- **Spec**: 004-cms-admin-app
- **Release**: v1.1.0
- **Date**: 2026-06-23
