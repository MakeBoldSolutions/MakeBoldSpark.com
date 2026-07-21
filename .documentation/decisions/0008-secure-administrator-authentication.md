# ADR 0008: Secure Administrator Authentication

## Status

Accepted

## Context

The CMS administration interface requires a real authentication boundary. The
existing API accepted any well-formed token whenever no JWT authority was
configured, so a login screen alone would not protect administrative actions.

## Decision

Add a public login endpoint that verifies an administrator's email and
password, issues a JWT signed by a server-held signing key, and require
signature validation for protected routes. Hash passwords with ASP.NET Core's
`PasswordHasher<TUser>`, enforce unique author email addresses in the database,
and apply email- and client-scoped login throttling that returns the same
generic failure response as an invalid login.

## Consequences

### Positive

- Administrative access has a verifiable identity boundary.
- Passwords are not stored or compared in plain text.
- Generic failures and throttling reduce account discovery and guessing risks.

### Negative

- Every deployment must provide either a JWT signing key or a JWT authority.
- Existing seeded administrators require credential bootstrapping before sign-in.

## Source

- **Spec**: 004-cms-admin-app
- **Release**: v1.1.0
- **Date**: 2026-06-23
