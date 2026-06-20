# ADR 0005: Authentication Boundaries

## Status
Accepted

## Context
Public content clients need frictionless read access, while CMS/admin/publishing operations require protection.

## Decision
Expose public read-only endpoints anonymously. Require authorization for admin, publishing, backup, and integration endpoints. Register JWT Bearer as the default authentication scheme (returns 401 on challenge) with Azure App Service Authentication as the planned runtime identity provider.

## Consequences
The system remains simple for public clients while protecting state-changing operations. Additional identity complexity (OIDC, Azure Entra ID) can be added later by configuring the JwtBearer options without route changes.
