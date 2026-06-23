# Pull Request Review: feat: add CMS admin app with authenticated content management

## Review Metadata

- **PR Number**: #3
- **Source Branch**: `004-cms-admin-app`
- **Target Branch**: `main`
- **Review Date**: 2026-06-23 12:34:33 UTC
- **Last Updated**: 2026-06-23 13:00:22 UTC
- **Reviewed Commit**: `82f3fb7`
- **Reviewer**: devspark.pr-review
- **Constitution Version**: 1.1.0

## Revision Log

| Rev | Commit | Date | Critical | High | Medium | Low | CON | Test Command | Result |
|-----|--------|------|----------|------|--------|-----|-----|--------------|--------|
| 1 | `6dfc019` | 2026-06-23 | 2 | 0 | 1 | 0 | 0 | `npm test` (CMS client); scoped `dotnet test` for changed auth/CMS classes | pass: 16 client tests; 14 API tests |
| 2 | `82f3fb7` | 2026-06-23 | 0 | 0 | 0 | 0 | 0 | `npm test` (CMS client); scoped `dotnet test` for changed auth/CMS classes | pass: 16 client tests; 15 API tests |

## PR Summary

- **Author**: @markhazleton
- **Created**: 2026-06-23
- **Status**: OPEN
- **Files Changed**: 100
- **Commits**: 9
- **Lines**: +11,852 -3,186

## Stats

| Metric | Value |
|--------|-------|
| Files changed | 154 |
| Lines added | +11,908 |
| Lines removed | -3,194 |
| Net lines | +8,714 |
| Commit snapshot | `82f3fb7` |

## Executive Summary

- ❌ **Constitution Compliance**: FAIL (8/10 principles checked; two blocking deviations)
- 📋 **Spec Lifecycle**: Complete
- 📝 **Task Completion**: 65/65 tasks complete
- 🔒 **Security**: 1 timing-side-channel issue found
- 📊 **Code Quality**: 1 recommendation
- 🧪 **Testing**: PASS (scoped tests)
- 📝 **Documentation**: PARTIAL (the required Constitution amendment remains proposed)
- 🏛️ **Constitution Improvements**: 0

**Overall Assessment**: The CMS embedding, client tests, credential hashing, JWT signature validation, throttling, and sanitized Markdown preview are solid. The login path still leaks whether an administrator account exists through password-hash timing, and the anonymous token-issuance route remains outside the Constitution's defined route categories.

**Approval Recommendation**: ❌ REJECT

## Action Items

### Immediate Actions (Blocking — must resolve before merge)

- [x] **C-01** `src/MakeBoldSpark.Api/Features/Auth/AuthService.cs:25` — Login failures have distinguishable timing. — *Fixed in `82f3fb7`: all normal rejection paths verify one password hash against either the eligible author or a timing-safe placeholder.*
  - **Broken code**:

    ```csharp
    if (author is null || !author.IsAdmin)
        return null;

    if (Hasher.VerifyHashedPassword(author, author.Password, request.Password)
        == PasswordVerificationResult.Failed)
    ```

  - **Fix**: perform one PBKDF2 verification using a fixed dummy hash whenever no eligible administrator is found, then add a regression test covering unknown-email, non-admin, and wrong-password paths. This implements the contract's timing-indistinguishability requirement without disclosing administrator-account existence.

- [x] **C-02** `src/MakeBoldSpark.Api/Program.cs:339` — `POST /api/public/auth/login` is not a read-only public route. — *Fixed in `82f3fb7`: Constitution v1.2.0 defines the constrained `/api/public/auth/*` authorization category.*
  - **Broken code**:

    ```csharp
    publicApi.MapGroup("/auth").MapAuthApi();
    ```

  - **Fix**: have the project owner ratify the proposed Principle VIII amendment before merge, explicitly adding a constrained anonymous authentication route category. The tracking decision correctly identifies the gap, but it expressly remains proposed and cannot override the Constitution.

### Recommended Improvements

- [x] **M-01** `src/MakeBoldSpark.Cms/client/.vite/deps_temp_a5958e15/package.json:1` — Generated Vite/TypeScript cache and compiled configuration files are tracked (`.vite/`, `*.tsbuildinfo`, `vite.config.js`, `vite.config.d.ts`, and `spa-built.sentinel`). Remove them from the PR and ignore the generated paths; retain only authored TypeScript/configuration sources and the lockfile. — *Fixed in `82f3fb7`: generated files were removed from Git and targeted ignore rules added.*

### Constitution Improvements (Non-blocking — feed into `/devspark.evolve-constitution`)

