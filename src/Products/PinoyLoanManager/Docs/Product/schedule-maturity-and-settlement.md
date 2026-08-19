# Pinoy Loan Manager — Schedule, Maturity, and Settlement

**Status:** Planning index; calendar/maturity accepted in PLM-DOC-03; settlement/prepayment accepted in PLM-DOC-04
**Implementation present:** No
**Last updated:** 2026-08-19

Pointer to accepted schedule, delinquency, penalty, maturity, and settlement policies. Restructuring and write-off remain later packages.

**Canonical PLM-DOC-03 policies:**

- [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md)
- [delinquency-and-missed-payment-policy.md](delinquency-and-missed-payment-policy.md)
- [penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md)
- [maturity-and-post-maturity-policy.md](maturity-and-post-maturity-policy.md)

**Canonical PLM-DOC-04 policies:**

- [early-settlement-and-principal-prepayment-policy.md](early-settlement-and-principal-prepayment-policy.md)

ADRs: [../Decisions/ADR-005-schedule-calendar-and-exception-treatment.md](../Decisions/ADR-005-schedule-calendar-and-exception-treatment.md), [../Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md](../Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md), [../Decisions/ADR-007-early-settlement-and-prepayment-policy.md](../Decisions/ADR-007-early-settlement-and-prepayment-policy.md).

Related: [financial-calculation-baseline.md](financial-calculation-baseline.md), [payment-allocation-and-prepayment-policy.md](payment-allocation-and-prepayment-policy.md), [loan-lifecycle-model.md](loan-lifecycle-model.md), [penalty-exception-and-waiver-model.md](penalty-exception-and-waiver-model.md).

---

## Schedule generation

The future schedule engine creates **explicit installments** from snapshotted inputs (disbursement date, first due date, frequency, installment count, collection calendar, adjustment rule, exception policy, time zone, versions). Term labels alone are insufficient.

Installment financial states: Future, Due, Partially Paid, Paid, Past Due — separate from Collection Attempt outcomes.

Rounding residuals reconcile via the final applicable installment: [money-precision-and-rounding-policy.md](money-precision-and-rounding-policy.md).

---

## Calendar and first due (accepted)

Following Valid Collection Day. Same Day or Last Calendar Day for month-end. Quick Loan first due = next valid Collection Day after Disbursement. Traditional default = one full frequency interval after Disbursement.

Detail: [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md).

---

## Exceptions

Quick Loan default: Shift Future Due Dates. Traditional default: Fixed Schedule / Penalty Suppression. New schedule version; original remains visible.

---

## Maturity

Maturity Date = final effective scheduled due date of the current version. Reaching the date does not close the Loan or forgive balance. Remaining outstanding → Matured Past Due.

Detail: [maturity-and-post-maturity-policy.md](maturity-and-post-maturity-policy.md).

---

## Early / advance payment

MVP advance payment remains as in PLM-DOC-02: [payment-allocation-and-prepayment-policy.md](payment-allocation-and-prepayment-policy.md). Principal prepayment recalculation: [early-settlement-and-principal-prepayment-policy.md](early-settlement-and-principal-prepayment-policy.md).

---

## Early settlement

Settlement Quote is required. Canonical formula, rebate/accrual, and quote validity: [early-settlement-and-principal-prepayment-policy.md](early-settlement-and-principal-prepayment-policy.md). Legal review remains **OPEN** (PLM-D-00-11).

---

## Restructuring and write-off

Do **not** silently edit an existing schedule. Exact financial/accounting rules remain **OPEN**.

---

## Explicit non-goals

- Default penalty amounts or grace `N`
- Restructuring or write-off accounting
- Final enum / schema design
