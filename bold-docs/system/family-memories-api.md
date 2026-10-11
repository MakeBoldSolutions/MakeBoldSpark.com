# Family Memories API

Editors manage memories at `/cms/family-memories`. Public readers receive only published text. No public submission endpoint exists. Examples below are fictional, not seed content.

## Integration

Final integration URL on the repository's documented production API host:
`https://api.markhazleton.com/api/public/family-memories`

Deploy this API change before configuring the consumer. This document does not assert the feature is already deployed.

```dotenv
VITE_FAMILY_MEMORIES_API_URL=https://api.markhazleton.com/api/public/family-memories
```

This is a **build-time** Vite setting. Changing it requires rebuilding and deploying the consuming frontend. No API key or browser-held secret is needed.

```sh
curl -i 'https://api.markhazleton.com/api/public/family-memories' \
  -H 'Accept: application/json' -H 'Origin: https://texecon.com'
```

HTTP 200, application/json:
```json
{"success":true,"data":{"memories":[{"personName":"Fictional Contributor","personPronouns":"they/them","relationshipToJared":"Fictional family friend","body":"First paragraph.\n\nSecond paragraph with a [**Fictional Name**](https://example.com) link."}]}}
```

When nothing is published: `{"success":true,"data":{"memories":[]}}` with HTTP 200. The entire collection is returned, ordered by `sortOrder` ascending then immutable `id` ascending. Internal fields never appear publicly. Responses use `Cache-Control: no-store`; unpublishing cannot recall copies already fetched.

## Editing and validation

| Field | Limit (UTF-16 code units) | Rules |
|---|---:|---|
| personName | 200 | Required non-whitespace string |
| personPronouns | 100 | Required string, empty allowed; null rejected |
| relationshipToJared | 300 | Required non-whitespace string |
| body | 100,000 | Required non-whitespace string; long multi-paragraph text |
| sortOrder | 0–2,147,483,647 | Integer, defaults 0 |
| isPublished | boolean | Defaults false |

All four text fields must be present on POST and PUT. Strings are stored verbatim: no trimming, Unicode normalization, newline conversion, HTML conversion, or Markdown processing. JSON escape syntax may differ on the wire; decoded strings are preserved. The consumer supports paragraphs and HTTP(S) Markdown links, including bold labels, not full Markdown. Editors should enter that supported syntax; other text is not transformed or rendered by this API.

The CMS uses a plain textarea, numeric editorial order and Published checkbox. Save with Published checked to publish; clear it and save to unpublish. Change editorial order and save to reorder; equal values sort by ID. Delete uses the existing confirmation. Empty pronouns remain an empty string.

## Protected management API

Use an existing CMS administrator JWT (`Authorization: Bearer ...`) from the normal login flow. Publisher/service roles do not grant management access.

| Method | Path | Success |
|---|---|---|
| GET | /api/admin/makeboldspark/family-memories | 200, array including drafts and metadata |
| GET | /api/admin/makeboldspark/family-memories/{id} | 200, management record |
| POST | /api/admin/makeboldspark/family-memories | 201, record and Location |
| PUT | /api/admin/makeboldspark/family-memories/{id} | 200, updated record |
| DELETE | /api/admin/makeboldspark/family-memories/{id} | 204, no body |

```sh
curl -i 'https://api.markhazleton.com/api/admin/makeboldspark/family-memories' \
  -H "Authorization: Bearer $CMS_ADMIN_TOKEN" -H 'Content-Type: application/json' \
  --data '{"personName":"Fictional Contributor","personPronouns":"","relationshipToJared":"Fictional friend","body":"A fictional memory.\n\nAnother paragraph.","sortOrder":10,"isPublished":false}'
```

Example management response:
```json
{"id":1,"personName":"Fictional Contributor","personPronouns":"","relationshipToJared":"Fictional friend","body":"A fictional memory.\n\nAnother paragraph.","sortOrder":10,"isPublished":false,"createdDate":"2026-09-12T12:00:00Z","updatedDate":"2026-09-12T12:00:00Z"}
```

PUT uses the same body as POST, fully replacing editable fields; set `isPublished:true` to publish. IDs and UTC timestamps are server-owned; submitted metadata is ignored. Each write is atomic; the last committed edit wins, matching existing CMS behavior. Omitting sortOrder/isPublished resets them to 0/false on PUT.

Invalid input or malformed JSON returns 400; missing authentication 401; insufficient role 403; missing record 404; unexpected failures 500 with generic ProblemDetails. Unsupported JSON content type returns 415. Never interpret `success:false` in a 200 response as a failure: failures use non-2xx statuses. Validation errors use ProblemDetails with field-keyed `errors`.

## Browser configuration

The endpoint-specific `FamilyMemoriesPublic` CORS policy always allows `https://texecon.com`. Add explicit development origins in configuration, for example:
```json
{"FamilyMemories":{"DevelopmentOrigins":["http://localhost:5173","http://127.0.0.1:5173"]}}
```
Azure settings equivalents: `FamilyMemories__DevelopmentOrigins__0`, `FamilyMemories__DevelopmentOrigins__1`. Default list is empty. Origins include scheme and port, with no path or trailing slash. No wildcards, credentials or implicit development fallback. The policy allows GET and Accept; preflight requests are supported. Unconfigured origins receive no Access-Control-Allow-Origin header. Other APIs retain their existing AllowedOrigins policy. CORS is a browser rule, not access control for this intentionally public collection.

## Persistence and deployment

`FamilyMemories` lives in the existing API `MakeBoldSparkDbContext` using `DefaultConnection`, not the legacy Core context. Additive migration creates the table and publication/order index without modifying existing content. No memories are seeded. Production must keep DefaultConnection under `/home/data/` per repository policy; deployment must not overwrite database files.

Before deployment, take a consistent SQLite backup using the established backup process (account for WAL; do not copy a live database alone). Apply the migration through existing `Database:ApplyMigrationsOnStartup=true`, or `dotnet ef database update --project src/MakeBoldSpark.Api --context MakeBoldSpark.Api.Infrastructure.Data.MakeBoldSparkDbContext` with deployment configuration. Check startup migration logs and verify the public endpoint returns 200. A degraded startup with a missing table returns 500, not a misleading empty success.

Application rollback can retain the additive table and data. A down migration drops FamilyMemories and loses its content; use only after backup and explicit operational decision. Use existing request status/duration logs to detect failures; do not log memory bodies. Editors control consent for published personal text.

Machine-readable contract: [family-memories.yaml](contracts/family-memories.yaml). Runtime metadata is also emitted at `/openapi/v1.json` through the existing API setup. No proposed deviations from the requested contract.