None found.

## What's Good

- The embedded SPA build now runs before the embedded-resource manifest and is validated on a clean build in `src/MakeBoldSpark.Cms/MakeBoldSpark.Cms.csproj`.
- `MarkdownPreview.tsx` sanitizes inline HTML and restricts iframe sources to HTTPS YouTube embed URLs.
- The authentication tests cover successful login, generic rejection bodies, throttling behavior, oversized passwords, JWT signature validation, and request-body logging.
- CI supplies a non-production test signing key and the test helpers restore ambient values, avoiding the runner-only failure that was diagnosed during this PR.

## Findings Detail

### Critical Issues (Blocking)

| ID | Status | Principle | File:Line | Issue | Fix |
|----|--------|-----------|-----------|-------|-----|
| C-01 | 🔴 Open | IV. Security by Default (`§IV.SHOWSTOPPER`) | `src/MakeBoldSpark.Api/Features/Auth/AuthService.cs:25` | The code returns before password-hash verification for unknown or non-admin emails, but verifies PBKDF2 for a wrong password on an administrator account. This conflicts with the contract's requirement that rejection timing be indistinguishable and enables administrator-account enumeration. | Verify against a constant dummy hash on all ineligible-account paths and test the three rejection paths. |
| C-02 | 🔴 Open | VIII. Clear Authorization Boundaries (`§VIII.SHOWSTOPPER`) | `src/MakeBoldSpark.Api/Program.cs:339` | The Constitution requires every route to fall under a defined category and defines `/api/public/*` as anonymous, read-only. This anonymous token-issuance POST is neither. Decision 0007 states the amendment is only proposed. | Ratify and commit the explicit `/api/public/auth/*` category amendment before merging. |

### High Priority Issues

None found.

### Medium Priority Suggestions

| ID | Status | Principle | File:Line | Issue | Recommendation |
|----|--------|-----------|-----------|-------|----------------|
| M-01 | 🔴 Open | III. Simplicity (`§III.MEDIUM`) | `src/MakeBoldSpark.Cms/client/.vite/deps_temp_a5958e15/package.json:1` | Tool-generated caches and compiled outputs add unstable, non-authored files to the change set. | Remove generated files and extend `.gitignore` for `.vite/` and `*.tsbuildinfo`; keep source `vite.config.ts` only. |

### Low Priority Improvements

None found.

### Constitution Improvements

None found.

## Constitution Alignment Details

| Principle | Status | Evidence | Notes |
|-----------|--------|----------|-------|
| I. API-First | ✅ Pass | `contracts/auth-api.md`, OpenAPI metadata in `AuthEndpoints.cs` | Login request/response behavior is documented and tagged. |
| II. Test-First | ✅ Pass | CMS and API tests added | Scoped tests pass. |
| III. Simplicity | ⚠️ Partial | Generated client files committed | Remove build/cache artifacts. |
| IV. Security by Default | ❌ Fail | `AuthService.cs:25-32` | Failure timing reveals whether an eligible administrator was found. |
| V. Spec-Driven Development | ✅ Pass | Spec complete; 65/65 tasks complete | Full-compliance trust tier. |
| VI. Ownership Boundary | ✅ Pass | No framework operation overwrites project-owned artifacts | Framework changes are separate from CMS implementation. |
| VII. Single Backend Platform | ✅ Pass | `MakeBoldSpark.Cms` is an embedded client library referenced by the existing API | No service split introduced. |
| VIII. Clear Authorization Boundaries | ❌ Fail | `Program.cs:339` | The required amendment is still proposed. |
| IX. Relational-First Data Strategy | ✅ Pass | EF Core/SQLite migration and DbContext use | No new non-relational default store. |
| X. Zero Secrets in Source Control | ✅ Pass | Test-only strings do not provide production access; runtime key is configuration-driven | No production credential identified. |

## Security Checklist

- [x] No production secrets or credentials committed
- [ ] Input validation/timing handling complete for the sign-in path (C-01)
- [ ] Authentication/authorization boundaries appropriate (C-02)
- [x] No SQL injection introduced in reviewed paths
- [x] Markdown/inline HTML preview sanitizes output and constrains embeds
- [x] Dependencies use lockfiles; no production dependency vulnerability was identified during this review

## Testing Coverage

**Status**: ADEQUATE, with one missing timing-side-channel regression test required by C-01.

- `npm test` in `src/MakeBoldSpark.Cms/client`: 6 files, 16 tests passed.
- Scoped API runs for changed authentication/CMS hosting/Swagger test classes: 14 tests passed.
- GitHub Actions validation is currently passing for the reviewed branch.

