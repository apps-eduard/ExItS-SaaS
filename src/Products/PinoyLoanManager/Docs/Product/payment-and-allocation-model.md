# Pinoy Loan Manager — Payment and Allocation Model

**Status:** Planning / product-rule baseline; MVP allocation accepted in PLM-DOC-02
**Implementation present:** No
**Last updated:** 2026-08-19

Payment posting, missed-installment carry-forward, reversal/idempotency, and pointers to accepted allocation policy. Not a posting-engine specification.

**Canonical allocation policy:** [payment-allocation-and-prepayment-policy.md](payment-allocation-and-prepayment-policy.md). ADR: [../Decisions/ADR-004-rounding-fees-and-payment-allocation.md](../Decisions/ADR-004-rounding-fees-and-payment-allocation.md).

Related: [financial-calculation-baseline.md](financial-calculation-baseline.md), [schedule-maturity-and-settlement.md](schedule-maturity-and-settlement.md), [../Architecture/loan-ledger-and-balance-model.md](../Architecture/loan-ledger-and-balance-model.md), [penalty-exception-and-waiver-model.md](penalty-exception-and-waiver-model.md), [disbursement-and-payment-controls.md](disbursement-and-payment-controls.md), [exception-reversal-and-variance-workflow.md](exception-reversal-and-variance-workflow.md).

Penalty rates/caps remain **PLM-DOC-03**.

---

## Accepted MVP allocation (PLM-DOC-02)

- oldest due obligation first
- within that obligation: Due Interest / Finance Charge → Due Principal → Due Scheduled Fees → Due Penalties
- not organization-editable in MVP
- snapshotted/versioned per Loan
- deducted finance charge is not outstanding scheduled interest
- partial payments **supported**
- multiple payments **supported**
- advance payment satisfies future scheduled obligations after past/current due; does not silently regenerate the schedule
- excess is **not** inferred as principal prepayment
- no general borrower wallet in MVP
- payment reversal does not delete the original payment

Detail: [payment-allocation-and-prepayment-policy.md](payment-allocation-and-prepayment-policy.md).

---

## Missed installment does not disappear

A missed installment does not disappear when the next due date arrives.

Illustrative example only (not a rate):

- Day 1: PHP 100 due, PHP 0 paid
- Day 2: another PHP 100 becomes due

Conceptually the customer may now have Past Due plus Current Due. Subject to penalty / grace / exception rules (**PLM-DOC-03**).

The schedule retains **both** installment histories.

---

## Penalty posting (explicit event)

Penalty must be represented as an explicit financial event / charge. Do **not** silently increase principal. Rates/caps: **PLM-DOC-03**. Classification: [penalty-exception-and-waiver-model.md](penalty-exception-and-waiver-model.md).

Engineering safety defaults (not legal caps): penalty-on-penalty default **OFF**; unlimited penalty growth **prohibited** as an engineering default; penalty-cap **support** required.

---

## Reversals

Do **not** edit the original payment to zero. Original Payment + Authorized Reversal Event + new correct payment where needed. Cash refund is a separate correlated action.

Posted events are not silently deleted. See [../Architecture/loan-ledger-and-balance-model.md](../Architecture/loan-ledger-and-balance-model.md).

---

## Concurrency / duplicate safety

Financial commands must be protected against accidental duplicate submission. Future design should use **idempotency / correlation** controls. Implementation is **not** designed in this package.

---

## Explicit non-goals

- Organization-editable allocation order in MVP
- Customer wallet / unapplied credit as MVP
- Penalty rates/amounts (PLM-DOC-03)
- Implementation of posting, idempotency keys, or APIs
