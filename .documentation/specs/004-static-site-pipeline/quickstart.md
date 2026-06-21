# Quickstart: Static Content Build Pipeline

**Phase**: 1 — Design & Contracts
**Branch**: `004-static-site-pipeline`

## One-time setup

```bash
cd src/MakeBoldSpark.Web
npm install
```

## Author a new article

1. Create `src/content/articles/my-new-article.md` with front matter (see `contracts/content-frontmatter.md`).
2. Run `npm run serve` and open the printed local URL to preview — edits to the file update the preview within a few seconds (SC-003), no need to run the .NET app.
3. When satisfied, run `npm test` to validate the isolated build path, then run `npm run build` once to produce the final output. During initial migration, do not run the production build until the pre-migration baseline is captured and all existing pages are ready to generate.
4. Commit the new `.md` file. Nothing else needs to be hand-edited (SC-001) — the build regenerates `wwwroot/insights/index.html`, `wwwroot/insights/{system}/index.html`, and `wwwroot/assets/makebold/catalog.json` automatically.

## Add or update a System

1. Edit `src/_data/systems.json` directly.
2. Run `npm run build` (or `npm run serve` while iterating).
3. Commit `systems.json`. The corresponding `/systems/{id}/` page and every listing that references it are regenerated.

## Verify nothing is stale before publishing

`dotnet build` on `MakeBoldSpark.Api` runs the Eleventy build automatically via the `BeforeTargets="Build"` MSBuild target — you do not need to remember to run `npm run build` yourself before building or publishing. If Node/npm isn't installed or the static build fails, `dotnet build` fails loudly rather than producing stale content.

Every API `dotnet build` invokes the Node toolchain and regenerates static content. `dotnet test` does not invoke it unless the test command first builds the API project.

## Removing content

Delete or rename the `.md` file (or remove the entry from `systems.json`) and run `npm run build`. The previously-generated page is removed automatically (FR-011) — no manual cleanup of `wwwroot/insights/**` or `wwwroot/systems/**` is needed or expected.
