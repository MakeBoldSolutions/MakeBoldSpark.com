# ADR 0003: Static Web App Clients

## Status
Accepted

## Context
Most public websites are content-focused and do not require live database access on every page load.

## Decision
Use Azure Static Web Apps for public clients. Clients should consume generated static JSON by default and call live public APIs only when dynamic behavior is required.

## Consequences
Public sites remain fast, inexpensive, and resilient. The backend API becomes the authoring, publishing, integration, and preview layer rather than the runtime dependency for every page view.
