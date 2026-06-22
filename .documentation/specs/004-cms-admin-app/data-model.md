# Phase 1 Data Model: CMS Admin App

All 11 CMS entities already exist in `MakeBoldSpark.Core.Data` and are unchanged by this feature except where noted (`Author.password`). This document records the fields relevant to this feature's requirements — full field lists are already documented in `.documentation/frontend/makeboldspark-api-guide.md`.

## Author (modified usage, no schema change)

| Field | Type | Notes |
|---|---|---|
| `id` | number | — |
| `email` | string | **Sign-in identifier.** Confirmed unique platform-wide (resolved clarification) — enforced via a new unique database index (per `/devspark.critic`'s Questionable Assumptions finding, rather than left as an unverified assumption), so no tie-breaking rule is needed for sign-in matching. |
| `password` | string | **Meaning changes**: previously stored as entered (empty string in seed data); from this feature forward, stores a `PasswordHasher<TUser>` (PBKDF2) hash. Never returned to any client in any form (FR-006), regardless of `isAdmin`. The **submitted** login password (not the stored hash) is capped at 256 characters before verification is attempted — see Validation rules below. |
| `displayName` | string | Shown in the admin app after sign-in. |
| `isAdmin` | boolean | **Sign-in gate.** Only authors with `isAdmin = true` may successfully sign in (FR-001a); `false` produces the same generic failure as a wrong password. |

**Validation rules introduced by this feature**:
- A login attempt MUST match exactly zero or one `Author` row by `email` (uniqueness precondition, now database-enforced — see `email` row above).
- A login attempt against a matched row with `isAdmin = false`, OR against zero matched rows, OR with a password that fails hash verification, OR with a submitted password exceeding 256 characters, MUST all produce the identical response (FR-001a) — no branch in the implementation should produce a distinguishable response shape, status code, or meaningfully different timing for these cases.
- A login attempt MUST be rejected by either of two independent rate limiters (per-email, per-IP/global) without a distinguishable response — see `contracts/auth-api.md`.

**State/lifecycle note**: No new state is introduced. `password` is opaque to every code path except the hasher; nothing else reads or compares it directly.

## Issued Access Credential (new concept, not a persisted entity)

Not a database table — a JWT constructed at sign-in time and handed to the caller. Recorded here because its shape is part of this feature's data contract.

| Claim | Source | Notes |
|---|---|---|
| `sub` | `Author.id` | Subject identifier |
| `role` | literal `"Admin"` | Matches the existing `AdminOnly`/`Publisher` policy's `RequireRole` check in `AuthorizationSetup.cs:64-67` — no policy code changes needed |
| `name` | `Author.displayName` | For display in the admin app header, not for authorization |
| `exp` | issued time + fixed session lifetime | Enforced by the existing `ValidateLifetime = true` (unchanged) |

No refresh-token concept is introduced — re-authentication on expiry is explicit sign-in again (FR-001c), consistent with self-service password reset being out of scope.

## Other 10 CMS Entities (Site/Domain, Blog, Post, Category, Menu, Keyword, Content Part, Subscriber, Newsletter, Mail Configuration)

Unchanged by this feature — full field lists, relationships, and known gotchas (e.g., `Category.content` is the display name, `Menu.parentId` self-reference, `MailSetting.userPassword` never returned, full-replace `PUT` semantics) are already documented in `.documentation/frontend/makeboldspark-api-guide.md` and remain authoritative. This feature's `entityConfigs.ts` (per-entity field definitions driving the generic `EntityCrudPage`) is a direct mapping of those existing types — no new fields, relationships, or validation rules beyond what FR-002 through FR-005 already specify (which operations are allowed per entity, and which fields must be masked/non-editable).

| Entity | CRUD allowed (per FR-002–FR-005) | Special handling |
|---|---|---|
| Site/Domain | view, create, edit, delete | — |
| Blog | view, create, edit, delete | — |
| Post | view, create, edit, delete | Requires selecting a Blog first (FR-011); body uses format-while-you-type editing (FR-010) |
| Category | view, create, edit, delete | — |
| Menu | view, create, edit, delete | Requires selecting a Site first (FR-011); displayed as a parent/child tree, not a flat list (FR-003) |
| Keyword | view, create, edit, delete | — |
| Content Part | view, create, edit, delete | Body uses format-while-you-type editing (FR-010) |
| Subscriber | view, create, edit, delete | — |
| Newsletter | view, create, delete (**no edit**) | Append-only history (FR-004) |
| Mail Configuration | view, create, edit, delete | Sending credential masked by default (FR-005) |

**Cross-entity validation rule (FR-012)**: any delete attempt on a record with dependent records (e.g., a Blog with Posts, a Site with Menus) MUST be blocked, with a response identifying what depends on it — this is enforced by checking for dependents before issuing the existing `DELETE` call, surfacing the platform's underlying FK-constraint failure as a specific message rather than a generic one (per the guide's documented "500 usually means a FK constraint" gotcha).
