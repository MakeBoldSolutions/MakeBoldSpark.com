1. **Every close has a fixed calendar deadline** (`close.days_to_close` business days after period-end) — a close with no deadline drifts until the next one is already overdue too.
   <!-- source: starter(close-checklist) -->
   **Status**: enforced

2. **Every checklist step names an owner and ties to a specific account, reconciliation target, or report** — "review the books" is not a step; "reconcile 1000 Operating Checking to the bank statement" is.
   <!-- source: starter(close-checklist) -->
   **Status**: enforced

3. **The close has a named reviewer distinct from the preparer whenever `close.has_named_reviewer` is true** — a close nobody but its own preparer ever looks at is a control gap, not a completed close.
   <!-- source: starter(close-checklist) -->
   **Status**: adopting — waivable for a single-person accounting function; waiver must be named in the Escalation Path section

4. **A step that misses its deadline is escalated by name, not silently carried to next period** — the checklist's Escalation Path is what makes a missed step visible instead of quietly becoming "normal."
   <!-- source: starter(close-checklist) -->
   **Status**: enforced

5. **The checklist is reviewed against the current chart of accounts at least once a year** — a close step referencing a renamed or deleted account is a stale checklist, not a completed one.
   <!-- source: starter(close-checklist) -->
   **Status**: enforced
