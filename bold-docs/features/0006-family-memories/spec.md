# CMS-managed Family Memories

**Tier**: Feature
**Status**: Complete
**Ratified**: User confirmed Feature on 2026-09-12; scope is the supplied self-contained contract.

## Product Owner TL;DR
Editors manage memories in the existing CMS. Texecon reads the entire published collection anonymously, preserving the entered text. No public submissions or production fixtures are included.

## Intent and design
Use the single API host, existing AdminOnly policy, generic CMS CRUD screen, and API MakeBoldSparkDbContext SQLite persistence (DefaultConnection). Add a feature folder with endpoint → service → repository → context separation. Existing posts/content parts have incompatible HTML and publication semantics; reuse the editor framework rather than overloading those entities.

## Acceptance Criteria
1. GET /api/public/family-memories returns HTTP 200 and exactly {success:true,data:{memories:[...]}}. Each memory has only personName, personPronouns, relationshipToJared, body, all strings. Empty collection returns []; no pagination. Order is sortOrder ascending, then id ascending, published only.
2. Preserve input strings without trimming, normalization, HTML conversion, or Markdown processing, including Unicode, CRLF/LF, whitespace and bold HTTP(S) Markdown link labels. Require non-whitespace name (200 UTF-16 code units), relationship (300), body (100000); pronouns must be present and non-null, may be empty, maximum 100. All public string fields required on writes. Reject bad input with 400. sortOrder is an integer 0..2147483647; isPublished is boolean, defaults false on creation; sortOrder defaults 0. PUT fully replaces editable fields; omitted publication/order revert to defaults.
3. AdminOnly GET list/detail, POST, PUT and DELETE under /api/admin/makeboldspark/family-memories support draft creation, editing, numeric ordering, publishing and unpublishing. IDs and UTC timestamps are server-owned. POST returns 201 with Location, PUT 200, DELETE 204; absent records 404, anonymous 401, non-admin 403. Last committed edit wins, consistent with existing CMS; ordering ties use immutable ID. Each write is atomic.
4. CMS /cms/family-memories uses existing navigation, authentication, forms and delete confirmation, plain textarea body, published checkbox and numeric order. Describe limits in field labels and enforce maximum lengths in controls.
5. Endpoint-specific CORS always allows https://texecon.com and only explicitly configured FamilyMemories:DevelopmentOrigins additional origins. GET and Accept: application/json work without credentials or secrets, including preflight. Disallowed origins receive no allow-origin header. No wildcard or implicit localhost fallback. Unexpected endpoint failures return generic 500 ProblemDetails and are logged without memory bodies. Responses use Cache-Control: no-store to avoid stale published/draft exposure.
6. Add additive migration to API database, no seed data. Test fresh migrations and upgrade with existing unrelated data preserved; document backup, migration, application rollback retaining table, and destructive down-migration warning. Existing startup migration configuration applies.
7. OpenAPI contract precedes implementation; integration guide includes example public and admin requests/responses, limits, status codes, CORS configuration, and https://api.markhazleton.com/api/public/family-memories as integration URL on documented host. VITE_FAMILY_MEMORIES_API_URL is build-time; changes require frontend rebuild and deployment. Verify hosting references before final delivery; do not claim deployment.
8. Meaningful API/database and CMS tests cover AC1–7, malformed/null/oversize input, all management authorization boundaries, failures, preservation, lifecycle, full collection and CORS. Run existing regression suites and CMS build/lint. No new dependencies, services, secrets, feedback endpoint, production content or external publication.

## Operational constraints and backbone coverage
I: checked-in OpenAPI before code. II: tests alongside implementation. III/VII: existing framework and database, no new packages/services. IV/VIII: explicit AdminOnly management, anonymous published projection only; published editorial content is public content, never raw CMS records. IX: API SQLite migration, production DefaultConnection under /home/data; no deployment edits to data. X: no secrets. V: this spec/tasks; VI: no framework payload modifications.
Full collection is intentionally unpaginated for this low-volume use case; one indexed query, no N+1. Operators monitor existing request status/duration logging and database backups. Unpublish/delete cannot recall copies already fetched by visitors. Editors are responsible for consent to publish personal text. No extra regulatory workflow is introduced.

## Open Questions
None. Clarification scan: scope, data lifecycle, edge cases, security, integration, constraints and testability covered. Remaining implementation choices fall within the user's authorization.

## Tasks
- [X] T001 Define OpenAPI and guide in bold-docs/system/contracts/family-memories.yaml and bold-docs/system/family-memories-api.md (AC1–8).
- [X] T002 Add model/repository/service/endpoints in src/MakeBoldSpark.Api/Features/FamilyMemories/, context migration and tests in tests/MakeBoldSpark.Api.Tests/Features/FamilyMemories/ (AC1–3,5–6,8).
- [X] T003 Integrate configuration-driven CMS editor and tests in src/MakeBoldSpark.Cms/client/src/cms/ (AC4,8).
- [X] T004 Run migrations, API regressions, CMS tests/build/lint; record results and integration URL in bold-docs/features/0006-family-memories/validation.md (AC6–8).

## Waivers
None.



