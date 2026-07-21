# Prairie Comfort Mechanical, LLC — Close Checklist

> **TL;DR for the Product Owner**
> *What*: A written monthly close checklist — 9 steps, each with an owner and a due day — so the books are signed off by day 5 every month instead of "whenever the office manager gets to it."
> *Why*: The close currently has no calendar; it slips whenever a big job lands, and nobody but the preparer ever looks at the result.
> *Status*: Complete — produced as Bold in a Day's mock/practice engagement (`bold-tool-plan.md` §14, E2). **Prairie Comfort Mechanical is a fictional client, invented to iterate the Finance-flavor delivery kit before any real pilot — not a real business.**
> *Decision needed*: none.

**Tier**: Feature
**Status**: Complete

## Intent

Companion to the `company-plan` and `chart-of-accounts` starters' worked examples — same fictional client. The new chart of accounts (revenue-line COGS separation) only pays off if the close actually reconciles to it every month; this checklist is what makes that routine instead of aspirational.

## Company Snapshot

- **Close period**: Monthly
- **Deadline**: 5 business days after period-end
- **Accounting software**: QuickBooks Online
- **Named reviewer**: Yes — Dale Whitfield (Owner), distinct from the Office Manager who prepares the close

## Close Calendar

| Day | Milestone |
|---|---|
| Period-end | Bank feed and job-cost spreadsheet frozen for the month |
| Day 2 | All reconciliations complete, drafted close ready for review |
| Day 4 | Dale's review complete, any corrections posted |
| Day 5 | Fully signed off |

## Checklist Steps

| # | Step | Owner | Target Account / Reconciliation | Day Due |
|---|---|---|---|---|
| 1 | Reconcile bank feed to statement | Office Manager | 1000 Operating Checking | Day 1 |
| 2 | Reconcile AR aging to open invoices | Office Manager | 1200 Accounts Receivable | Day 1 |
| 3 | Reconcile parts/materials on hand to the last physical count | Office Manager | 1300 Parts & Materials Inventory | Day 2 |
| 4 | Reconcile transition-period job-cost spreadsheet total to COGS by line, until the spreadsheet is retired per the chart of accounts' Migration Notes | Office Manager | 5000/5010, 5100/5110, 5200 | Day 2 |
| 5 | Reconcile AP aging to open bills | Office Manager | 2000 Accounts Payable | Day 2 |
| 6 | Reconcile payroll liabilities to the payroll provider's report | Office Manager | 2100 Payroll Liabilities | Day 2 |
| 7 | Reconcile deferred maintenance-contract revenue to the active-contract schedule | Office Manager | 2400 Deferred Maintenance Revenue | Day 2 |
| 8 | Confirm revenue booked to the correct line per job type | Office Manager | 4000, 4100, 4200 | Day 3 |
| 9 | Review and sign off | Dale Whitfield | Full close package | Day 5 |

## Escalation Path

If any step is not complete by its due day, the Office Manager flags it to Dale the same day by text — not silently carried into next month's close. If Dale's own Day 5 review surfaces a correction, it's posted and re-reviewed before sign-off; the close isn't marked complete until Dale has actually signed off, not merely received the package. Principle 3 (named reviewer) is not waived here — Dale is the reviewer of record.

## Acceptance Criteria

- [x] Every step names an owner and a specific target account or reconciliation
- [x] The close has a calendar (period-end through day 5), not just a step list
- [x] A named reviewer distinct from the preparer signs off before the close is marked complete
- [x] The escalation path names who is told and how fast when a step slips

## Affected Documents

- `chart-of-accounts` starter example (this checklist's reconciliation targets are that chart's accounts)
- `company-plan` starter example (row 2 of its roadmap — job costing by line — is what this checklist makes routine)

## Tasks

- [x] T001 Interview the Office Manager on the current (undocumented) close routine
- [x] T002 Map each existing informal step to a chart-of-accounts target or drop it if it has none
- [x] T003 [P] Set the close calendar against `core.close.days_to_close`
- [x] T004 Ratify the named-reviewer step with Dale
- [x] T005 Confirm the escalation path with both Dale and the Office Manager before marking complete
