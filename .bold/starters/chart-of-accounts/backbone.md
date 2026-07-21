1. **Every account number falls in exactly one standard block** (1000s assets, 2000s liabilities, 3000s equity, 4000s revenue, 5000s cost of goods sold, 6000s operating expense, 7000s other income/expense) — no account improvises its own numbering scheme.
   <!-- source: starter(chart-of-accounts) -->
   **Status**: enforced

2. **Cost of goods sold is separated from operating expense for every revenue-generating activity** — a "materials" or "job costs" account blended into general overhead is treated as a defect to fix, not a starting point to preserve.
   <!-- source: starter(chart-of-accounts) -->
   **Status**: enforced

3. **When `coa.revenue_lines` is more than 1, each revenue line gets its own revenue account and, wherever direct-cost tracking is possible, its own COGS sub-accounts** — a chart that can't say which line made money isn't answering the question a chart of accounts exists to answer.
   <!-- source: starter(chart-of-accounts) -->
   **Status**: adopting — waivable when line-level margin genuinely isn't decision-relevant yet; waiver must be named in Migration Notes

4. **Every account carries a plain-language description a non-bookkeeper owner can read cold** — an account name alone is not documentation.
   <!-- source: starter(chart-of-accounts) -->
   **Status**: enforced
