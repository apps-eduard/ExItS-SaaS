# PLM-DOC-04 — Early Settlement, Reversals, Variance & Accounting Boundaries

**Status:** Documentation package complete (planning only)
**Implementation present:** No
**Last updated:** 2026-08-19
**Branch:** `docs/plm-final-decisions`

Runtime / browser / device / database / production validation: **Not Applicable**.

---

## Scope

Finalize Pinoy Loan Manager product rules for full early settlement, partial principal prepayment, unearned finance-charge rebate, Settlement Quotes, payment reversals, disbursement cancellation/reversal, fee and penalty refunds, physical cash refunds, cashier/collector cash variance, session close-with-variance, high-risk maker/checker, operational Loan subledger boundaries, and accounting/GL boundaries.

Explicitly **out of scope:** code, database, migrations, APIs, UI, solution changes, parked scaffold, POS/Platform implementation, legal compliance claims, full GL integration, write-off/recovery accounting, Platform commercial transport (D-P12-03).

---

## Settlement formula

```text
Settlement Amount
  = Outstanding Principal
  + Earned/Accrued Unpaid Finance Charge
  + Applicable Outstanding Fees
  + Valid Outstanding Penalties
  + Other Approved Debit Adjustments
  − Unearned Finance-Charge Rebate
  − Refundable/Unearned Fee Credits
  − Approved Waivers
  − Other Approved Credit Adjustments
```

Floor at PHP 0.00. Excess credit → Refund Payable.

Canonical: [../Product/early-settlement-and-principal-prepayment-policy.md](../Product/early-settlement-and-principal-prepayment-policy.md).

---

## Method-specific settlement

| Method | Behavior |
|---|---|
| Flat/Add-On added-interest | Unearned future finance charge rebated; not collectible after full settlement |
| Flat/Add-On deducted-interest | Unearned portion is an auditable rebate credit; not collected twice |
| Reducing-balance | Future interest after quote date not charged; current period uses Actual-Days proration |

MVP quote validity = until end of stated Branch-local business date. No MVP settlement/prepayment penalty.

---

## Principal-prepayment rules

Explicit action only; not inferred from excess. Satisfy Past Due (and Current Due where required) first. Default schedule treatment = **Reduce Term**. New schedule version; prior schedule preserved.

---

## Refund workflow

Refund Payable is not a wallet. MVP cash refunds are Office/Cashier only. Collector field refunds are not allowed. Cash Refund and Loan reversal remain separate.

Canonical: [../Product/reversal-refund-and-correction-policy.md](../Product/reversal-refund-and-correction-policy.md).

---

## Reversal / correction

Posted history is never edited/deleted. Payment correction = full reversal + new correct Payment.

---

## Disbursement cancellation / reversal

Cancel before release. After release: reverse only for error/duplicate/incomplete or fully recovered funds with high-risk approval. Do not fake reversal while borrower retains funds.

Canonical: [../Product/disbursement-cancellation-and-reversal-policy.md](../Product/disbursement-cancellation-and-reversal-policy.md).

---

## Variance-close policy

Sessions may close with **visible** unresolved variance after authorized review. Nonzero variance cannot be marked balanced. Resolve via new events.

Canonical: [../Product/cash-variance-and-session-close-policy.md](../Product/cash-variance-and-session-close-policy.md).

---

## Maker/checker and Owner Override

**PLM-D-00-13 Closed.** Distinct approver when another eligible user exists. Controlled Owner Override only for sole eligible Owner with explicit grant, reason, evidence, enhanced audit, and subsequent-review reporting.

---

## Operational subledger and GL boundary

Loan subledger and Cash Accountability ledger are separate and correlated. PLM is **not** a complete General Ledger. Accounting projection must not rewrite PLM history.

Canonical: [../Architecture/operational-subledger-and-accounting-boundary.md](../Architecture/operational-subledger-and-accounting-boundary.md).

---

## Remaining open decisions

| ID | Remaining |
|---|---|
| PLM-D-00-07 | Persistence/schema; journal/export; write-off/recovery accounting; external GL |
| PLM-D-00-08 | Restructuring; write-off/recovery financial treatment |
| PLM-D-00-06 | Exact grant identifiers (including Owner Override grant) |
| PLM-D-00-11 | Legal/compliance, including statutory rebate |
| PLM-DOC-05 | Roles, grants, workflow authorization & operational security |

PLM-D-00-12 remains **Closed**. PLM-D-00-10 remains **Closed / Product Owner Accepted**. PLM-D-00-13 is **Closed**.

---

## No-code / no-database statement

This package is **documentation only**. No `.cs`, `.csproj`, `ExItS.slnx`, migrations, database, API, UI, tests, POS, Platform implementation, or parked-scaffold changes.

Implementation remains **paused**. `feat/plm-01-scaffold` remains unmerged.

---

## Validation

Documentation only. `git diff --check` recorded at commit time. No implementation authorization. No legal compliance claim. No prepayment/settlement penalty. No borrower wallet.

---

## Git evidence

Recorded in the PLM-DOC-04 commit on `docs/plm-final-decisions`. Parked scaffold `feat/plm-01-scaffold` @ `4ec9e96e9149cd8d014adde3d694872a6d5ef576` not modified.

---

## Exact next documentation package

**PLM-DOC-05 — Roles, Grants, Workflow Authorization & Operational Security Finalization**

Do not start PLM-DOC-05 in this package. Implementation remains paused.
