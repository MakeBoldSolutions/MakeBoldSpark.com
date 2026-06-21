# Project Guide

## DevSpark Commands

This project uses DevSpark for spec-driven development. Available slash commands:

- `/devspark.specify` — Define requirements and user stories
- `/devspark.plan` — Create implementation plan
- `/devspark.tasks` — Break plan into actionable tasks
- `/devspark.implement` — Execute tasks
- `/devspark.create-pr` — Draft or update a pull request
- `/devspark.pr-review` — Constitution-based PR review
- `/devspark.address-pr-review` — Resolve PR review findings
- `/devspark.quickfix` — Lightweight bug fix workflow
- `/devspark.critic` — Challenge assumptions before committing
- `/devspark.clarify` — Resolve ambiguities in specs or plans
- `/devspark.analyze` — Analyze spec consistency
- `/devspark.release` — Release workflow
- `/devspark.harvest` — Archive session context

See `.devspark/defaults/commands/` for the full list.

## Constitution

Read `.documentation/memory/constitution.md` before making changes — it defines the project's non-negotiable principles.

## Tech Stack

- C# / .NET (ASP.NET Core, Web API)
- API-first: OpenAPI/Swagger contracts defined before implementation
- Test-first: tests written before or alongside implementation
- `src/MakeBoldSpark.Web/` — Eleventy (`@11ty/eleventy`) static content build, Node.js LTS. Build-time only; not a runtime dependency of `MakeBoldSpark.Api` and not part of `MakeBoldSpark.slnx`. Generates `MakeBoldSpark.Api/wwwroot/{insights,systems}/**` and `wwwroot/assets/makebold/catalog.json` (see spec `004-static-site-pipeline`).

## Upgrading DevSpark

To update DevSpark stock files while preserving all customizations:

```
/devspark Follow the instructions at https://raw.githubusercontent.com/markhazleton/devspark/main/templates/commands/upgrade.md
```
