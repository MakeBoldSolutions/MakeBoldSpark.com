# Tracking: Constitution Amendment for Public Authentication Token Issuance

## Status

Ratified — Principle VIII was amended in Constitution v1.2.0 on 2026-06-23.

## Trigger

The CMS Admin App adds `POST /api/public/auth/login`. It verifies supplied administrator
credentials and issues a signed access token, but it is neither a content read nor a CMS-data
write. The current Constitution Principle VIII table defines `/api/public/*` as anonymous,
read-only routes, so the feature records a controlled waiver in
`004-cms-admin-app/plan.md`.

## Resolution

The owner ratified an explicit `/api/public/auth/*` authorization category in Principle VIII.
It permits anonymous credential verification and signed-token issuance only; it does not permit
CMS-data disclosure or CMS-data writes. The CMS login contract supplies the required generic
failures, bounded input, signed tokens, and throttling.

## Source

- Feature: `004-cms-admin-app`
- Task: `T054`
- Date: 2026-06-22
