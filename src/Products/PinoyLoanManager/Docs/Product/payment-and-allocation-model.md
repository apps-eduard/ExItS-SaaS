# Pinoy Loan Manager — Payment and Allocation Model

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Payment posting, allocation, missed-installment carry-forward, and reversal/idempotency requirements. Not a posting-engine specification.

Related: [financial-calculation-baseline.md](financial-calculation-baseline.md), [schedule-maturity-and-settlement.md](schedule-maturity-and-settlement.md), [../Architecture/loan-ledger-and-balance-model.md](../Architecture/loan-ledger-and-balance-model.md), [penalty-exception-and-waiver-model.md](penalty-exception-and-waiver-model.md), [disbursement-and-payment-controls.md](disbursement-and-payment-controls.md), [exception-reversal-and-variance-workflow.md](exception-reversal-and-variance-workflow.md).

---

## Engineering defaults (accepted planning baseline)

### A. Partial payments — supported

Example (illustrative amounts only):

- Amount currently due: PHP 100
- Customer pays: PHP 50

The payment is posted. The remaining PHP 50 remains unpaid / past-due according to schedule rules.

Do **not** force the collector to reject a legitimate partial cash payment.

### B. Multiple payments — supported

A borrower may make multiple payments against the same Loan / day.

### C. Payment greater than today’s installment

May be accepted up to the amount that can validly be applied to the Loan under its snapshotted policy (advance / prepayment / settlement — see [schedule-maturity-and-settlement.md](schedule-maturity-and-settlement.md)).

### D. True overpayment — MVP

Do **not** create a general borrower wallet / customer-credit system in MVP.

Default engineering direction: **prevent or explicitly resolve** payment amounts above the valid collectible / settlement amount rather than silently creating unexplained credit.

Future unapplied-credit support is a separate product decision.

---

## Payment allocation

Every payment must use a **deterministic** allocation policy.

Allocation must be:

- explicit
- snapshotted / versioned where contractually material
- reproducible
- auditable

### Schedule-level recommended baseline

Payments apply to the **oldest unpaid due obligations first**, unless an explicitly approved policy says otherwise.

Illustrative example only:

- Day 1 due: PHP 100 unpaid
- Day 2 due: PHP 100 unpaid
- Customer pays PHP 150

The system should be capable of allocating:

- Day 1 → PHP 100
- Day 2 → PHP 50

rather than applying money arbitrarily.

### Component order remains open

The exact component order among:

- penalty
- fee
- interest
- principal

remains **OPEN / Product Owner + Legal/Accounting Validation Required**.

Do **not** choose a universal order in this package.

---

## Missed installment does not disappear

A missed installment does not disappear when the next due date arrives.

Illustrative example only:

- Day 1: PHP 100 due, PHP 0 paid
- Day 2: another PHP 100 becomes due

Conceptually the customer may now have:

- Past Due: PHP 100
- Current Due: PHP 100
- Total currently due: PHP 200

subject to the Loan’s penalty / grace / exception rules.

The schedule retains **both** installment histories.

---

## Penalty posting (explicit event)

Penalty must be represented as an explicit financial event / charge. Do **not** silently increase principal.

```text
Past-due installment exists
        ↓
Penalty policy determines eligibility
        ↓
Penalty Assessment posted
        ↓
Outstanding penalty balance changes
```

If waived:

- Penalty Assessment +PHP X
- Penalty Waiver −PHP X

If assessed incorrectly:

- Penalty Assessment +PHP X
- Penalty Reversal −PHP X

History remains visible. Classification, exception vs waiver vs reversal, and safety defaults: [penalty-exception-and-waiver-model.md](penalty-exception-and-waiver-model.md).

PHP X is a placeholder, not a rate.

---

## Penalty engineering safety defaults

Accepted **engineering** defaults (not legal caps):

- Penalty-on-penalty default: **OFF**
- Unlimited penalty growth: **prohibited** as an engineering default
- Penalty-cap **support**: required
- Collector cannot approve own waiver
- Manager/Owner authorization required according to future grants
- Every penalty / waiver / reversal requires audit metadata

Do **not** define an actual legal cap or rate. Do **not** claim compliance.

---

## Reversals

If a PHP 100 payment was posted incorrectly, do **not** edit the original payment to PHP 0.

Conceptually:

- Original Payment: +PHP 100
- Authorized Payment Reversal: −PHP 100 financial effect

with reason, actor, authorization, timestamp, and reference to the original transaction.

If corrected, a **new** correct payment is posted separately.

Posted events are not silently deleted. See [../Architecture/loan-ledger-and-balance-model.md](../Architecture/loan-ledger-and-balance-model.md).

---

## Concurrency / duplicate safety

Financial commands must be protected against accidental duplicate submission.

Examples:

- collector taps Record Payment twice
- mobile retries after network timeout
- API client retries disbursement

Future design should use **idempotency / correlation** controls. Implementation is **not** designed in this package.

---

## Explicit non-goals

- Universal penalty/fee/interest/principal allocation order
- Customer wallet / unapplied credit as MVP
- Implementation of posting, idempotency keys, or APIs
