# 0006: Static Content Build Pipeline

## Status

Accepted

## Context

Adding a system or insight page required copying and editing complete HTML shells,
which caused duplicated navigation and footer markup to drift. The public site
already serves static files from the API application's `wwwroot`, so the content
workflow needed a source-controlled authoring path without changing the API
runtime.

## Decision

Use a standalone Eleventy project to author insights in Markdown and system
metadata in JSON. Build output is generated into a temporary directory and
replaces `wwwroot/insights`, `wwwroot/systems`, and
`wwwroot/assets/makebold/catalog.json` only after success. The API MSBuild target
runs the static build before each API build.

## Consequences

### Positive

- Content authors edit Markdown and structured metadata rather than repeated
  HTML shells.
- Failed static builds preserve the prior generated output.
- Every API build refreshes static content automatically.

### Negative

- API builds require the pinned Node/npm toolchain and run `npm ci`.
- Generated paths must not be edited directly.

## Source

- **Spec**: 004-static-site-pipeline
- **Release**: v1.0.0
- **Date**: 2026-06-21
