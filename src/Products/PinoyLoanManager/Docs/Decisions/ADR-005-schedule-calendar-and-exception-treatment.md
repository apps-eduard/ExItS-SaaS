# ADR-005 — Schedule calendar and exception treatment

**Status:** Accepted product policy (PLM-DOC-03); not implemented
**Date:** 2026-08-19
**Decisions:** Calendar/frequency/exception defaults accepted; **PLM-D-00-08** Open / Partially Resolved; **PLM-D-00-11** Open

> **Final status note:** **PLM-D-00-08 Closed for MVP Product business/calculation policy** at final review. See [PLM-decision-status-summary.md](PLM-decision-status-summary.md).

---

## Context

PLM needed Product Owner rules for due-date generation, collection calendars, first due dates, and how organization/service exceptions treat the schedule. Prior WP04 docs left calendar and excused-day treatment open.

---

## Decision

1. Financial event timestamps are UTC instants. Due/collection-business dates use snapshotted Organization/Branch local time zone. Never infer time zone from device or browser. Relocating a Collector does not change Loan due dates.
2. MVP frequencies: Daily (valid Collection Days only), Weekly (7 calendar days), Biweekly (14 calendar days), Semi-Monthly (two anchors; day 1–28 plus later day 2–28 or Month End), Monthly (day-of-month). No custom cron/scripts.
3. Non-collection-day adjustment: **Following Valid Collection Day** (forward only; no silent installment merge; cascade later installments on collision). Original calculated dates remain in generation evidence.
4. Monthly / Semi-Monthly Month End: **Same Day or Last Calendar Day**, then Following Valid Collection Day.
5. Quick Loan first due default: next valid Collection Day **after** Disbursement. Same-day first installment not MVP. Traditional default: one full selected frequency interval after Disbursement, then calendar. Explicit first due date may be set before Disbursement where the Loan Product allows. Borrower must see the actual first due date.
6. Installment financial states (Future / Due / Partially Paid / Paid / Past Due) are separate from Collection Attempt outcomes.
7. Exception policies: **Fixed Schedule / Penalty Suppression** and **Shift Future Due Dates** (new schedule version; original remains visible). Quick Loan default = Shift Future Due Dates. Traditional default = Fixed Schedule / Penalty Suppression. Snapshotted per Loan.

Canonical text: [../Product/schedule-and-collection-calendar-policy.md](../Product/schedule-and-collection-calendar-policy.md).

---

## Consequences

Schedule generation has an approved calendar contract.

**Still open:** early-settlement rebate, principal prepayment recalculation, restructuring, write-off, legal/compliance (PLM-D-00-11).

No schedule engine, schema, or implementation is authorized by this ADR.
