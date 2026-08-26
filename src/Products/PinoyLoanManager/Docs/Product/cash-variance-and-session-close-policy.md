# Pinoy Loan Manager — Cash Variance and Session Close Policy

**Status:** Accepted product policy (PLM-DOC-04); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Collector and Cashier expected-cash calculation, close-with-variance, and variance resolution via new events. Not a cash-session engine or legally validated accounting treatment.

**Canonical companions:** [reversal-refund-and-correction-policy.md](reversal-refund-and-correction-policy.md), [cashier-and-collector-control-model.md](cashier-and-collector-control-model.md), [collector-cash-and-reconciliation.md](collector-cash-and-reconciliation.md). ADR: [../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md](../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md). Accounting boundary: [../Architecture/operational-subledger-and-accounting-boundary.md](../Architecture/operational-subledger-and-accounting-boundary.md).

Do **not** modify Expected Cash merely to force zero variance. Nonzero variance must **never** be marked balanced.

---

## Cash movement corrections

Do not edit or delete posted opening float, additional float, collection cash receipt, disbursement cash release, partial remittance, end-of-day remittance, Cashier-to-Collector transfer, Collector-to-Cashier transfer, or Cash Refund.

Correct through: linked Cash Movement Reversal + new correct Cash Movement + reason + actor + approval + audit.

Loan financial reversal and cash-movement reversal remain **separate**.

---

## Collector reconciliation

At Collector end-of-day:

```text
Expected Collector Cash
  = Opening Float
  + Additional Float
  + Cash Collections
  − Field Disbursements
  − Partial Remittances
  − Other Approved Cash-Out Movements

Variance = Actual Cash Received − Expected Collector Cash
```

Possible result: Zero, Overage, Shortage.

---

## Collector close status

Conceptual statuses (not finalized enums):

- Active
- Submitted For Remittance
- Counted
- Reconciled
- Closed Balanced
- Closed With Variance Pending Resolution

A Collector day may be operationally closed with an unresolved variance after actual cash is counted, remittance is confirmed, variance is recorded, Manager/Owner review is opened, and **no fake balancing movement** is created.

Do not reopen and rewrite the original day. Resolve through later events.

---

## Cashier Session close

If Cashier variance = zero: session may close as **Closed Balanced**.

If Cashier variance ≠ zero:

- Cashier submits the count and variance
- Cashier **cannot** mark it balanced
- Manager/Owner review is required
- session may close as **Closed With Variance** only after authorized approval
- unresolved variance remains visible after close
- original movements and count remain immutable

A Cashier Session must not remain permanently open solely because a real variance exists.

Cashier never resolves their own Cashier variance.

---

## Variance resolution

Variance resolution creates a **new** auditable Resolution event. Do not modify the original Expected or Actual values.

Supported conceptual outcomes:

- Counting Error Corrected
- Missing Cash Movement Identified
- Employee Reimbursement
- Approved Shortage / Operational Loss
- Approved Overage / Unidentified Cash
- Cash Returned To Source
- Investigation Pending
- Other Approved Resolution

A counting-error correction requires evidence and a **new count record**. A missing movement must be posted as a real correlated movement. Employee reimbursement creates a new cash receipt. Approved shortage/overage remains an operational accounting item.

Do **not** apply overage to a borrower Loan without evidence. Do **not** charge shortage to a borrower Loan.

Full accounting treatment remains deferred (PLM-D-00-07 remainder).

---

## Invalid operations

Future validation must reject:

- session marked balanced with nonzero variance
- Cashier resolving own variance
- Collector approving own high-risk action
- duplicate cash movement/reversal
- direct editing/deletion of posted cash history
- applying overage to a borrower Loan without evidence
- charging shortage to a borrower Loan

---

## Legal / compliance boundary

No variance close practice is claimed legally or accounting-complete. **PLM-D-00-11 remains Open.** This package does not invent Philippine regulations.

---

## Explicit non-goals

- Fake balancing movements
- Marking nonzero variance as balanced
- Charging shortage/overage to a borrower Loan
- Full GL journal mapping
- Implementation of cash sessions
