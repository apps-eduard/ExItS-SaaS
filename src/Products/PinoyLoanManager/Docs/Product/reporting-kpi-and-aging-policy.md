# Pinoy Loan Manager — Reporting KPI and Aging Policy

**Status:** Accepted product policy (PLM-DOC-08); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Operational KPI formulas, PAR definitions, aging buckets, and report catalog. Not statutory accounting ratios, GL integration, or implemented report queries.

**Canonical companions:** [reporting-baseline.md](reporting-baseline.md), [delinquency-and-missed-payment-policy.md](delinquency-and-missed-payment-policy.md), [write-off-and-recovery-policy.md](write-off-and-recovery-policy.md), [../Architecture/loan-ledger-and-balance-model.md](../Architecture/loan-ledger-and-balance-model.md), [../Architecture/operational-subledger-and-accounting-boundary.md](../Architecture/operational-subledger-and-accounting-boundary.md). ADR: [../Decisions/ADR-015-documents-receipts-and-reporting-policy.md](../Decisions/ADR-015-documents-receipts-and-reporting-policy.md).

---

## Scope and authority

- All reports and KPIs are derived from the **operational Loan subledger** and related PLM facts.
- Reports are **not** the organization General Ledger.
- All reports apply the same **server-authoritative resource scopes** as operational screens (Organization, Branch, Assigned Work, Own Session). See [../Security/resource-scope-and-data-minimization-policy.md](../Security/resource-scope-and-data-minimization-policy.md).
- Collectors do not receive unrestricted organization-wide financial browse by default.

Do **not** claim these formulas are statutory accounting ratios or legally compliant portfolio metrics (PLM-D-00-11 Open).

---

## Core KPI definitions

### GROSS OUTSTANDING PRINCIPAL

```
GROSS OUTSTANDING PRINCIPAL =
  Sum of Outstanding Principal
  for Loans that are not Settled and not Cancelled
```

Includes Active, Past Due, Matured Past Due, and other non-settled lifecycle states with remaining principal.

Written-Off Loans are **excluded** from active portfolio principal totals unless a report explicitly targets write-off/recovery views.

---

### PAST-DUE SCHEDULED AMOUNT

```
PAST-DUE SCHEDULED AMOUNT =
  Sum of unpaid scheduled components
  whose effective due date has passed
```

Uses the Loan's snapshotted schedule and posted allocation state. Includes unpaid principal, finance charge, and scheduled fee components that are past due. Penalties assessed separately are reported in penalty views unless explicitly included in a configured scheduled component.

---

### COLLECTION RATE FOR PERIOD

```
COLLECTION RATE FOR PERIOD =
  (Payments allocated during the period to scheduled obligations due in that period)
  ÷
  (Scheduled obligations due in that period)
  × 100
```

Rules:

- **Numerator:** only allocations to obligations whose due date falls within the report period.
- **Exclude** ordinary advance payments against future obligations from the numerator.
- Period boundaries use Branch-local collection dates unless a report explicitly states otherwise.
- Currency: PHP; amounts at posted precision (2 dp).

---

### PAR-X (Portfolio at Risk)

```
PAR-X =
  (Outstanding Principal of Loans whose DPD is at least X)
  ÷
  (Gross Outstanding Principal of active/matured-past-due portfolio)
  × 100
```

Where:

- **DPD** = Days Past Due per [delinquency-and-missed-payment-policy.md](delinquency-and-missed-payment-policy.md)
- **Denominator** = Gross Outstanding Principal of the active/matured-past-due portfolio (non-settled, non-cancelled; Written-Off excluded)
- **Numerator** = Outstanding Principal of Loans with DPD ≥ X

### Standard PAR report views

| View | Threshold |
|---|---|
| PAR 1 | DPD ≥ 1 |
| PAR 7 | DPD ≥ 7 |
| PAR 30 | DPD ≥ 30 |
| PAR 60 | DPD ≥ 60 |
| PAR 90 | DPD ≥ 90 |

Written-Off Loans are **excluded** from active PAR denominator and reported separately. See [write-off-and-recovery-policy.md](write-off-and-recovery-policy.md).

---

## Aging buckets

Standard operational aging by DPD:

| Bucket | DPD range |
|---|---|
| Current | 0 |
| 1–7 | 1 through 7 |
| 8–30 | 8 through 30 |
| 31–60 | 31 through 60 |
| 61–90 | 61 through 90 |
| 91+ | 91 and above |

Additional identifiable categories (not merged into standard buckets):

- **Matured Past Due** — separately identifiable
- **Written-Off** — separately identifiable; excluded from active PAR denominator

Aging reports show Outstanding Principal and may optionally show scheduled-amount or total-outstanding breakdowns by component where authorized.

---

## Additional operational reports (MVP plan)

Reports must respect scope filters. Planned report areas:

| Area | Reports |
|---|---|
| Origination | Application/request pipeline; approval/rejection; awaiting Disbursement |
| Disbursement / collections | Disbursement; due/collected/missed; Collector activity |
| Customer conduct | PTP kept/broken |
| Lifecycle | Restructuring; settlement/prepayment |
| Exceptions | Penalty/waiver/reversal |
| Cash operations | Cashier sessions; float/remittance; cash variance |
| Write-off / recovery | Write-off; recovery |
| Borrower / Personal | Personal-linked/unlinked Borrowers |
| Audit / security | Audit/high-risk actions; Owner Override usage |

Dashboard indicators listed in [reporting-baseline.md](reporting-baseline.md) may use these formulas where applicable.

---

## Write-off and recovery reporting

- Written-Off Loans appear in dedicated write-off/recovery reports, not in active PAR denominator.
- Recovery Payments are separately reportable.
- Written-off components (Principal, Interest/Finance Charge, Fees, Penalties) remain separately identifiable.

---

## Legal / accounting boundary

| Claim | Status |
|---|---|
| Operational KPI formulas approved for MVP planning | Yes |
| Statutory NPL/regulatory ratio compliance | **No** (PLM-D-00-11 Open) |
| GL / external accounting integration | **No** (PLM-D-00-07 remainder Open) |
| Implemented report engine | **No** |

---

## Honesty gates

| Claim | Allowed? |
|---|---|
| GROSS OUTSTANDING PRINCIPAL, PAST-DUE SCHEDULED AMOUNT, COLLECTION RATE, PAR-X formulas approved | Yes |
| Aging buckets Current / 1–7 / 8–30 / 31–60 / 61–90 / 91+ approved | Yes |
| Scope-filtered reporting requirement approved | Yes |
| Legally mandated portfolio reporting | **No** (PLM-D-00-11 Open) |
| Implemented | **No** |
