# ADR 0001: Single Backend API

## Status
Accepted

## Context
MakeBoldSpark hosts several very small personal/project APIs. The workloads are low-volume and share common concerns such as authentication, logging, data access, content publishing, and deployment.

## Decision
Use one modular ASP.NET Core backend API rather than separate services or repositories for each API.

## Consequences
This reduces hosting cost, deployment complexity, and repository sprawl. It increases coupling, so features must maintain clear route and code boundaries. APIs may be split later if scale, security, reliability, or release independence requires it.
