# ADR-003 — Supported interest and schedule methods

**Status:** Accepted product policy (PLM-DOC-02); not implemented
**Date:** 2026-08-19
**Decisions:** Product calculation methods accepted; **PLM-D-00-08** Open / Partially Resolved; **PLM-D-00-11** Open

> **Final status note:** **PLM-D-00-08 Closed for MVP Product business/calculation policy** at final review. See [PLM-decision-status-summary.md](PLM-decision-status-summary.md).

---

## Context

PLM needed Product Owner rules for which contractual calculation methods MVP may use, how rate bases work, and which interest treatments are compatible. Prior WP04 docs recorded modes without choosing formulas.

Default rates, legal EIR/APR, due-date calendars, and penalties are out of scope.

---

## Decision

1. Quick Loan MVP uses **Flat / Add-On Finance Charge only**.
2. Traditional Loan MVP may use **Flat / Add-On** or **Reducing-Balance Equal-Installment Amortization**.
3. Other methods (equal-principal, interest-only, balloon, custom scripts, revolving, compound, variable-rate) are **deferred**. Organization-supplied executable formulas are **not** authorized.
4. Flat rate basis is explicit: **Per Term** (`I_raw = P × r`) or **Per Installment Period** (`I_raw = P × r × N`). Do not infer basis from payment frequency.
5. Flat supports Added To Repayment and Deducted From Proceeds. Deducted finance charge is satisfied at disbursement and must **not** also be scheduled as unpaid interest.
6. Reducing-balance supports **Added To Repayment only**. Combination with deducted-interest is **prohibited**. Periodic rate must match the installment period; no silent annual/monthly/daily conversion.
7. Every Loan snapshots method, version, treatment, rate, rate basis, fees, rounding, allocation, and calculated outputs. Later product/template edits must not silently recalculate historical Loans.

Canonical text: [../Product/interest-and-finance-charge-policy.md](../Product/interest-and-finance-charge-policy.md).

---

## Consequences

Product configuration and a future calculation engine have an approved method set.

**Still open**

- due-date / calendar rules (PLM-DOC-03)
- penalties / rates / caps (PLM-DOC-03)
- excused-day treatment (PLM-DOC-03)
- early-settlement future-interest treatment
- restructuring and write-off
- legal/compliance/disclosure (PLM-D-00-11)
- default or maximum rates (never invented here)

No calculation engine, schema, or implementation is authorized by this ADR.
