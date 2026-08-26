# Pinoy Loan Manager — Loan Lifecycle Model

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Origination status, Loan lifecycle, and delinquency / collection condition are **separate dimensions**. Not a status-enum specification.

Related: [lending-operating-model.md](lending-operating-model.md), [financial-calculation-baseline.md](financial-calculation-baseline.md), [schedule-maturity-and-settlement.md](schedule-maturity-and-settlement.md), [early-settlement-and-principal-prepayment-policy.md](early-settlement-and-principal-prepayment-policy.md), [disbursement-cancellation-and-reversal-policy.md](disbursement-cancellation-and-reversal-policy.md), [delinquency-and-missed-payment-policy.md](delinquency-and-missed-payment-policy.md), [maturity-and-post-maturity-policy.md](maturity-and-post-maturity-policy.md), [disbursement-and-payment-controls.md](disbursement-and-payment-controls.md), [../Architecture/application-surface-model.md](../Architecture/application-surface-model.md).

---

## Avoid one giant status

Do **not** mix origination, operational lifecycle, and collection condition into a single enum.

A Loan can simultaneously be:

- Lifecycle = **Active**
- Collection condition = **Past Due**

Do **not** force Past Due into the same lifecycle dimension. This is the preferred architecture baseline.

---

## A. Origination / request status

Traditional application or Quick Loan request may have concepts such as:

- Draft
- Submitted
- Under Review
- Approved
- Rejected
- Cancelled
- Expired

These describe the **request / application**, not the operational Loan after disbursement.

Exact Traditional Loan workflow remains **OPEN** for assessment criteria and approval limits. Direction: [traditional-loan-model.md](traditional-loan-model.md), [loan-application-and-approval.md](loan-application-and-approval.md). Quick Loan request flow: [quick-loan-model.md](quick-loan-model.md).

---

## B. Loan lifecycle

Concepts such as:

- Awaiting Disbursement
- Active
- Settled
- Cancelled Before Disbursement
- Written Off

**Approved does not mean Disbursed.**

```text
Approved
  → Awaiting Disbursement
  → Disbursed
  → Active
```

Not a finalized enum. Cancelled-before-disbursement: [disbursement-cancellation-and-reversal-policy.md](disbursement-cancellation-and-reversal-policy.md). Settled: [early-settlement-and-principal-prepayment-policy.md](early-settlement-and-principal-prepayment-policy.md). Written-off rules remain **OPEN**.

---

## C. Delinquency / collection condition

Separate **derived / operational** classification such as:

- Current
- Past Due
- Matured Past Due
- Settled

Derived from schedule, payments, maturity, and policy — not a substitute for lifecycle. Canonical: [delinquency-and-missed-payment-policy.md](delinquency-and-missed-payment-policy.md), [maturity-and-post-maturity-policy.md](maturity-and-post-maturity-policy.md).

---

## Disbursement

Approval and disbursement remain separate.

Disbursement must record:

- authorized amount
- net proceeds actually released
- method
- branch
- cashier / collector actor
- customer / borrower
- date / time
- reference
- related Loan
- audit context

Cash release should create the authoritative operational financial event.

It also remains the **preferred trigger** for future ExItS Platform usage billing (not Loan Approved). Transport remains **D-P12-03**. See [lending-operating-model.md](lending-operating-model.md).

---

## Convergence after origination

Traditional Loan Application and Quick Loan Request may differ until an approved obligation becomes an operational Loan. Then both use the same financial core. See [financial-calculation-baseline.md](financial-calculation-baseline.md).

---

## Explicit non-goals

- Final status codes / schema
- Auto-approval
- Silent close at maturity
- Mixing Past Due into lifecycle
