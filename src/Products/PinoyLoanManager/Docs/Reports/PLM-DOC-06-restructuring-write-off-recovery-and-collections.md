# PLM-DOC-06 — Restructuring, Write-Off, Recovery & Collections

**Status:** Documentation package complete (planning only)
**Implementation present:** No
**Last updated:** 2026-08-19
**Branch:** `docs/plm-final-decisions`

Runtime / browser / device / database / production validation: **Not Applicable**.

> **Historical note:** Open dependencies below reflect PLM-DOC-06 package completion. **PLM-D-00-07 Closed for MVP Product financial model** at final review. Final status: [../Decisions/PLM-decision-status-summary.md](../Decisions/PLM-decision-status-summary.md).

---

## Scope

Finalize Pinoy Loan Manager MVP product rules for restructuring, refinancing separation, Promise to Pay, Collection Cases, Write-Off, Recovery, and collection conduct boundaries.

**Out of scope:** code, database, migrations, APIs, UI, external GL, legal compliance claims, default numeric thresholds.

---

## Delivered

| Area | Canonical doc |
|---|---|
| Restructuring / hardship | [../Product/restructuring-and-hardship-policy.md](../Product/restructuring-and-hardship-policy.md) |
| Write-Off / Recovery | [../Product/write-off-and-recovery-policy.md](../Product/write-off-and-recovery-policy.md) |
| PTP / Collection Case | [../Product/collections-case-and-promise-to-pay-policy.md](../Product/collections-case-and-promise-to-pay-policy.md) |
| ADR-011 | [../Decisions/ADR-011-restructuring-refinancing-and-hardship.md](../Decisions/ADR-011-restructuring-refinancing-and-hardship.md) |
| ADR-012 | [../Decisions/ADR-012-write-off-recovery-and-collections-case-policy.md](../Decisions/ADR-012-write-off-recovery-and-collections-case-policy.md) |

---

## Key decisions

- Restructuring: same Loan, new schedule version, no capitalization of penalties/fees/interest into Principal
- Refinancing: deferred beyond MVP
- PTP: operational only; does not change schedule or count as Payment
- Write-Off: classification only; separate component tracking; stop post-write-off accrual
- Recovery: separate Payment identity; allocation Interest → Principal → Fees → Penalties
- Collection conduct: respectful recording; no abusive product features
- **PLM-D-00-08 Closed for MVP Product business/calculation policy**
- **PLM-D-00-07 Closed for MVP Product operational financial model** (persistence, journal/export, GL remain implementation work)

---

## Open dependencies

| ID | Status |
|---|---|
| PLM-D-00-07 | **Closed for MVP Product financial model** (historical: Open / Partially Resolved — persistence, journal/export, GL implementation deferred) |
| PLM-D-00-11 | Open — legal/compliance |
| Refinancing | Deferred beyond MVP |

---

## No-code statement

Documentation only. Implementation paused. Parked scaffold unmerged.

---

## Exact next documentation package

**PLM-DOC-07 — Borrower Onboarding, Application, Assessment, Approval & Disbursement Readiness**
