# Phase 0 Research: CMS Admin App

No `NEEDS CLARIFICATION` markers remained in Technical Context — this feature's design decisions were largely settled during the `/devspark.specify`/`/devspark.clarify` conversation. This document records the resulting decisions and the alternatives considered, so they aren't re-derived during implementation.

## Decision: Self-issued JWT login replaces the earlier "paste a token" idea

**Decision**: `MakeBoldSpark.Api` gains a `POST /api/public/auth/login` endpoint that checks an `Author`'s email/password and returns a JWT signed with a server-held symmetric key (`Jwt:SigningKey`).

**Rationale**: Verified directly in code (`AuthorizationSetup.cs:13-48`, `appsettings.json:11-14`) that `Jwt:Authority`/`Jwt:Audience` are empty everywhere — not just in dev — so signature validation is currently disabled unconditionally and any well-formed token is accepted. No `/login`, `/token`, or credential-checking endpoint exists anywhere in `Features/` or `Infrastructure/`. A client-side "generate an unsigned dev token" convenience (considered earlier in this feature's design) would have papered over this rather than fixing it, and would coexist insecurely once a real login screen existed. The `Author` entity already models `email`, `password`, and `isAdmin` (`.documentation/frontend/makeboldspark-api-guide.md` lines 116-123) — the natural credential store, confirmed unique by email per the resolved clarification.

**Alternatives considered**:

- Full ASP.NET Core Identity membership system — rejected, adds a parallel account system and Identity-specific tables on top of `Author`, which already has everything needed.
- External OAuth/OIDC provider (e.g., Entra ID) — rejected for this iteration as disproportionate setup for a single-administrator tool; `AuthorizationSetup.cs` already supports pointing `Jwt:Authority` at one later without further code changes, so this path stays open without being built now.

## Decision: Tighten `AuthorizationSetup.cs` to require signature validation

**Decision**: Once `Jwt:SigningKey` is configured, `AuthorizationSetup.cs` validates tokens against it (`ValidateIssuerSigningKey = true`) instead of falling back to the current "no authority → accept any well-formed token" branch. The app fails fast at startup if neither `Jwt:Authority` nor `Jwt:SigningKey` is configured, rather than silently running with an open admin gate.

**Rationale**: Shipping a real login screen in front of a still-unverified token would be worse than today — it implies a security boundary that isn't actually enforced. This is a deliberate breaking change to a known-insecure default; nothing else in the codebase was found to depend on the permissive fallback (confirmed via search — only the test project's separate `TestAuthHandler`/`X-Test-Claims` mechanism bypasses JWT entirely, and that's independent of `AuthorizationSetup`'s validation parameters).

**Alternatives considered**: Leave the insecure fallback in place "for dev convenience" — rejected; it would mean local dev and production differ in the one property (signature verification) that actually matters, the opposite of what should be tested.

## Decision: `Microsoft.AspNetCore.Identity`'s `PasswordHasher<TUser>`, used standalone

**Decision**: Hash/verify passwords with `PasswordHasher<TUser>` (PBKDF2, framework-provided), without adopting Identity's membership system, user store, or tables. `Author.password` changes meaning from "stored as entered" (currently empty strings in seed data) to "PBKDF2 hash."

**Rationale**: Matches Constitution Principle III (Simplicity) and the "no unnecessary packages" guidance — gets a vetted, maintained hashing implementation without a schema change or a new account system.

**Alternatives considered**: `BCrypt.Net` or `Konscious.Argon2` — both viable, but add a third-party dependency where a framework-provided one already does the job.

## Decision: Built-in ASP.NET Core rate limiting for the login throttle (FR-014) — two layers, not one

