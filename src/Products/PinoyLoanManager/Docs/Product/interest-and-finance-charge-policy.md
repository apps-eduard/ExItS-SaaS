# Pinoy Loan Manager — Interest and Finance Charge Policy

**Status:** Accepted product policy (PLM-DOC-02); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

MVP contractual calculation methods, rate bases, interest-treatment compatibility, and snapshot rules. Not a calculation engine, default price list, or legally validated disclosure.

**Canonical companions:** [fees-and-net-proceeds-policy.md](fees-and-net-proceeds-policy.md), [money-precision-and-rounding-policy.md](money-precision-and-rounding-policy.md), [payment-allocation-and-prepayment-policy.md](payment-allocation-and-prepayment-policy.md). ADR: [../Decisions/ADR-003-supported-interest-and-schedule-methods.md](../Decisions/ADR-003-supported-interest-and-schedule-methods.md). Terminology index: [financial-calculation-baseline.md](financial-calculation-baseline.md).

Penalties, due-date calendars, excused days, and post-maturity rules belong to **PLM-DOC-03**. Early-settlement unearned-interest treatment remains open.

---

## Terminology used here

| Term | Meaning |
|---|---|
| Requested Amount | Amount requested by the borrower |
| Approved Principal | Principal approved by the organization |
| Contract / Face Principal (`P`) | Contractual principal on which the selected calculation policy operates |
| Finance Charge / Interest (`I`) | Separately calculated charge for use of the principal |
| Net Proceeds | Cash/value actually released to the borrower |
| Total Scheduled Repayment | Total contractual amount expected under the original schedule |
| Outstanding Principal | Contract principal not yet satisfied |
| Settlement Amount | Amount required by a valid settlement quote to fully settle the Loan |

Do not use a single ambiguous field named only “Amount” or “Balance” when the financial meaning is material.

No default rate or maximum rate is defined in this package.

---

## MVP calculation methods

| Method | Quick Loan MVP | Traditional Loan MVP |
|---|---|---|
| Flat / Add-On Finance Charge | **Supported (only method)** | **Supported** |
| Reducing-Balance Equal-Installment Amortization | **Not supported** | **Supported** |

Deferred unless separately approved later:

- reducing-balance equal-principal schedule
- interest-only
- balloon payment
- custom formula scripts
- arbitrary organization-provided formulas
- revolving credit
- compound-interest schedules
- variable-rate loans

Do **not** authorize organization-supplied executable or custom formulas.

The future calculation engine may be strategy-based internally. Only **approved product calculation methods** may run.

---

## Compatibility

| Method | Added To Repayment | Deducted From Proceeds |
|---|---|---|
| Flat / Add-On | Allowed | Allowed |
| Reducing-Balance Equal-Installment | **Required** (only supported treatment) | **Prohibited** |

Invalid combinations must be rejected before submission and again before disbursement. See Invalid configuration in [fees-and-net-proceeds-policy.md](fees-and-net-proceeds-policy.md).

---

## Snapshot (mandatory)

Every submitted request/application and resulting Loan must snapshot:

- calculation method and version
- interest treatment
- rate and rate basis
- term, installment count, payment frequency
- fee definitions and amounts
- rounding policy and version
- payment-allocation policy and version
- schedule inputs and calculated outputs

Changing a Loan Product or Quick Loan Template later must **never** recalculate an already submitted, approved, or disbursed Loan silently.

Quick Loan Template snapshot must include: approved principal, rate, rate basis, term, installment count, payment frequency, interest-treatment mode, fee definitions, calculation method version, rounding policy version, and payment-allocation policy version.

---

## Flat / Add-On Finance Charge

`P` = Contract / Face Principal  
`r` = approved decimal interest/finance-charge rate  
`N` = number of installment periods when the rate basis is per installment period

The template/terms must state **explicitly** whether the rate is **Per Term** or **Per Installment Period**.

Do **not** infer rate basis from payment frequency.  
Do **not** label a per-term rate as monthly or annual.

### A. Per Term

Raw Total Finance Charge:

`I_raw = P × r`

### B. Per Installment Period

Raw Total Finance Charge:

`I_raw = P × r × N`

The posted Total Finance Charge is calculated from `I_raw` using [money-precision-and-rounding-policy.md](money-precision-and-rounding-policy.md).

