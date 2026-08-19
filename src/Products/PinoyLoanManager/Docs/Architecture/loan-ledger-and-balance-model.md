# Pinoy Loan Manager — Loan Ledger and Balance Model

**Status:** Planning / architecture baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Conceptual operational Loan **subledger** and multi-component balances. Not a database schema, class design, or organization General Ledger.

Related: [../Product/financial-calculation-baseline.md](../Product/financial-calculation-baseline.md), [../Product/payment-allocation-and-prepayment-policy.md](../Product/payment-allocation-and-prepayment-policy.md), [../Product/fees-and-net-proceeds-policy.md](../Product/fees-and-net-proceeds-policy.md), [../Product/payment-and-allocation-model.md](../Product/payment-and-allocation-model.md), [../Product/collector-cash-and-reconciliation.md](../Product/collector-cash-and-reconciliation.md), [../Product/cashier-and-collector-control-model.md](../Product/cashier-and-collector-control-model.md), [../Product/lending-operating-model.md](../Product/lending-operating-model.md).

Platform usage charge must **not** enter this subledger. Schema, GL integration, settlement accounting, and write-off/recovery accounting remain **Open** (PLM-D-00-07 remainder).

---

## Operational Loan subledger

An append-only / auditable Loan operational subledger is the **authoritative operational financial history** for the Loan.

It is **not** the organization’s full General Ledger / accounting system. Full accounting / GL integration remains **OPEN**.

Potential event categories (not finalized names / schema):

- Disbursement
- Payment
- Fee Assessment
- Penalty Assessment
- Waiver
- Reversal
- Adjustment
- Settlement
- Write-Off
- Recovery

Core principles:

- posted events are not silently deleted
- correction is represented by another event
- every event has time
- actor / system source
- organization
- branch where applicable
- related Loan
- correlation / reference
- reason where required
- idempotency support for externally retried operations
- deterministic balance calculation

---

## Balance model

Do **not** rely on one unexplained “Balance” number.

The future Loan model should be capable of explaining components such as:

- Outstanding Principal
- Outstanding Charges
- Outstanding Fees
- Outstanding Penalties
- Past Due
- Current Due
- Total Outstanding
- Settlement Amount

These may differ. Any displayed total must have a **defined derivation**. Terminology: [../Product/financial-calculation-baseline.md](../Product/financial-calculation-baseline.md). Allocation: [../Product/payment-allocation-and-prepayment-policy.md](../Product/payment-allocation-and-prepayment-policy.md).

---

## Loan ledger vs collector cash ledger

Reaffirm WP03:

| Ledger | Question it answers |
|---|---|
| **Loan subledger** | What does the borrower owe / pay? |
| **Collector cash accountability** | How much physical cash is the collector responsible for? |

Example (illustrative):

Collector receives PHP 100.

- Loan: Payment +PHP 100
- Collector cash: Cash Received +PHP 100

Same business event / correlation, **different** ledgers / balances.

Never derive collector physical cash solely from Loan balance. Cashier Session and collector daily accountability: [../Product/cashier-and-collector-control-model.md](../Product/cashier-and-collector-control-model.md). Detail: [../Product/collector-cash-and-reconciliation.md](../Product/collector-cash-and-reconciliation.md).

---

## Traditional and Quick Loan

Both origination paths, once operational, use **this** subledger and balance engine. Do not duplicate financial engines.

---

## Explicit non-goals

- Table / class / enum design
- Journal entries to a corporate GL
- Deriving collector cash from Loan outstanding
- Silent edit of posted events
