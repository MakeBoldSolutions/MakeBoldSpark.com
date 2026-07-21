# Changelog

<!-- markdownlint-disable MD024 -->

## [v1.1.0] - 2026-06-23

### Added

- **CMS Admin App**: A branded administrative SPA for authenticated management
  of CMS content, navigation, reusable content, subscribers, newsletters, and
  mail settings.
- **Administrator sign-in**: Credential verification and signed JWT issuance
  for administrator access, with password hashing and login throttling.

### Changed

- Admin access now validates signed credentials instead of accepting any
  well-formed token.

### Architectural Decisions

- **0008**: Secure administrator authentication with signed JWTs.
- **0009**: Embedded CMS SPA as an in-solution project.
- **0010**: Configuration-driven generic CMS CRUD interface.
- **0011**: Reuse shared MakeBold brand assets in the CMS SPA.

### Contributors

- Mark Hazleton

## [v1.0.0] - 2026-06-21

### Added

- **Platform foundation**: Modular .NET 10 API platform with SQLite-backed data,
  OpenAPI, authorization boundaries, health checks, and deployment automation.
- **Static content pipeline**: Eleventy-based Markdown authoring for system and
  insight pages, with safe generated-output replacement and API-build integration.

### Changed

- Generated system, insight, and catalog output now replaces hand-authored
  client-side rendering.

### Architectural Decisions

- **0006**: Static content build pipeline for generated website content.

### Contributors

- Mark Hazleton
- markhazleton
