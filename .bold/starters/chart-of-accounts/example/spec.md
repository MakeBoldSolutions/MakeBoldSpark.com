# Prairie Comfort Mechanical, LLC — Chart of Accounts

> **TL;DR for the Product Owner**
> *What*: Replaces one catch-all "Job Materials" account with a chart that separates COGS by each of the three revenue lines, so bid margin is readable straight out of QuickBooks.
> *Why*: The company plan's roadmap named this as the prerequisite for knowing which jobs actually make money without an off-system spreadsheet rebuild every month.
> *Status*: Complete — mock/practice engagement (`bold-tool-plan.md` §14, E2). **Prairie Comfort Mechanical is a fictional client, invented to iterate the Finance-flavor delivery kit before any real pilot — not a real business.**
> *Decision needed*: none.

**Tier**: Feature
**Status**: Complete

## Intent

Companion to the `company-plan` starter's worked example — same fictional client. Feature tier: this replaces the account structure everything downstream (job costing, the fractional check-in's monthly review) depends on.

## Company Snapshot

- **Accounting method**: Accrual (moving from an informal cash-ish practice)
- **Existing software**: QuickBooks Online
- **Revenue lines**: 3 — Install & Replacement, Service & Repair, Maintenance Contracts (matches the company plan's Mission & Offerings)
- **Job costing required**: Yes, by revenue line now; per-job costing is a stated future step, not built into this chart (see Migration Notes)

## Numbering Scheme

| Block | Range | Contents |
|---|---|---|
| Assets | 1000–1999 | Operating cash, AR, parts inventory, vehicles/equipment |
| Liabilities | 2000–2999 | AP, payroll liabilities, equipment loans, deferred maintenance-contract revenue |
| Equity | 3000–3999 | Owner's equity, retained earnings, owner draws |
| Revenue | 4000–4999 | One account per revenue line |
| Cost of Goods Sold | 5000–5999 | Materials, direct labor, subcontract labor, equipment rental — split by revenue line |
| Operating Expense | 6000–6999 | Admin, non-job vehicle, insurance, marketing, office |
| Other Income/Expense | 7000–7999 | Interest expense, gain/loss on equipment disposal |

## Account List

| # | Account | Type | Description |
|---|---|---|---|
| 1000 | Operating Checking | Asset | Primary operating bank account |
| 1200 | Accounts Receivable | Asset | Unpaid customer invoices |
| 1300 | Parts & Materials Inventory | Asset | HVAC parts/materials on hand, not yet job-assigned |
| 1500 | Vehicles & Equipment | Asset | Trucks, lifts, tools — fixed assets |
| 1510 | Accumulated Depreciation | Asset (contra) | Offsets 1500 |
| 2000 | Accounts Payable | Liability | Unpaid supplier/vendor bills |
| 2100 | Payroll Liabilities | Liability | Withheld taxes, benefits payable |
| 2300 | Equipment Loans Payable | Liability | Financed vehicles/equipment |
| 2400 | Deferred Maintenance Revenue | Liability | Cash received for maintenance contracts not yet performed |
| 3000 | Owner's Equity | Equity | Founder's capital contributions |
| 3100 | Retained Earnings | Equity | Accumulated prior-year earnings |
| 3200 | Owner Draws | Equity (contra) | Distributions to Dale |
| 4000 | Revenue — Install & Replacement | Revenue | New-install and full-system-replacement billings |
| 4100 | Revenue — Service & Repair | Revenue | Time-and-materials break-fix billings |
| 4200 | Revenue — Maintenance Contracts | Revenue | Recurring inspection-agreement billings |
| 5000 | COGS — Materials, Install | COGS | Parts/equipment installed, Install & Replacement jobs |
| 5010 | COGS — Direct Labor, Install | COGS | Field-tech hours billed to Install & Replacement jobs |
| 5100 | COGS — Materials, Service | COGS | Parts used on Service & Repair calls |
| 5110 | COGS — Direct Labor, Service | COGS | Field-tech hours on Service & Repair calls |
| 5200 | COGS — Direct Labor, Maintenance | COGS | Field-tech hours on scheduled maintenance visits |
| 5300 | COGS — Subcontract Labor | COGS | Outside trades (electrical, crane) subbed on any line, allocated by job |
| 5400 | COGS — Equipment Rental | COGS | Lifts/rental equipment tied to a specific job |
| 6000 | Office Salaries & Wages | Operating Expense | Office manager, dispatcher |
| 6100 | Vehicle Expense — Non-Job | Operating Expense | Fuel/maintenance not billed to a specific job |
| 6200 | Insurance — General Liability & Workers Comp | Operating Expense | |
| 6300 | Marketing & Business Development | Operating Expense | Rotary sponsorships, bid marketing |
| 6400 | Office & Admin | Operating Expense | Software (QuickBooks, field-service tool), supplies |
| 7000 | Interest Expense | Other Expense | Equipment loan interest |

## Migration Notes

The prior single "Job Materials" catch-all account is retired: its balance is allocated across 5000, 5100, and the new 1300 inventory account, based on the office manager's outside spreadsheet for the current fiscal year — then the spreadsheet is retired going forward, with everything posting directly to the new accounts.

Per-job costing (tagging each transaction to a specific job number, not just a revenue line) is named in the company plan's roadmap but deliberately **not** built into this chart — QuickBooks Online's built-in "Projects" feature handles job-level tracking on top of these accounts without needing job-specific GL accounts, which would make the chart unmanageably large. **This is the chart's one waived instance of backbone principle 3**: it satisfies revenue-line-level COGS now, and defers job-level tracking to a tool feature rather than a chart change.

## Acceptance Criteria

- [x] Every account number falls in exactly one standard block
- [x] All three revenue lines from the company plan have their own 4000-block account
- [x] COGS is separated from operating expense for every revenue line
- [x] Every account has a plain-language description
- [x] The old catch-all account has a stated, dated migration path, not just a note to "deal with it later"

## Affected Documents

- `company-plan` starter example (this chart implements that plan's roadmap row 2)
- QuickBooks Online chart of accounts (target system for import)

## Tasks

- [x] T001 Export current QuickBooks chart + trial balance
- [x] T002 [P] Draft numbering scheme against backbone principle 1
- [x] T003 [P] Draft revenue/COGS split by line against backbone principles 2–3
- [x] T004 Draft migration notes reconciling the old catch-all account balance
- [x] T005 Ratify the job-costing waiver (QBO Projects vs. per-job GL accounts) with Dale before finalizing
