# ADR 0002: SQLite Default With Selective Cosmos

## Status

Accepted

## Context

The platform needs a low-cost relational data model for CMS/content features. The maintainer prefers EF Core and SQLite for portability and simplicity. Cosmos DB experience is also valuable but should not be forced where relational storage is better.

## Decision

Use EF Core + SQLite as the default persistence model with WAL journal mode for concurrent read access. Use Cosmos DB selectively for document-shaped features or portfolio demonstrations.

## Consequences

The app remains inexpensive and portable. SQLite requires careful handling of persistence (production data at `/home/data/makeboldspark.db`), backup, and single-instance hosting. Cosmos features must be optional and configuration-driven.
