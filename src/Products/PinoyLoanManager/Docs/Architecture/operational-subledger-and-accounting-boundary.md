# Pinoy Loan Manager — Operational Subledger and Accounting Boundary

**Status:** Accepted architecture policy (PLM-DOC-04); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Operational Loan subledger, Cash Accountability ledger, correlation, and the boundary versus a complete General Ledger. Not a database schema, Chart of Accounts, or GL integration.

**Canonical companions:** [loan-ledger-and-balance-model.md](loan-ledger-and-balance-model.md), [../Product/early-settlement-and-principal-prepayment-policy.md](../Product/early-settlement-and-principal-prepayment-policy.md), [../Product/reversal-refund-and-correction-policy.md](../Product/reversal-refund-and-correction-policy.md), [../Product/cash-variance-and-session-close-policy.md](../Product/cash-variance-and-session-close-policy.md). ADR: [../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md](../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md).

Write-off and recovery accounting remain **Open**.

---

## Operational Loan subledger

PLM maintains an authoritative operational Loan subledger. It answers:

- what the borrower was obligated to pay
- what was assessed
- what was paid
- how payment was allocated
- what was waived/reversed
- what remains outstanding
- how settlement was calculated

It is append-only / auditable in financial effect.

Potential event categories (not finalized names):

- Disbursement
- Principal Prepayment
- Finance-Charge Assessment
- Finance-Charge Rebate
- Fee Assessment
- Penalty Assessment
- Payment
- Payment Allocation
- Waiver
- Reversal
- Adjustment
- Refund Payable
- Settlement
- future Write-Off
- future Recovery

Do not finalize database schema or event enum names.

---

## Cash Accountability ledger

A **separate** operational Cash Accountability ledger answers:

- how much physical cash a Cashier/Collector is responsible for
- where cash came from
- where cash went
- what was remitted
- what was counted
- what variance exists

Potential cash movement categories (not finalized names):

- Opening Cash
- Opening Float
- Additional Float
- Customer Cash Collection
- Office Payment Cash
- Loan Disbursement Cash
- Partial Remittance
- End-of-Day Remittance
- Cash Refund
- Employee Reimbursement
- Cash Movement Reversal
- approved cash adjustment

Do **not** derive physical cash solely from Loan balances.

---

## Transaction correlation

A business transaction may create effects in multiple operational ledgers. Example: a cash Payment posts Payment and allocation in the Loan subledger and Cash received in Cash Accountability.

Both must share correlation/reference, organization, branch, actor, amount, time, channel, and idempotency identity where applicable. They remain **separate financial facts**.

---

## Full General Ledger boundary

Pinoy Loan Manager MVP is **not** the organization’s complete accounting General Ledger.

PLM does not need to own Chart of Accounts, full double-entry books, tax accounting, statutory financial statements, payroll accounting, complete organization expenses, or bank reconciliation outside PLM scope.

Future accounting integration may consume approved PLM operational events or summaries through exports, journal projections, or approved APIs/events. Do **not** create the GL integration now.

---

## Accounting projection principle

Future accounting projection must **not** become the authoritative source for borrower Loan balance, Collector cash accountability, Cashier session cash, or Loan settlement.

PLM operational ledgers remain authoritative for PLM operations. An accounting integration may reconcile against PLM but must **not** directly rewrite PLM operational history.

---

## Write-off and recovery (not finalized)

Preserve:

- Write-Off never deletes the Loan
- original Disbursement and payments remain
- Write-Off requires high-risk authorization
- later Recovery must be separately representable
- borrower visibility and legal effect require future decision
- GL/accounting treatment requires qualified accounting/legal review

Keep these open under **PLM-D-00-07** and **PLM-D-00-08**.

---

## Legal / compliance boundary

No subledger or projection rule is claimed accounting-complete or legally sufficient. **PLM-D-00-11 remains Open.** This package does not invent Philippine regulations.

---

## Explicit non-goals

- Chart of Accounts / double-entry GL
- Journal/export contract
- Write-off/recovery accounting
- Schema / enum design
- Implementation of either ledger
