# Pinoy Loan Manager — Financial Calculation Baseline

**Status:** Planning index; MVP methods accepted in PLM-DOC-02
**Implementation present:** No
**Last updated:** 2026-08-19

This document is the **terminology index** and pointer to accepted MVP calculation policies. It is **not** a calculation engine, default price list, or legally validated rulebook.

**Canonical PLM-DOC-02 policies:**

- [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md)
- [fees-and-net-proceeds-policy.md](fees-and-net-proceeds-policy.md)
- [payment-allocation-and-prepayment-policy.md](payment-allocation-and-prepayment-policy.md)
- [money-precision-and-rounding-policy.md](money-precision-and-rounding-policy.md)

ADRs: [../Decisions/ADR-003-supported-interest-and-schedule-methods.md](../Decisions/ADR-003-supported-interest-and-schedule-methods.md), [../Decisions/ADR-004-rounding-fees-and-payment-allocation.md](../Decisions/ADR-004-rounding-fees-and-payment-allocation.md).

Companions: [schedule-maturity-and-settlement.md](schedule-maturity-and-settlement.md), [loan-lifecycle-model.md](loan-lifecycle-model.md), [../Architecture/loan-ledger-and-balance-model.md](../Architecture/loan-ledger-and-balance-model.md), [quick-loan-model.md](quick-loan-model.md), [lending-operating-model.md](lending-operating-model.md).

Penalties, calendars, excused days, and post-maturity **engine** rules: [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md), [penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md). Default amounts remain undefined.

---

## Money terminology (accepted)

Do **not** assume that Principal, Net Proceeds, and Total Scheduled Repayment are always identical. UI, API, and domain must use **explicit names** instead of one ambiguous “Amount” or “Balance”.

| Term | Meaning |
|---|---|
| **Requested Amount** | Amount requested by the borrower |
| **Approved Principal** | Principal approved by the organization |
| **Contract / Face Principal** | Contractual principal on which the selected calculation policy operates |
| **Finance Charge / Interest** | Separately calculated charge for use of the principal |
| **Upfront Fee** | Disclosed fee deducted when money is released |
| **Financed Fee** | Disclosed fee added to the repayment obligation |
| **Scheduled Fee** | Disclosed fee assigned to one or more scheduled obligations |
| **Net Proceeds** | Cash/value actually released to the borrower |
| **Total Scheduled Repayment** | Total contractual amount expected under the original schedule |
| **Current Due** | Amount due for the current obligation/date |
| **Past Due** | Unpaid obligations whose due dates have passed |
| **Outstanding Principal** | Contract principal not yet satisfied |
| **Outstanding Charges** | Interest/finance charges, fees, or other approved non-principal obligations not yet satisfied |
| **Penalty** | Separately assessed late/delinquency charge ([penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md)) |
| **Total Outstanding** | All currently outstanding contractual components |
| **Settlement Amount** | Amount required by a valid settlement quote to fully settle the Loan |

Any displayed total must have a defined derivation. See [../Architecture/loan-ledger-and-balance-model.md](../Architecture/loan-ledger-and-balance-model.md).

---

## MVP methods (accepted)

| Origination | Methods |
|---|---|
| Quick Loan | Flat / Add-On only |
| Traditional Loan | Flat / Add-On or Reducing-Balance Equal-Installment |

Reducing-balance + deducted-interest is **prohibited**. Deducted finance charge is satisfied at disbursement and must not also be scheduled as unpaid interest.

Detail: [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md).

No default rate or maximum rate is defined.

---

## Money / precision (PLM-D-00-12 Closed)

Decimal money; PHP; posted 2 decimal places; intermediate at least 8 decimal places; midpoint **To Even**; final-installment residual reconciliation.

Detail: [money-precision-and-rounding-policy.md](money-precision-and-rounding-policy.md).

---

## Term vs installment count

“1 month” and “30 daily installments” are **not** automatically the same thing. `N` must be explicit. Calendar/due-date rules: [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md).

---

## Traditional and Quick Loan convergence

Once an approved obligation becomes an operational Loan, both use the **same** Loan subledger, balance engine, payment posting, schedule engine, penalty engine, collector model, reconciliation, settlement, and audit.

Do **not** duplicate financial engines.

---

## Legal / compliance boundary

Financial configuration capability does **not** mean every possible configuration is legally permitted.

Before Production, qualified review is required (PLM-D-00-11). This package does **not** invent Philippine regulatory rules and does **not** define default rates.

---

## Explicit non-goals

- Default interest rates or fee amounts
- Penalty rates/amounts (engine accepted; no defaults)
- Early-settlement unearned-interest formula
- Legal EIR/APR algorithm
- Database tables, classes, or enums
- Claiming legal permissibility of deducted-interest configurations
