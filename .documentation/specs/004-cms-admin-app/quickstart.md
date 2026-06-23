# Quickstart: CMS Admin App

## One-time setup (per environment)

1. Generate a signing key and set `Jwt:SigningKey`:
   - Local dev: `dotnet user-secrets set "Jwt:SigningKey" "<random-256-bit-value>" --project src/MakeBoldSpark.Api`
   - Deployed: set `Jwt__SigningKey` as an Azure App Service application setting (never commit it to `appsettings*.json`, per Constitution Principle X)
2. **Bootstrap an administrator credential** (required — seed data currently stores `Author.password` as an empty string, which will never verify against any submitted password once hashing is wired in; the same command provisions any future second administrator, per gate finding critic-007):

   ```text
   dotnet run --project src/MakeBoldSpark.Api -- bootstrap-admin <email> <password> [displayName]
   ```

   This hashes `<password>` with the same `PasswordHasher<Author>` the login endpoint verifies against, then inserts or updates that `Author` row with `isAdmin = true`, and exits without starting the host. Safe to re-run against an existing email to reset its password.

## Pre-deploy checklist (every environment beyond local dev)

`AddMakeBoldSparkAuth` registers JWT authentication globally during service configuration — a missing `Jwt:SigningKey`/`Jwt:Authority` now fails the **entire API's** startup, not just this feature (gate finding critic-001, SHOWSTOPPER). Before deploying this feature to any environment:

- [ ] Confirm `Jwt__SigningKey` exists as an Azure App Service application setting for the target environment (or `Jwt:Authority`, if pointing at an external IdP instead).
- [ ] Confirm at least one `Author` row has a real password hash and `isAdmin = true` in the target environment's database (step 2 above) — otherwise nobody can sign in after deploy.

This is a deploy-time gate only — it does not block local development or any user-story implementation work.

## Local development loop

1. `cd src/MakeBoldSpark.Cms/client && npm install && npm run dev` — Vite dev server on `http://localhost:5173`, proxying `/api/*` to `http://localhost:5290`.
2. In a second terminal: `dotnet run --project src/MakeBoldSpark.Api` (or `dotnet watch run`).
3. Open `http://localhost:5173`, sign in with the bootstrapped administrator credential.

## Verification checklist (end-to-end)

1. **Login correctness**: sign in with the bootstrapped administrator credential → succeeds, reaches the content area. Sign in with a wrong password, an unknown email, and a non-admin author's correct credentials → all three produce the identical generic failure message (FR-001a), and exercising several wrong attempts in a row triggers the invisible throttle without any "locked out" message appearing (FR-014).
2. **Core content (P1)**: create a Post under an existing Blog/Author, edit it, confirm the change persisted and no other field was altered; delete a Category with no dependent Posts.
3. **Delete guardrail (FR-012)**: attempt to delete a Blog that still has Posts → blocked, with an explanation of what depends on it, not a generic error.
4. **Navigation/taxonomy (P2)**: create a nested Menu item under an existing Site and confirm the parent/child hierarchy renders, not a flat list.
5. **Subscribers/newsletters/mail (P3)**: add a Subscriber; record a Newsletter send against an existing Post and confirm it has no edit action afterward; open Mail Configuration and confirm the sending credential is masked until explicitly revealed.
6. **Branding (FR-013)**: confirm the app's logo, color palette, and typography visually match the platform's other public pages (same `brand.css`/fonts, not a re-derived approximation).
7. **Production-mode parity**: `dotnet build` from repo root (triggers `BuildReactSpa` embedding), `dotnet run --project src/MakeBoldSpark.Api`, browse to `https://localhost:7152/cms/`, repeat steps 1–3 against the embedded build, and confirm a hard refresh on a deep link (e.g. `/cms/posts`) returns the SPA via the fallback route instead of a 404.
8. **Authorization tightening regression check**: confirm that a request to any `/api/admin/makeboldspark/*` route with a hand-crafted, unsigned token (the kind that worked before this feature) is now rejected with `401` — this is the core security gap this feature closes.