**Decision**: Apply `Microsoft.AspNetCore.RateLimiting` (ships in the ASP.NET Core shared framework since .NET 7 — no new package) to the login route with **two** fixed-window limiter policies: one keyed by the submitted email (deters repeated guessing against one known account), and a second keyed by client IP or a global partition (deters spraying a small set of common passwords across many different emails, which a per-email-only limiter does nothing to slow). When either limiter is exceeded, the endpoint returns the **same generic failure response** used for a wrong password or unknown email — never a distinct `429` — so the response carries no observable signal that throttling occurred (this directly satisfies SC-006's "no observable difference in the response that would reveal why").

**Rationale**: Zero new dependencies; framework-native; keeps the response shape uniform as required. The second layer closes a gap surfaced by `/devspark.critic` (finding critic-002): an email-keyed-only limiter doesn't slow an attacker who tries different emails rather than repeating one, since each new email starts its own fresh throttle bucket.

**Alternatives considered**: A custom `IMemoryCache`-based counter — rejected as reinventing what the framework already provides. A single global-only limiter (no per-email layer) — rejected because it would let one attacker's spray traffic throttle out a legitimate administrator trying to sign in around the same time; keeping both layers isolates the two threat models.

## Decision: Bound the login password field's length before hashing

**Decision**: `LoginRequest.Password` is validated against a maximum length (256 characters) before any call into `PasswordHasher<TUser>`, rejected with the same generic failure response used for any other rejection reason.

**Rationale**: PBKDF2-style hashing cost is sensitive to input handling; an unbounded password field is a cheap, repeatable lever for a denial-of-service attempt against the login endpoint (`/devspark.critic` finding critic-004). A 256-character cap comfortably exceeds any real password while closing the lever.

**Alternatives considered**: No cap (status quo) — rejected as an unnecessary, free-to-fix exposure once named.

## Decision: Explicit guard against logging the login request body

**Decision**: Verified `RequestLoggingMiddleware.cs` logs only method/path/status/duration/correlation-id/user-id today — request bodies are never captured, so the login endpoint's plaintext password is not actively leaking. A code comment and a test on the login route assert this invariant going forward, so a future change (e.g., enabling ASP.NET Core's built-in `HttpLogging` middleware) can't silently start capturing it without a test failing first.

**Rationale**: `/devspark.critic` (finding critic-006) and `/devspark.analyze` (finding E5) both flagged the *absence* of a regression guard for this invariant, even though the invariant currently holds. Cheap to assert now, expensive to discover later as an actual leak.

## Decision: Enforce `Author.email` uniqueness at the database level, not just as an assumption

**Decision**: Add a unique index on `Author.email` in `MakeBoldSpark.Core` (EF Core migration), turning the resolved clarification ("author emails are already unique platform-wide") into an enforced invariant rather than an unverified assumption the sign-in logic depends on.

**Rationale**: `/devspark.critic`'s Questionable Assumptions section noted that if this assumption were ever violated (e.g., by a future data import), sign-in matching becomes non-deterministic — whichever row a `.FirstOrDefault()`-style query happens to return could authenticate as the wrong author. A database constraint makes that failure mode impossible instead of merely unlikely.

## Decision: `MakeBoldSpark.Cms` as an in-solution embedded-resource project (not a NuGet package)

