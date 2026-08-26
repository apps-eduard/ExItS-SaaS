# Pinoy Loan Manager — Disbursement Cancellation and Reversal Policy

**Status:** Accepted product policy (PLM-DOC-04); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Cancellation before cash release versus Disbursement Reversal after release. Not a Platform usage-transport design (D-P12-03 remains Open).

**Canonical companions:** [reversal-refund-and-correction-policy.md](reversal-refund-and-correction-policy.md), [cash-variance-and-session-close-policy.md](cash-variance-and-session-close-policy.md), [disbursement-and-payment-controls.md](disbursement-and-payment-controls.md). ADR: [../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md](../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md).

Do **not** delete an approved request/application. Do **not** silently delete a Disbursement after cash release.

---

## Cancellation before release

Before physical funds are released, an Approved / Awaiting Disbursement Loan may be cancelled through an authorized cancellation workflow.

- no Disbursement event exists
- no Platform usage event is created
- no Collector/Cashier cash movement exists
- cancellation reason is recorded
- approval/application history remains

---

## After cash release

Do not simply cancel or delete the Loan.

A **Disbursement Reversal** is permitted only when:

- the Disbursement was erroneous, duplicate, or not actually completed, **or**
- all released funds were physically recovered through a separately recorded cash-return movement
- no conflicting downstream financial activity prevents safe reversal
- an authorized high-risk approval is completed
- reason/evidence is recorded

If the borrower legitimately received and retained the funds:

- the Loan remains Disbursed / Active
- return of money is handled through repayment or early settlement
- do **not** fake a Disbursement Reversal

---

## Disbursement Reversal effects

An authorized Disbursement Reversal must:

- preserve the original Disbursement
- reference the original event
- reverse the Loan financial effect
- preserve the original cash movement
- require a separate Cash Return movement where cash was physically recovered
- reverse/cancel any pending Platform usage signal through the future approved commercial contract
- remain auditable and idempotent

**D-P12-03 remains Open.** Do not design Platform transport here.

---

## Invalid operations

Future validation must reject disbursement reversal while the borrower retains released funds, and any silent deletion of the original Disbursement.

---

## Legal / compliance boundary

No cancellation or reversal practice is claimed legally sufficient. **PLM-D-00-11 remains Open.** This package does not invent Philippine regulations.

---

## Explicit non-goals

- Silent delete after release
- Fake reversal while borrower retains funds
- Platform usage-transport design
- Implementation of disbursement reversal
