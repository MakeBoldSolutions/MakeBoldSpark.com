# Risk critique

Product Owner TL;DR: Principal risks are draft disclosure, text conversion, skipped migrations and stale browser content; the spec explicitly addresses each before implementation.

- Trust/auth: AC1 explicit public DTO, AC3 AdminOnly all management reads/writes, AC8 negative role tests.
- Secrets: AC5 anonymous CORS reads, AC8 no secrets/dependencies; existing login reused.
- Data continuity: AC6 additive migration and backup/rollback guide; AC3 atomic saves, AC4 existing delete confirmation.
- Validation: AC2 required strings, limits, null rejection and no normalization; AC8 malformed input tests.
- Errors/resilience: AC5 generic 500 and metadata logging; AC3 404; no external calls or retries added.
- Concurrency: AC3 explicitly follows existing last-committed-edit convention, immutable ID breaks ordering ties; no multi-record reorder transaction needed.
- Scale: AC1/operational constraints deliberately require full collection, one indexed query and no N+1. This is a low-volume collection, not a paginated general content store.
- Observability: AC5 reuses request status/duration logging, generic endpoint failure without text content.
- Deployment/rollback: AC6 uses API migration pipeline, tests existing-data upgrade, retains additive table for application rollback. Core's legacy pre-mark behavior is not used.
- Supply chain: AC8 prohibits new dependencies.
- Backward compatibility: additive endpoints/editor/context table; dedicated CORS policy preserves other routes.
- Privacy: published projection only, no response caching, editorial consent and inability to recall fetched copies are stated in operational constraints.

No open findings against the current spec. These are design checks, not claims of implementation validation.
