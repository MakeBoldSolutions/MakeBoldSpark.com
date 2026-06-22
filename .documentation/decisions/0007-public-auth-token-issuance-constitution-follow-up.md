# Tracking: Constitution Amendment for Public Authentication Token Issuance

## Status

Proposed — requires human ratification.

## Trigger

The CMS Admin App adds `POST /api/public/auth/login`. It verifies supplied administrator
credentials and issues a signed access token, but it is neither a content read nor a CMS-data
write. The current Constitution Principle VIII table defines `/api/public/*` as anonymous,
read-only routes, so the feature records a controlled waiver in
`004-cms-admin-app/plan.md`.

## Required Follow-up

Run `/devspark.constitution` with the project owner to consider adding an explicit
`/api/public/auth/*` authorization category to Principle VIII. The amendment must define:

- anonymous access only for credential verification and token issuance;
- no CMS data disclosure or CMS-data writes from this route area; and
- security controls equivalent to the CMS login contract: generic failures, bounded input,
  signed tokens, and throttling.

Until the owner ratifies an amendment, the waiver in the CMS Admin App plan remains the
authoritative record. This tracking note does not itself amend the Constitution.

## Source

- Feature: `004-cms-admin-app`
- Task: `T054`
- Date: 2026-06-22