**Decision**: New class library `src/MakeBoldSpark.Cms/`, React SPA built via an MSBuild target (`BuildReactSpa`, mirroring `ApiTestSpark`'s `NUGET-PACKAGE-WALKTHROUGH.md`) and embedded as manifest resources, referenced from `MakeBoldSpark.Api` via plain `ProjectReference`, mounted via `app.MapMakeBoldSparkCms()`.

**Rationale**: Explicit user direction — this UI has exactly one consumer, so NuGet packaging/publishing/versioning overhead (which `ApiTestSpark` needs because it's redistributed across unrelated host apps) doesn't apply. `ManifestEmbeddedFileProvider` + `UseStaticFiles` + `MapFallbackToFile` is the same serving mechanism either way.

**Alternatives considered**: Plain static files copied into `MakeBoldSpark.Api/wwwroot/cms` via an MSBuild target (like `MakeBoldSpark.Web`'s Eleventy output) — viable, but embedding keeps the SPA's build artifacts out of `wwwroot` entirely and out of source control (no `wwwroot/cms/**` to decide whether to commit), and matches the user's explicit request to mirror `ApiTestSpark`'s mechanism.

## Decision: No `/cms/config` bridge endpoint

**Decision**: Drop the previously-considered `/cms/config` endpoint (originally meant only to tell the SPA whether it's safe to show a fake "Generate Dev Token" button).

**Rationale**: That button is superseded by the real login endpoint — there's nothing left for a config bridge to gate. The SPA calls `/api/public/auth/login` and `/api/admin/makeboldspark/*` same-origin; no environment-conditional behavior is needed.

## Decision: Reuse the platform's already-extracted brand assets directly; no duplication

**Decision**: The CMS SPA references the existing `/assets/makebold/brand.css` and font files already served by `MakeBoldSpark.Api` (confirmed present and already derived from `.documentation/branding/MakeBoldSolutions/` — found in `wwwroot/assets/makebold/brand.css` across 5 build-output locations) via absolute same-origin URLs, instead of copying or re-deriving brand assets into the new project.

**Rationale**: Since the SPA is served from the same host at runtime, `<link href="/assets/makebold/brand.css">` and `/assets/makebold/fonts/...`/`/assets/makebold/logos/...` just work — no asset duplication, no second source of truth for brand styling, and it automatically stays in sync if the platform's brand.css changes.

**Alternatives considered**: Re-deriving styles from the raw `.ai`/`.pdf` brand guide files into the new project — rejected, unnecessary duplication of work already done for the platform's other public pages.

## Decision: One generic `EntityCrudPage`, not 11 bespoke screens

**Decision**: A single list+edit component configured per entity via a field-definition array (`entityConfigs.ts`), reusing the documented `apiFetch`/`authedFetch`/`adminCrud<T>` client pattern from `.documentation/frontend/makeboldspark-api-guide.md` almost verbatim. Three entities layer small overrides on the same component: Menu (tree display via the guide's `buildMenuTree()`), Newsletter (no edit action — append-only per FR-004), Author/MailSetting (masked credential fields per FR-005/FR-006).

**Rationale**: All 11 admin resources share an identical `GET list / GET {id} / POST / PUT {id} / DELETE {id}` shape (guide, "Admin CRUD for Public Entities" section) — bespoke screens per entity would violate Simplicity without adding value.

## Decision: Treat signing-key provisioning as a deployment-runbook step, not an implicit assumption

**Decision**: Because `AddMakeBoldSparkAuth` registers JWT authentication globally during service configuration (`Program.cs:33`), the fail-fast startup guard introduced above means the **entire API** — not just this feature — fails to start in any environment missing `Jwt:SigningKey`/`Jwt:Authority`. A pre-deploy checklist item (and, if a CI/CD pipeline exists, an automated check) confirming the setting exists in the target environment is added as an explicit task (`tasks.md` T009a), separate from the local-dev `user-secrets` step (T006).

**Rationale**: `/devspark.critic` (finding critic-001, SHOWSTOPPER) identified this as the one finding worth stopping for — an otherwise-routine deploy could take down the whole site, not a narrow feature, if this step is skipped or forgotten.

## Bootstrapping note: one-time setup, made repeatable

Existing seed data stores `Author.password` as an empty string (per the frontend guide's documented gotcha). Once `PasswordHasher<TUser>` verification is wired in, **no existing seeded author can sign in** until at least one `Author` row with `isAdmin = true` has a real password hash set. This needs a one-time bootstrapping task (e.g., a small console/script step run once against the target database) — captured in `tasks.md` T014, not a recurring feature of the app itself (self-service password reset is explicitly out of scope per `spec.md`). Per `/devspark.critic` (finding critic-007), T014's procedure is documented as a short, repeatable runbook entry in `quickstart.md` rather than a throwaway task description, since provisioning a *second* administrator later will need the same steps.
