# Release Notes: v1.0.0

## Release Metadata

- **Version**: v1.0.0
- **Release Date**: 2026-06-21
- **Release Window**: repository inception → 2026-06-21
- **Previous Version**: None
- **Commit Range**: Initial history → `e5896be4cce5029b52cb4c19b716b388323f38c1`
- **Commits**: 54
- **Contributors**: 2
- **Merged PRs**: 2

## Highlights

This first release establishes the MakeBoldSpark API platform: a modular .NET 10 backend with SQLite persistence, public and administrative route boundaries, OpenAPI support, health endpoints, and deployment-oriented configuration.

It also introduces Markdown-first static content authoring. The Eleventy build safely generates systems, insights, and catalog data before each API build, replacing the retired browser-side rendering path while preserving existing generated output when a content build fails.

## New Features

### MakeBoldSpark Platform Foundation

Shared ASP.NET Core platform for low-volume personal and portfolio APIs, including database, authorization, observability, and deployment foundations.

**Spec**: [001-makeboldspark-foundation](specs/001-makeboldspark-foundation/spec.md)

### Static Content Build Pipeline

Markdown and JSON authoring pipeline for generated public site content, with safe build-and-swap behavior and preview support.

**Spec**: [004-static-site-pipeline](specs/004-static-site-pipeline/spec.md)

## Architectural Decisions

- [0006: Static Content Build Pipeline](../../decisions/0006-static-content-build-pipeline.md)

## Deferred Features

- **002-recipe-api**: Remains active for a future release.
- **003-makeboldspark-api**: Remains active for a future release.

## Upgrade Guide

Install the .NET 10 runtime and Node.js 20 or newer. API builds now regenerate the static content output automatically.

## Metrics

| Metric | Value |
| --- | --- |
| Features delivered | 2 |
| Bugs fixed | 0 |
| PRs merged | 2 |
| ADRs created | 1 |
| Contributors | 2 |
| Commits | 54 |