## Test Inventory

| File | Main | Branch | Delta | Justification |
|------|------|--------|-------|---------------|
| `src/MakeBoldSpark.Cms/client/src/api/client.test.ts` | 1 | 0 | +1 | Client request behavior |
| `src/MakeBoldSpark.Cms/client/src/cms/EntityCrudPage.test.tsx` | 6 | 0 | +6 | Editor/list interactions |
| `src/MakeBoldSpark.Cms/client/src/cms/entityConfigs.test.ts` | 4 | 0 | +4 | Entity configuration |
| `src/MakeBoldSpark.Cms/client/src/cms/overrides/MarkdownField.test.tsx` | 2 | 0 | +2 | Markdown editor |
| `src/MakeBoldSpark.Cms/client/src/cms/overrides/MaskedCredentialField.test.tsx` | 1 | 0 | +1 | Credential masking |
| `src/MakeBoldSpark.Cms/client/src/cms/overrides/MenuTree.test.tsx` | 2 | 0 | +2 | Menu hierarchy |
| `tests/MakeBoldSpark.Api.Tests/Features/Auth/AuthEndpointsTests.cs` | 5 | 0 | +5 | Login behavior |
| `tests/MakeBoldSpark.Api.Tests/Features/CmsHostingTests.cs` | 1 | 0 | +1 | Embedded CMS hosting |
| `tests/MakeBoldSpark.Api.Tests/Features/SwaggerAvailabilityTests.cs` | 5 | 0 | +5 | OpenAPI availability |
| `tests/MakeBoldSpark.Api.Tests/Infrastructure/Auth/AuthorizationSetupTests.cs` | 1 | 0 | +1 | Missing JWT configuration |
| `tests/MakeBoldSpark.Api.Tests/Infrastructure/Auth/AuthorizationSignatureTests.cs` | 2 | 0 | +2 | JWT signature validation |
| **Total** | **30** | **0** | **+30** | |

No test removals detected.

## Documentation Status

**Status**: PARTIAL

The API contract, quickstart, data model, research, and decision record are present. The decision record deliberately leaves the required Principle VIII amendment for owner ratification; this is the blocking documentation/governance gap in C-02.

## Changed Files Summary

| File/group | Tier | Changes | Type | Findings |
|------------|------|---------|------|----------|
| `src/MakeBoldSpark.Api/Features/Auth/*` | P0 | +146 -0 | Added | C-01 |
| `src/MakeBoldSpark.Api/Infrastructure/Auth/AuthorizationSetup.cs` | P0 | +18 -16 | Modified | None |
| `src/MakeBoldSpark.Api/Program.cs` | P0 | +138 -1 | Modified | C-02 |
| `src/MakeBoldSpark.Cms/MakeBoldSparkCmsExtensions.cs` | P0 | +42 -0 | Added | None |
| `src/MakeBoldSpark.Cms/client/src/*` | P1/P2 | +2,800+ | Added | None |
| `src/MakeBoldSpark.Cms/client/.vite/*` and generated config/cache outputs | P2 | +9 files | Added | M-01 |
| `.github/workflows/validate.yml` | P2 | +2 -0 | Modified | None |
| `.documentation/specs/004-cms-admin-app/*` | P3 | +1,000+ | Added | C-02 evidence |

## Behavioral Changes

| Change | Before | After | Intentional? | Risk |
|--------|--------|-------|-------------|------|
| Administrator authentication | Existing admin endpoints did not verify an issued credential | Login verifies an author password and issues a signed JWT | Yes | Correctly adds identity verification, subject to C-01/C-02. |
| CMS delivery | No embedded `/cms` application | SPA assets embedded and served at `/cms` | Yes | Asset manifest and hosting tests cover the path. |

## Approval Decision

**Recommendation**: ❌ REJECT

**Reasoning**: The PR is well documented and tested, but it cannot be approved while login rejection timing leaks eligible administrator accounts and while the documented route-category waiver has not been ratified into the authoritative Constitution. Resolve C-01 and C-02, remove generated cache files (M-01), then request a re-review.

**Estimated Rework Time**: 2-4 hours plus owner ratification of the Constitution amendment.

---

*Review generated by devspark.pr-review v1.2*
*Constitution-driven code review for MakeBoldSpark*
*To re-review after fixes: `/devspark.pr-review #3 re-review`*
*When addressing these findings, run `/devspark.address-pr-review 3`. The review file must be committed on its own.*
