# Pinoy Loan Manager — Reporting Baseline

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Organization operational reporting intent. Not a KPI formula catalog, PAR definition, or accounting specification.

Related: [loan-documents-and-receipts.md](loan-documents-and-receipts.md), [notification-model.md](notification-model.md), [../Security/audit-and-history-baseline.md](../Security/audit-and-history-baseline.md), [../Architecture/loan-ledger-and-balance-model.md](../Architecture/loan-ledger-and-balance-model.md).

---

## Organization dashboard (potential indicators)

Future operational indicators **may** include:

- applications pending
- Quick Loan requests pending
- awaiting disbursement
- disbursements today
- collections due today
- collected today
- overdue accounts
- matured / past-due Loans
- Collector status
- unresolved cash variance
- pending penalty / exception approvals

Do **not** finalize financial KPI formulas. Do **not** define PAR unless explicitly approved later.

---

## Reporting areas

### Loans

- active
- settled
- overdue
- matured / past-due
- disbursements

### Collections

- due
- collected
- missed
- collector productivity / activity

### Financial operational reporting

- outstanding principal
- outstanding charges
- penalty
- payments
- settlement
- reversals

These are **operational** views derived from the Loan subledger. They are not the organization’s General Ledger.

### Cash operations

- cashier sessions
- collector float
- remittance
- variance
- disbursement cash

Loan ledger and cash accountability remain separate facts.

### Borrowers

- borrower portfolio
- linked / unlinked Personal relationships

### Audit

- approvals
- reversals
- waivers
- exceptions
- cash variance

Staff see reports according to grants and scope. Collectors do not receive unrestricted organization-wide financial browse by default. See [../authorization-matrix.md](../authorization-matrix.md).

---

## Legal / compliance boundary

No report, dashboard, or KPI in this document is claimed legally compliant or accounting-complete. External qualified legal/compliance review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations.

---

## Explicit non-goals

- PAR / NPL accounting formulas
- GL integration
- Exact KPI definitions
- Implementation of report queries
