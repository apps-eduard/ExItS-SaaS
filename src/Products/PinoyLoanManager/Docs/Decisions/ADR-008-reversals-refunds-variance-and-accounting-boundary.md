# ADR-008 — Reversals, refunds, variance, and accounting boundary

**Status:** Accepted product policy (PLM-DOC-04); **PLM-D-00-13 Closed**; not implemented
**Date:** 2026-08-19
**Decisions:** **PLM-D-00-13 Closed**; **PLM-D-00-07** and **PLM-D-00-08** Open / Partially Resolved; **PLM-D-00-11** Open

> **Final status note:** **PLM-D-00-07/08 Closed for MVP Product policy** at final review. Persistence/GL remain implementation work. See [PLM-decision-status-summary.md](PLM-decision-status-summary.md).

---

## Context

PLM needed Product Owner rules for correcting posted financial history, physical cash refunds, disbursement cancellation vs reversal, session close-with-variance, high-risk maker/checker, and the boundary between operational ledgers and a complete General Ledger.

---

## Decision

1. Posted financial history is never edited or deleted. Payment correction = full Payment Reversal + new correct Payment. Do not use a negative Payment shortcut.
2. Loan Payment Reversal and physical Cash Refund are separate correlated actions. Refund Payable is not a borrower wallet. MVP cash refunds are Office/Cashier only; Collector field refunds are not allowed.
3. Approved/Awaiting Disbursement Loans may be cancelled before cash release. After release, Disbursement Reversal is allowed only for error/duplicate/incomplete disbursement or fully recovered funds with high-risk approval. Do not fake reversal while the borrower retains funds.
4. Collector and Cashier sessions may close with **visible** unresolved variance after authorized review. Nonzero variance cannot be marked balanced. Resolve via new events; do not rewrite the original day.
5. Maker/checker: requester cannot self-approve high-risk actions when another eligible approver exists. Collector never self-approves high-risk actions. Cashier never resolves own variance or approves own Payment Reversal/Cash Refund.
6. Controlled **Owner Override** is allowed only when no other eligible approver exists, the actor has Owner preset plus explicit `plm.owner-override.execute` grant, reason/evidence are mandatory, the action is classified Owner Override, enhanced audit is written, and subsequent-review reporting is required. Not available to Collector, Cashier-only, or Manager without the override grant. This **closes PLM-D-00-13**. Grant identifiers finalized in PLM-DOC-05 (**PLM-D-00-06 Closed for MVP**).
7. PLM operational Loan subledger and Cash Accountability ledger are separate and correlated. PLM is **not** a complete General Ledger. Accounting projection must not rewrite PLM operational history. Write-off/recovery accounting remains open.

Canonical text: [../Product/reversal-refund-and-correction-policy.md](../Product/reversal-refund-and-correction-policy.md), [../Product/cash-variance-and-session-close-policy.md](../Product/cash-variance-and-session-close-policy.md), [../Product/disbursement-cancellation-and-reversal-policy.md](../Product/disbursement-cancellation-and-reversal-policy.md), [../Architecture/operational-subledger-and-accounting-boundary.md](../Architecture/operational-subledger-and-accounting-boundary.md).

---

## Consequences

Corrections, cash, and SoD have an approved operational contract.

**Still open:** persistence/schema, journal/export contract, write-off/recovery accounting, external GL details, legal/compliance (PLM-D-00-11). Grant identifiers closed for MVP in PLM-DOC-05 (PLM-D-00-06).

No implementation is authorized by this ADR.
