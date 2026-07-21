# Project Guide

## Bold Commands

This project uses Bold for spec-driven development. Available slash commands:

- `/bold-plan` — Triage the request, ratify a tier, produce the tier-appropriate planning artifact
- `/bold-plan-clarify` — Resolve ambiguities in specs or plans
- `/bold-plan-analyze` — Check spec consistency, duplication, and requirement/task coverage
- `/bold-plan-critic` — Adversarial risk critique before committing
- `/bold-plan-checklist` — Generate or verify a requirements-quality checklist
- `/bold-plan-tasks` — Regenerate the task breakdown for the active feature
- `/bold-build` — Apply the ratified tier's gate set and execute
- `/bold-build-status` — Report task/gate/artifact status for the active feature
- `/bold-ship` — Draft a PR (or finalize the deliverable) for the active feature
- `/bold-ship-review` — Review pass over changes before or after a PR is opened
- `/bold-ship-address` — Respond to review findings on an open PR
- `/bold-ship-harvest` — Classify feature artifacts: promote durable knowledge, archive work products

See `.bold/commands/` for the full command surface.

## Backbone

Read `bold-docs/backbone.md` before making changes — it defines the project's principles. Supporting architecture/tech-stack/workflow detail lives in `bold-docs/system/architecture.md`.

## Tech Stack

- C# / .NET (ASP.NET Core, Web API)
- API-first: OpenAPI/Swagger contracts defined before implementation
- Test-first: tests written before or alongside implementation
- `src/MakeBoldSpark.Web/` — Eleventy (`@11ty/eleventy`) static content build, Node.js LTS. Build-time only; not a runtime dependency of `MakeBoldSpark.Api` and not part of `MakeBoldSpark.slnx`. Generates `MakeBoldSpark.Api/wwwroot/{insights,systems}/**` and `wwwroot/assets/makebold/catalog.json` (see `.archive/releases/v1.0.0/specs/004-static-site-pipeline`).

## Upgrading Bold

To update Bold stock files while preserving all customizations:

```
/bold-install
```
