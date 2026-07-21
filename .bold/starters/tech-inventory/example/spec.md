# Prairie Comfort Mechanical, LLC — Tech Inventory

> **TL;DR for the Product Owner**
> *What*: A first written inventory of the 7 tools Prairie Comfort actually runs on — what each costs, who owns it, and which are core vs. convenience — naming one real redundancy and one real gap.
> *Why*: Nobody had ever listed what the business pays for or who's responsible for each tool; the 4th-crew roadmap item (company plan, row 1) is about to break the phone-and-whiteboard dispatch process that's been "good enough" at 3 crews.
> *Status*: Complete — produced as Bold in a Day's mock/practice engagement (`bold-tool-plan.md` §14, E2). **Prairie Comfort Mechanical is a fictional client, invented to iterate the Finance-flavor delivery kit before any real pilot — not a real business.**
> *Decision needed*: none.

**Tier**: Feature
**Status**: Complete

## Intent

Companion to the `company-plan`, `chart-of-accounts`, and `close-checklist` starters' worked examples — same fictional client. The company plan's primary goal (stand up a 4th crew) is what makes this inventory urgent now: the dispatch gap named below is exactly what a 4th crew would break first.

## Company Snapshot

- **Estimated tool count**: 7
- **Review cadence**: Annual
- **Owner assigned per tool today**: No — 2 of 7 tools have no named owner

## Tool List

| Tool | Category | Owner | Recurring Cost | Core or Convenience | Notes |
|---|---|---|---|---|---|
| QuickBooks Online | Accounting | Office Manager | $90/mo | Core | Source of the new chart of accounts (`chart-of-accounts` starter example) |
| Job-Cost Spreadsheet (Excel) | Job costing | Office Manager | $0 license; ~4 hrs/mo rebuild time | Core (through transition) | Scheduled for retirement per the chart of accounts' Migration Notes, once QBO Projects job costing is live |
| Gusto | Payroll | Office Manager | $180/mo | Core | |
| Google Workspace | Email & calendar | *(none named)* | $72/mo (6 licenses) | Core | No named admin owner — flagged below |
| Phone & text | Dispatch | Dispatcher | $0 | Core | Entire dispatch system today: no software, no job-status record beyond memory and the whiteboard |
| Paper whiteboard | Crew scheduling | Dispatcher | $0 | Core | Actually used day-to-day |
| Shared Google Calendar | Crew scheduling | *(none named)* | $0 (included in Workspace) | Convenience | Rarely updated; duplicates the whiteboard without replacing it |

## Redundancies & Gaps

- **Redundancy**: the paper whiteboard and the shared Google Calendar both claim to be "where crew scheduling lives," but only the whiteboard is actually kept current — the Calendar is stale and nobody owns updating it.
- **Gap (core-operational, highest priority)**: dispatch has no software at all — phone calls, texts, and a whiteboard are the complete system. This works at 3 crews because Dale personally tracks most of it in his head; the company plan's primary goal (stand up a 4th crew) removes that headroom.
- **Ownership gap**: Google Workspace (email + the redundant Calendar) has no named internal owner — nobody is responsible for adding/removing users or deciding whether the Calendar is worth keeping.

## Recommendations

1. Evaluate a lightweight field-service dispatch/scheduling tool before the 4th crew stands up — the highest-priority item, directly blocking the company plan's primary goal.
2. Retire the shared Google Calendar (or repurpose it as the dispatch tool's calendar view, if the chosen tool integrates with Google) — resolves the redundancy either way.
3. Retire the job-cost spreadsheet once the chart of accounts' revenue-line COGS split is live in QuickBooks and job-level tracking moves to QBO Projects — already scheduled, not new work.
4. Name an owner for Google Workspace admin — smallest fix on this list, currently nobody's job.

## Acceptance Criteria

- [x] Every tool has an owner, a cost, and a core/convenience label (owner explicitly marked "none named" where true, not omitted)
- [x] At least one redundancy is named by both tool names, not gestured at generally
- [x] The core-operational gap most relevant to the current roadmap (dispatch, tied to the 4th-crew goal) is called out as highest priority
- [x] Recommendations are ordered, not a flat list

## Affected Documents

- `company-plan` starter example (the 4th-crew roadmap row is what makes the dispatch gap urgent)
- `chart-of-accounts` starter example (the job-cost spreadsheet's retirement is already scheduled there)

## Tasks

- [x] T001 Interview Dale and the Office Manager on every tool currently paid for or relied on
- [x] T002 [P] Confirm cost and owner for each tool against the QuickBooks vendor list
- [x] T003 Identify redundancies by cross-checking which tools claim the same job
- [x] T004 Rank recommendations against the company plan's primary goal
- [x] T005 Ratify the recommendation order with Dale before marking complete