### Added To Repayment

Conceptually:

```text
Net Proceeds = P − Upfront Deducted Fees

Total Scheduled Repayment
  = P + Total Finance Charge + Financed Fees + Scheduled Fees
```

Default component distribution:

- principal divided across `N` installments
- finance charge divided across `N` installments
- residual centavos reconciled in the final applicable installment ([money-precision-and-rounding-policy.md](money-precision-and-rounding-policy.md))

Scheduled fees follow their snapshotted schedule/timing.

### Deducted From Proceeds

Conceptually:

```text
Net Proceeds = P − Total Finance Charge − Upfront Deducted Fees

Total Scheduled Repayment = P + Financed Fees + Scheduled Fees
```

The deducted finance charge is assessed and **satisfied at disbursement** through the disclosed proceeds deduction.

It must **not** also appear as unpaid scheduled interest.

Do **not** charge the same finance charge twice.

The schedule normally contains repayment of principal, financed fees, and scheduled fees according to the snapshotted terms.

A Loan must not be represented merely as “Borrow PHP 3,000” when the customer actually receives less than PHP 3,000. Required disclosure fields: [fees-and-net-proceeds-policy.md](fees-and-net-proceeds-policy.md).

Do **not** claim that deducted-interest configurations are legally permissible without qualified review (PLM-D-00-11).

---

## Reducing-Balance Equal-Installment (Traditional only)

`P` = Contract Principal  
`i` = approved periodic decimal rate matching the installment period  
`N` = number of installments (`N` > 0)

Supports **Interest Treatment = Added To Repayment only**. Deducted-from-proceeds is prohibited.

For `i > 0`:

```text
A_raw = P × i / (1 − (1 + i)^(−N))
```

`A_raw` is the raw equal periodic installment **before** final currency reconciliation.

For `i = 0`:

```text
A_raw = P / N
```

For each installment `k`:

```text
Interest_k_raw = Opening Outstanding Principal_k × i
Principal_k_raw = A_raw − Interest_k_raw
Closing Outstanding Principal_k = Opening Outstanding Principal_k − Principal_k
```

The final installment must reconcile remaining principal and the contractual total **exactly** after rounding.

### Periodic rate safety

The configured periodic rate must correspond **explicitly** to the payment/installment period. Example labels:

- rate per daily installment period
- rate per weekly installment period
- rate per monthly installment period

Do **not** silently convert annual to monthly, monthly to daily, monthly to weekly, or nominal to effective unless a later package documents an approved conversion policy.

Annualized/effective-cost disclosure, if required later, is calculated **separately** from contractual cash flows. It must not silently replace the snapshotted contractual formula. Legal EIR/APR algorithm is **not** defined here (PLM-D-00-11).

---

## Installment count

Every calculated schedule requires an explicit positive installment count `N`.

Do **not** assume:

- one month = 30 days
- one year = 365 collection days
- four weeks = one month
- daily payment means every calendar day

The relationship among term, frequency, installment count, first due date, and calendar rules must be explicit. Exact due-date/calendar behavior remains **PLM-DOC-03**.

---

## Customer calculation display

Before a customer submits a Quick Loan Request or accepts Traditional Loan terms, display at minimum:

- requested amount
- approved/contract principal when available
- calculation method
- rate
- rate basis
- total finance charge
- each fee and fee treatment
- total deductions
- Net Proceeds
- installment amount(s)
- installment count
- payment frequency
- first due date
- maturity date
- Total Scheduled Repayment
- payment-allocation policy summary

For reducing-balance schedules, display the complete schedule or a clear accessible schedule breakdown before acceptance/disbursement.

Do **not** claim this display satisfies legal disclosure requirements until qualified review (PLM-D-00-11).

---

## Legal / compliance boundary

No formula, rate basis, or interest treatment in this document is claimed legally compliant. External qualified legal/compliance review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations and does not define a default rate.

---

## Explicit non-goals

- Default or maximum interest rates
- Organization-supplied custom formulas
- Reducing-balance for Quick Loan MVP
- Reducing-balance with deducted-interest treatment
- Due-date calendar, penalties, excused days (PLM-DOC-03)
- Early-settlement unearned-interest formula
- Legal EIR/APR algorithm
- Implementation of a calculation engine
