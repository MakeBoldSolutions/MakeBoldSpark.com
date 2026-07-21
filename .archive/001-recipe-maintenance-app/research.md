# Research: Recipe Maintenance Application

## Decisions

### Embedded client hosting

**Decision**: Create `MakeBoldSpark.Recipe.Client` as an embedded SPA library hosted by `MakeBoldSpark.Api` at `/recipes`.

**Rationale**: This reuses the CMS build, asset-serving, fallback-routing, same-origin, and deployment pattern while preserving the constitution's single-backend rule.

**Alternatives considered**:

- Separate static-site deployment: rejected because it adds CORS, deployment, and environment coordination without a user requirement.
- Add recipe screens to the CMS: rejected because the request requires a dedicated maintenance application.

### Publisher inventory contract

**Decision**: Add publisher-only `GET` inventory/detail endpoints under `/api/publish/recipes` that require `domainId`, include unapproved records, and paginate recipe inventory.

**Rationale**: The existing anonymous routes intentionally return public data only and cannot satisfy complete, domain-bounded maintenance.

**Alternatives considered**:

- Reuse anonymous reads: rejected because unapproved records are absent and the access boundary is wrong.
- Return every recipe in a single response: rejected because bounded inventories prevent unbounded response size and allow predictable performance.

### Optimistic concurrency

**Decision**: Add an application-managed integer `Version` concurrency token to recipes and categories. The client sends the record version on update/delete; the API returns `412 Precondition Failed` for a stale version.

**Rationale**: It prevents silent lost updates and is reliable with SQLite. The client can keep its edit buffer, show the conflict, and ask the publisher to reload.

**Alternatives considered**:

- Last-write-wins: rejected by FR-010.
- Timestamp comparison: rejected because client timestamp parsing and resolution are weaker than a discrete concurrency token.

### Category deletion

**Decision**: Refuse category deletion with `409 Conflict` when recipes remain assigned to the category.

**Rationale**: Recipe categorization is required and the database relationship is restrictive; explicit reassignment or recipe removal avoids data loss. The in-use check and deletion must be atomic, with a database restrict violation mapped to the same `409 Conflict` result if a concurrent assignment wins the race.

**Alternatives considered**:

- Cascade-delete recipes: rejected as destructive data loss.
- Reassign automatically: rejected because a replacement category must be consciously selected by the publisher.

### Domain selection

**Decision**: Add a publisher-authorized configured-domain inventory to the Recipe Maintenance API for client selection, then pass the selected `domainId` to every recipe/category request. The API validates record and category ownership against that domain.

**Rationale**: The role model permits publishers to work across configured domains, but a selected context prevents accidental cross-site changes.

**Alternatives considered**:

- One combined cross-domain inventory: rejected by FR-011.
- New per-domain publisher assignment model: rejected because the established authorization model is role-based and the feature does not request delegated domain access.

### Release safety

**Decision**: Apply a forward-only EF Core migration that initializes a safe version value for existing data, deploy application code and migration together, and retain the existing persistent database backup/restore path.

**Rationale**: The client cannot enforce conflict detection until the persistence model supports it. A forward fix is safer than attempting production SQLite rollback.

**Alternatives considered**:

- No persistence token: rejected by FR-010.
- Runtime-only version cache: rejected because it is lost on restart and fails with multiple processes.
