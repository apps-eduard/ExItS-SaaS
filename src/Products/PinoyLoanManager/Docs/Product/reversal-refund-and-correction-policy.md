# Pinoy Loan Manager — Reversal, Refund, and Correction Policy

**Status:** Accepted product policy (PLM-DOC-04); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Payment reversal, Refund Payable, office cash refunds, and the separation of Loan financial correction from physical cash. Not a posting engine or legally validated refund practice.

**Canonical companions:** [early-settlement-and-principal-prepayment-policy.md](early-settlement-and-principal-prepayment-policy.md), [disbursement-cancellation-and-reversal-policy.md](disbursement-cancellation-and-reversal-policy.md), [cash-variance-and-session-close-policy.md](cash-variance-and-session-close-policy.md). ADR: [../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md](../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md). Maker/checker: [../Security/role-and-grant-baseline.md](../Security/role-and-grant-baseline.md).

Posted financial history is **never** edited or deleted.

---

## Payment reversal

A posted Payment must never be edited or deleted.

- Payment Reversal reverses the original Payment **in full**
- a partial correction is: (1) full reversal of the incorrect Payment, then (2) posting a new correct Payment
- reversal references the original transaction
- reason is mandatory
- authorization is mandatory
- original allocation remains historically visible
- reversal applies the opposite financial effects deterministically
- resulting balances are re-derived from events

Do **not** use a negative Payment record as a shortcut.

Payment Reversal is a **high-risk** action (PLM-D-00-13 Closed): requester normally cannot self-approve when another eligible approver exists.

---

## Payment Reversal vs Cash Refund

| Action | Meaning |
|---|---|
| **Payment Reversal** | Changes the borrower’s Loan financial history |
| **Cash Refund** | Records physical money returned to the borrower |

They may be correlated but are **separate** actions.

Reversing a cash Payment does **not** prove cash was returned. Returning cash without reversing the financial Payment does **not** correct the Loan.

---

## Refund Payable

A conceptual Refund Payable obligation (not a borrower wallet). May arise from:

- overpayment
- unearned deducted finance-charge rebate
- refundable fee credit
- corrected duplicate payment
- reversed fee/penalty where physical cash must be returned
- another approved adjustment

Refund Payable:

- is not available for unrelated purchases or Loans
- must reference its source transaction/calculation
- has its own status and audit history

Conceptual statuses (not finalized enums): Pending Approval, Approved, Paid, Cancelled, Reversed.

---

## Cash refund workflow (MVP)

MVP cash refunds are **Office / Cashier operations only**. Collector must **not** issue ad hoc field refunds in MVP.

```text
Refund Payable
  → authorized refund approval
  → borrower/recipient verification
  → active Cashier Session
  → sufficient accountable cash
  → exact refund amount confirmed
  → physical cash released
  → Cash Refund movement posted
  → refund receipt/reference produced
  → Refund Payable marked Paid
```

A Cash Refund:

- decreases Cashier accountable cash
- does **not** change Loan balances unless paired with the appropriate financial reversal/credit
- must reference the originating Refund Payable
- must be idempotent
- cannot exceed approved Refund Payable

Cashier never approves their own Payment Reversal or Cash Refund.

---

## High-risk maker/checker

Payment Reversal, Cash Refund approval/payment, Disbursement Reversal after release, Cash Variance Resolution, high-risk Personal identity correction, future Write-Off, future Recovery, and other classified financial corrections are high-risk.

Requester normally cannot approve their own action when another eligible approver exists. Collector never self-approves a high-risk action. Cashier never resolves their own Cashier variance and never approves their own Payment Reversal or Cash Refund.

Controlled Owner Override: [../Security/role-and-grant-baseline.md](../Security/role-and-grant-baseline.md), [../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md](../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md). **PLM-D-00-13 is Closed.**

---

## Invalid operations (refund/reversal)

Future validation must reject:

- reversal without an original transaction
- reversal of an already fully reversed transaction
- refund greater than Refund Payable
- cash refund without an accountable Cashier Session
- field refund by Collector in MVP
- duplicate cash movement/reversal
- direct editing/deletion of posted financial history
- Cashier resolving own variance
- Collector approving own high-risk action

---

## Legal / compliance boundary

No reversal or refund practice is claimed legally sufficient. **PLM-D-00-11 remains Open.** This package does not invent Philippine regulations.

---

## Explicit non-goals

- Borrower wallet
- Collector field refunds in MVP
- Silent edit/delete of posted Payments
- Implementation of a refund engine
