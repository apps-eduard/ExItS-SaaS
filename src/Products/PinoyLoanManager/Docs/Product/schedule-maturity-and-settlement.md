# Pinoy Loan Manager — Schedule, Maturity, and Settlement

**Status:** Planning index; calendar/maturity accepted in PLM-DOC-03
**Implementation present:** No
**Last updated:** 2026-08-19

Pointer to accepted schedule, delinquency, penalty, and maturity policies. Early settlement, restructuring, and write-off remain later packages.

**Canonical PLM-DOC-03 policies:**

- [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md)
- [delinquency-and-missed-payment-policy.md](delinquency-and-missed-payment-policy.md)
- [penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md)
- [maturity-and-post-maturity-policy.md](maturity-and-post-maturity-policy.md)

ADRs: [../Decisions/ADR-005-schedule-calendar-and-exception-treatment.md](../Decisions/ADR-005-schedule-calendar-and-exception-treatment.md), [../Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md](../Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md).

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

MVP advance payment remains as in PLM-DOC-02: [payment-allocation-and-prepayment-policy.md](payment-allocation-and-prepayment-policy.md). Principal prepayment recalculation and early-settlement unearned-interest treatment remain **OPEN** (**PLM-DOC-04**).

---

## Early settlement

Settlement Quote remains required in a later package. Exact future-interest rebate/treatment remains **OPEN** (PLM-D-00-08 remainder, PLM-D-00-11).

---

## Restructuring and write-off

Do **not** silently edit an existing schedule. Exact financial/accounting rules remain **OPEN**.

---

## Explicit non-goals

- Default penalty amounts or grace `N`
- Early-settlement unearned-interest formula
- Restructuring or write-off accounting
- Final enum / schema design
