# Pinoy Loan Manager — Financial Calculation Baseline

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

This document records money terminology, interest-treatment modes, precision rules, and snapshot requirements. It is **not** a calculation-engine specification, default price list, or legally validated rulebook.

Companions:

- [payment-and-allocation-model.md](payment-and-allocation-model.md)
- [schedule-maturity-and-settlement.md](schedule-maturity-and-settlement.md)
- [loan-lifecycle-model.md](loan-lifecycle-model.md)
- [../Architecture/loan-ledger-and-balance-model.md](../Architecture/loan-ledger-and-balance-model.md)
- [quick-loan-model.md](quick-loan-model.md)
- [lending-operating-model.md](lending-operating-model.md)

---

## Money terminology

Do **not** assume that Principal, Net Proceeds, and Total Repayment are always identical. UI, API, and domain must eventually use **explicit names** instead of one ambiguous “Amount”.

| Term | Meaning (planning) |
|---|---|
| **Requested Amount** | Amount the borrower asked for on the application / Quick Loan request |
| **Approved Amount** | Amount the organization authorized (may differ from requested) |
| **Face / Contract Loan Amount** | Contractual face obligation used as the agreed loan amount (P in the treatment modes below) |
| **Interest / Finance Charge** | Calculated charge I under the snapshotted method (not a default rate) |
| **Fee** | Other contractual/authorized charges distinct from interest and penalty |
| **Penalty** | Explicit assessed charge for qualifying missed/past-due events; never a silent principal increase |
| **Net Proceeds** | Cash/value actually released to the borrower at disbursement, after authorized deductions |
| **Scheduled Repayment** | Contractual total (or installment total) the borrower is scheduled to repay |
| **Amount Due** | Amount currently required under schedule + policy (current due ± past due as defined) |
| **Past-Due Amount** | Unpaid scheduled obligations whose due date has passed (subject to grace/exception) |
| **Outstanding Principal** | Remaining principal component of the obligation |
| **Outstanding Charges** | Remaining interest/finance charges, fees, and similar non-principal charges as defined by policy |
| **Outstanding Balance** | Defined derivation of remaining obligation components — not an unexplained single number |
| **Settlement Amount** | Amount required to close the Loan under a Settlement Quote policy at a point in time |

These may differ from each other. Any displayed total must have a defined derivation. See [../Architecture/loan-ledger-and-balance-model.md](../Architecture/loan-ledger-and-balance-model.md).

---

## Two interest-treatment modes

Preserve the WP03 Quick Loan concepts. Exact formula remains configurable. Examples below are **illustrative engine semantics only**. They are **not** default pricing, legal advice, or compliance approval. Do **not** treat PHP 300 or any implied percentage as a Platform default.

### Mode A — Interest / finance charge deducted from proceeds

Conceptually:

- Face Loan Amount = P
- Calculated Interest / Finance Charge = I
- Net Proceeds = P − I, **before** any other explicitly authorized deductions
- Scheduled contractual repayment remains based on the agreed **face** obligation

Illustrative example only:

- Face Loan Amount: PHP 3,000
- Illustrative charge: PHP 300
- Net Proceeds: PHP 2,700
- Scheduled repayment: PHP 3,000

### Mode B — Interest added to repayment

Conceptually:

- Face Loan Amount = P
- Net Proceeds = P
- Calculated Interest = I
- Total Scheduled Repayment = P + I, plus/minus other explicitly defined allowed components

Illustrative example only:

- Face Loan Amount: PHP 3,000
- Illustrative interest: PHP 300
- Net Proceeds: PHP 3,000
- Scheduled repayment: PHP 3,300

Template-level description: [quick-loan-model.md](quick-loan-model.md).

---

## Exact interest formula remains configurable

Do **not** choose one mandatory interest calculation method yet.

The future engine may support explicitly approved methods such as:

- flat / add-on
- reducing / declining
- simple interest
- other approved calculation models

Every loan must **snapshot**:

- calculation method
- rate
- rate basis
- term
- frequency
- calculation inputs
- resulting charges
- resulting schedule

A template modification must **never** recalculate an existing submitted, approved, or disbursed Loan silently.

Exact MVP methods, rate, and rate precision remain **Open / Product Owner + Legal/Accounting Validation Required** (PLM-D-00-08).

---

## Money / precision engineering baseline

Accepted **engineering** rules for a future implementation (not implemented here):

- Never use binary floating-point for authoritative money calculations.
- Future .NET implementation should use **decimal-based** money arithmetic.
- Initial business currency is **PHP**.
- Display / posted PHP money is normally represented to currency precision.
- Intermediate calculations may require greater precision.
- Rounding must happen only at **documented calculation boundaries**.
- Schedule totals must reconcile **exactly** to the contractual total.
- Any rounding residual should be deterministically reconciled, typically through the **final applicable installment**, rather than disappearing.

**Exact rounding mode** is **not** finalized in this package. Record it as a future explicit decision before calculation-engine implementation (PLM-D-00-12).

---

## Term vs installment count

“1 month” and “30 daily installments” are **not** automatically the same thing.

Do **not** assume 1 month = 30 calendar days.

A Quick Loan Template must eventually express enough information to generate an **unambiguous** schedule. Potential concepts (not implemented, not schema):

- term quantity
- term unit
- payment frequency
- installment count
- first due-date rule
- due-date calendar rule

---

## Traditional and Quick Loan convergence

Traditional Loan Application and Quick Loan Request may have different origination rules.

Once an approved obligation becomes an operational Loan, both use the **same**:

- Loan subledger
- balance engine
- payment posting
- schedule engine
- penalty engine
- collector model
- reconciliation model
- settlement model
- audit model

Do **not** duplicate financial engines. See [lending-operating-model.md](lending-operating-model.md) and [loan-lifecycle-model.md](loan-lifecycle-model.md).

---

## Legal / compliance boundary

Financial configuration capability does **not** mean every possible configuration is legally permitted.

Before Production, qualified review is required for applicable interest, finance charges, fees, penalty, disclosures, payment allocation, early settlement, collections, lending practices, and record retention (PLM-D-00-11).

This package does **not** invent Philippine regulatory rules. Illustrative PHP figures above are **not** default rates.

---

## Explicit non-goals

- Choosing an MVP interest formula, rate, or rounding mode
- Database tables, classes, or enums
- Claiming legal permissibility of Mode A or Mode B
