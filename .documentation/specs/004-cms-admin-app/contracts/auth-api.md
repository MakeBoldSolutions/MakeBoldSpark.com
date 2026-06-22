# Contract: Sign-In Endpoint

**Route**: `POST /api/public/auth/login`
**Authorization**: Anonymous (see Constitution Waiver in `plan.md` — this is the one anonymous route that doesn't fit the existing read-only `/api/public/*` definition)
**OpenAPI tag**: `Auth: Sign-In` (new tag, added alongside the existing `MakeBoldSparkOpenApiTags` constants per Principle I — API-First)

## Request

```json
{
  "email": "string, required",
  "password": "string, required, max 256 characters"
}
```

A `password` exceeding 256 characters is rejected with the same generic `401` response defined below — never a distinct validation error — before any hashing is attempted (closes a denial-of-service lever; see `research.md`).

## Responses

| Status | Body | When |
|---|---|---|
| 200 | `{ "accessToken": "string", "expiresAt": "ISO-8601 string", "displayName": "string" }` | Email matches exactly one `Author` row with `isAdmin = true`, and the submitted password verifies against the stored hash |
| 401 | `{ "message": "Invalid email or password." }` | **Every other case** — unknown email, wrong password, `isAdmin = false`, or the request was throttled (FR-014). The response body, status code, and approximate timing MUST be indistinguishable across all of these cases (FR-001a, SC-006) |

No other status codes are used by this endpoint — in particular, no `429` is returned for throttled requests, since that status itself would be an observable signal that throttling occurred.

## Behavioral requirements (traced to spec)

- FR-001, FR-001a, FR-001b: verify `email` (unique match), `isAdmin = true`, and password hash; identical failure response for all rejection reasons.
- FR-001c: the returned `accessToken` is the only credential the rest of the application uses; there is no separate session concept on the server beyond JWT expiry.
- FR-014: **two** fixed-window rate limits apply to this route — one keyed by the submitted `email` value (deters repeated guessing against one account), and one keyed by client IP or a global partition (deters spraying across many different emails, which the email-keyed limiter alone does not slow). Either limiter being exceeded produces the identical generic `401` response — never a distinct `429`.
- No password length validation error is ever distinguishable from any other rejection reason (see Request section above).

## Non-goals (explicitly out of scope per spec.md)

- No password reset / forgot-password flow.
- No refresh tokens — re-authentication on expiry is a new call to this same endpoint.
- No MFA, no external identity provider redirect.
