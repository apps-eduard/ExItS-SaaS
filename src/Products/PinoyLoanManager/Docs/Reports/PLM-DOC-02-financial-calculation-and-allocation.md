# PLM-DOC-02 — Financial Calculation, Fees & Payment Allocation

**Status:** Documentation package complete (planning only)
**Implementation present:** No
**Last updated:** 2026-08-19
**Branch:** `docs/plm-final-decisions`

Runtime / browser / device / database / production validation: **Not Applicable**.

> **Historical note:** PLM-D-00-07/08 statuses below reflect PLM-DOC-02 package completion. **Both are now Closed for MVP Product policy** (persistence/GL remain implementation work). Final status: [../Decisions/PLM-decision-status-summary.md](../Decisions/PLM-decision-status-summary.md).

---

## Scope

Finalize Pinoy Loan Manager MVP **financial-calculation** policies: supported interest methods, deducted vs added interest, money precision and rounding, fee modeling, Net Proceeds, installment component calculation, payment allocation, partial/advance/excess payments, and financial disclosure **requirements** (not legal sufficiency).

Explicitly **out of scope:** code, database, migrations, APIs, UI, solution changes, parked scaffold, POS/Platform implementation, default rates or fee amounts, penalty rates/amounts, due-date calendars, excused-day treatment, post-maturity rules (PLM-DOC-03), early-settlement unearned-interest formula, legal EIR/APR algorithm, legal compliance claims.

---

## Accepted methods

| Origination | MVP methods |
|---|---|
| Quick Loan | Flat / Add-On Finance Charge **only** |
| Traditional Loan | Flat / Add-On **or** Reducing-Balance Equal-Installment |

Deferred: equal-principal, interest-only, balloon, custom scripts, revolving, compound, variable-rate. Organization-supplied executable formulas are not authorized.

---

## Formulas

**Flat Per Term:** `I_raw = P × r`

**Flat Per Installment Period:** `I_raw = P × r × N`

Rate basis must be explicit. Do not infer from payment frequency.

**Reducing-balance (`i > 0`):** `A_raw = P × i / (1 − (1 + i)^(−N))`

**Reducing-balance (`i = 0`):** `A_raw = P / N`

Each installment: `Interest_k_raw = Opening Principal_k × i`; `Principal_k_raw = A_raw − Interest_k_raw`. Final installment reconciles remaining principal exactly.

Canonical: [../Product/interest-and-finance-charge-policy.md](../Product/interest-and-finance-charge-policy.md).

---

## Compatibility matrix

| Method | Added To Repayment | Deducted From Proceeds |
|---|---|---|
| Flat / Add-On | Allowed | Allowed (charge satisfied at disbursement; **not** also scheduled as unpaid interest) |
| Reducing-Balance Equal-Installment | Required | **Prohibited** |

---

## Rounding policy

- decimal money; never binary float
- PHP posted/display: 2 decimal places
- intermediate: at least 8 decimal places
- midpoint: **To Even** (`MidpointRounding.ToEven`)
- final-installment residual reconciliation; scheduled principal sums to Contract Principal

**PLM-D-00-12 Closed.** Canonical: [../Product/money-precision-and-rounding-policy.md](../Product/money-precision-and-rounding-policy.md).

---

## Fee policy

Bases: Fixed Amount; Percentage of Contract / Face Principal.

Treatments: Upfront Deducted; Financed; Scheduled (exactly one per fee on the snapshot).

Platform usage charge does **not** enter the borrower Loan.

Canonical: [../Product/fees-and-net-proceeds-policy.md](../Product/fees-and-net-proceeds-policy.md).

---

## Payment-allocation policy

Schedule: oldest due obligation first.

Components: Due Interest / Finance Charge → Due Principal → Due Scheduled Fees → Due Penalties.

Partial and multiple payments supported. Advance payment applies to future scheduled obligations after current/past due are satisfied; does not silently regenerate the schedule or reduce contracted flat finance charge. Excess is not inferred as principal prepayment. No borrower wallet in MVP.

Canonical: [../Product/payment-allocation-and-prepayment-policy.md](../Product/payment-allocation-and-prepayment-policy.md).

---

## Explicit remaining decisions

| ID | Remaining |
|---|---|
| PLM-D-00-07 | Subledger schema, GL, settlement/write-off accounting, cash refund workflow |
| PLM-D-00-08 | Calendar, penalties, excused days, early-settlement rebate, restructuring, write-off |
| PLM-D-00-11 | Legal/compliance/disclosure (including effective-cost formula) |
| PLM-DOC-03 | Schedule calendar, delinquency, penalties, maturity |

No default interest rate or fee amount was introduced.

---

## No-code / no-database statement

This package is **documentation only**. No `.cs`, `.csproj`, `ExItS.slnx`, migrations, database, API, UI, tests, POS, Platform implementation, or parked-scaffold changes.

Implementation remains **paused**. `feat/plm-01-scaffold` remains unmerged.

---

## Decision register

| ID | Outcome |
|---|---|
| PLM-D-00-07 | **Closed for MVP Product operational financial model** (historical: Open / Partially Resolved at package completion) |
| PLM-D-00-08 | **Closed for MVP Product business/calculation policy** (historical: Open / Partially Resolved at package completion) |
| PLM-D-00-12 | **Closed** — ToEven; PHP 2 dp; ≥8 intermediate; final-installment reconciliation |
| PLM-D-00-11 | **Open** |
| PLM-D-00-10 | **Closed / Product Owner Accepted** |

---

## Files changed

Created: interest/finance-charge, fees/net-proceeds, payment-allocation/prepayment, money-precision policies; ADR-003; ADR-004; this report.

Updated: product definition, architecture, security, development plan, roadmap, risks, README, FILE-MANIFEST, indexes, financial/payment/schedule/quick/traditional/loan-product/ledger baselines.

---

## Validation

Documentation only. `git diff --check` recorded at commit time.

No implementation authorization. No legal compliance claim.

---

## Git evidence

Recorded in the PLM-DOC-02 commit on `docs/plm-final-decisions`. Parked scaffold `feat/plm-01-scaffold` @ `4ec9e96e9149cd8d014adde3d694872a6d5ef576` not modified.

---

## Exact next documentation package

**PLM-DOC-03 — Schedule Calendar, Delinquency, Penalties & Maturity Decisions**

Do not start PLM-DOC-03 in this package. Implementation remains paused.
