# Family Memories validation

## Local runtime revalidation (2026-09-14)

- Started the Development host with `dotnet run --no-build --project src/MakeBoldSpark.Api --launch-profile http` at `http://localhost:5290`; startup reported successful database migrations.
- Live HTTP checks: `/`, `/cms/family-memories`, `/api/health`, and `/openapi/v1.json` returned 200. The public feed returned 200 with `success:true`, an empty memories array, and `Cache-Control: no-store`. OpenAPI included all six Family Memories operations.
- Live GET/OPTIONS allowed `https://texecon.com` (200/204), while unconfigured origins and default localhost:5173 received no allow-origin header.
- All five management operations returned 401 anonymously with appropriate request content types. POST without a JSON content type returned 415.
- Full API regression: 241 passed, zero failed/skipped, including authenticated CRUD, publication, validation, persistence, and authorization integration coverage. Report: `tests/MakeBoldSpark.Api.Tests/TestResults/family-memories-validation.trx`.
- CMS regression: initial run passed 17/18 with a Markdown preview lazy-loading timeout; immediate full rerun passed all 18 tests across seven files. Family Memories editor tests passed both runs.
- Authenticated writes were exercised in isolated integration fixtures, not against the running workstation database. No visual browser check or production deployment was performed.

Product Owner TL;DR: Implementation, additive migration, CMS management and public API are complete on `0006-family-memories`. All 241 API tests and 18 CMS tests pass. No deployment was performed.

## Executed checks (2026-09-12)

| Check | Result |
|---|---|
| `dotnet test tests/MakeBoldSpark.Api.Tests --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=family-memories-regression.trx'` | 241 passed, 0 failed, 0 skipped; 40 Family Memories cases included |
| `npm test` in `src/MakeBoldSpark.Cms/client` | 7 files, 18 tests passed; includes 2 Family Memories editor scenarios |
| `npm run lint` in CMS client | Passed, zero warnings |
| CMS TypeScript/Vite production build through API MSBuild | Passed |
| API/.NET build and existing static web build | Passed |
| `dotnet ef migrations add AddFamilyMemories --project src/MakeBoldSpark.Api --context MakeBoldSpark.Api.Infrastructure.Data.MakeBoldSparkDbContext` | Generated additive migration, designer and snapshot successfully |
| `git diff --check` | Passed |

Raw local test report: `tests/MakeBoldSpark.Api.Tests/TestResults/family-memories-regression.trx` (generated output, not committed).

## Acceptance evidence

- AC1: exact envelope/property names and string kinds asserted; empty 200; full 106-record collection; published-only data; ordering and ID tie-break; no-store response.
- AC2: SQL round-trip and reopened file database preserve Unicode, CRLF/LF, whitespace, punctuation, literal markup and bold Markdown links. Maximum lengths succeed; oversized, missing, null, numeric and whitespace-only fields fail without persistence changes. Empty pronouns succeed.
- AC3: create/detail/edit/reorder/publish/unpublish/delete lifecycle; 201 Location, 200, 204 and 404 responses; all management operations reject anonymous callers and non-admin/publisher roles. A real token obtained through the existing CMS login succeeds; invalid token fails.
- AC4: editor tests exercise authenticated management requests, plain textarea text, draft defaults, publication checkbox, reorder display, unpublish and confirmed/cancelled deletion. Existing CMS regressions pass.
- AC5: actual in-process HTTP GET/OPTIONS pipeline allows texecon and explicit development origin, rejects unconfigured origins and implicit localhost, and does not allow credentials. Missing database table produces 500 ProblemDetails with CORS and no SQL details. No external browser or live production smoke test was performed.
- AC6: fresh migrations run in integration fixtures. File-backed upgrade from AddBoldFeature retains an existing fictional tag, starts with no seeded memories, preserves a saved memory after close/reopen, rejects database null, and verifies down/up behavior without losing unrelated data. No pending EF model changes.
- AC7: checked-in OpenAPI contract preceded implementation; runtime OpenAPI projection/routes/body limit checked. Guide documents public/admin examples, statuses, limits, CORS, deployment/rollback and build-time frontend setting.
- AC8: no new dependencies, production content, public submissions, external writes or deployment. Existing suite passes in full.

## Corrections during verification

The initial test compilation used the wrong test-framework attributes and was converted to the repository's MSTest conventions. A CORS test exposed eager configuration capture; policy construction now reads final configuration when options are built. The real-auth fixture needed its validator aligned with its issuing key because the app registers auth before the test host configuration override. The final full run above includes these corrections.

Build tooling emitted existing runtime-served brand.css and EF tool-version notices; generation/build succeeded. No new package versions were introduced.

## Integration handoff

After deploying the API, use:
`VITE_FAMILY_MEMORIES_API_URL=https://api.markhazleton.com/api/public/family-memories`

Changing this build-time setting requires rebuilding and deploying the consuming frontend. Manage records at `/cms/family-memories`. Default development CORS list is empty; configure explicit origins as documented in `bold-docs/system/family-memories-api.md`.

No proposed deviations from the supplied contract. Production deployment and live endpoint verification remain outside this implementation delivery.
