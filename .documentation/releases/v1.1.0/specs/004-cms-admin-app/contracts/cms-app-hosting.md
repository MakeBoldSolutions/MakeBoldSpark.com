# Contract: CMS App Hosting & Routing

**Mounted by**: `app.MapMakeBoldSparkCms()` (new extension method in `MakeBoldSpark.Cms`), called from `Program.cs` alongside the existing `app.MapApiTestSpark(...)` call.

## Routes

| Route | Behavior |
|---|---|
| `GET /cms` and `GET /cms/{*path}` (static asset paths, e.g. `/cms/assets/index-abc123.js`) | Served from the `MakeBoldSpark.Cms` assembly's embedded `build/` resources via `ManifestEmbeddedFileProvider` + `UseStaticFiles({ RequestPath: "/cms" })` |
| `GET /cms/{*path}` (any path not matching a physical embedded asset — client-side SPA routes like `/cms/posts`) | Falls back to the embedded `index.html` via `MapFallbackToFile("/cms/{*path}", "index.html", ...)`, so client-side routing (e.g. selecting a blog, opening a post editor) survives a full page reload or direct link |

No `/cms/config` endpoint is introduced (see `research.md` — superseded by the real login endpoint; nothing left for a config bridge to gate).

## Data calls from the SPA

All data operations call the **existing** documented routes directly, same-origin, no proxy or bridge:

- `POST /api/public/auth/login` (new, this feature — see `contracts/auth-api.md`)
- `GET /api/public/makeboldspark/*` (existing, anonymous reads — used for non-sensitive lookups where convenient)
- `GET|POST|PUT|DELETE /api/admin/makeboldspark/*` (existing, `AdminOnly` policy — the access token returned by login satisfies this policy via its `role: Admin` claim, see `data-model.md`)

## Brand asset references

The SPA's `index.html`/CSS reference the platform's existing served assets directly — no duplication:

- `/assets/makebold/brand.css`
- `/assets/makebold/fonts/...` (e.g. InterTight)
- `/assets/makebold/logos/...`

These are already served by `MakeBoldSpark.Api`'s existing `UseStaticFiles()` call over `wwwroot/assets/makebold/**` (confirmed present in the current build output) — the CMS app takes a same-origin dependency on them rather than re-deriving from `.documentation/branding/MakeBoldSolutions/`.

## Dev-mode variant

`npm run dev` inside `src/MakeBoldSpark.Cms/client` runs a Vite dev server on `localhost:5173` (already permitted by `CorsSetup.cs:25`), proxying `/api/*` to `http://localhost:5290` so the same relative-path code works in both dev and embedded/production modes.
