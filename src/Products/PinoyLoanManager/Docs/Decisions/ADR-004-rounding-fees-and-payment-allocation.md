# ADR-004 — Rounding, fees, and payment allocation

**Status:** Accepted product policy (PLM-DOC-02); **PLM-D-00-12 Closed**; not implemented
**Date:** 2026-08-19
**Decisions:** **PLM-D-00-12** Closed; **PLM-D-00-07** and **PLM-D-00-08** Open / Partially Resolved; **PLM-D-00-11** Open

---

## Context

PLM needed deterministic money rounding, a disclosed fee model, Net Proceeds treatment, and a non-editable MVP payment-allocation contract. Prior WP04 docs left component order and rounding mode open.

---

## Decision

1. Authoritative money is **decimal**, never binary floating point. Initial currency **PHP**. Posted/display scale **2 decimal places**. Intermediate precision **at least 8 decimal places**. Round only at documented boundaries.
2. Midpoint rounding: **To Even** (`MidpointRounding.ToEven`). Final applicable installment receives residual centavos so scheduled principal sums to Contract Principal and scheduled totals reconcile.
3. Fee bases MVP: **Fixed Amount** or **Percentage of Contract / Face Principal**. Treatments: **Upfront Deducted**, **Financed**, **Scheduled** (exactly one per fee on the Loan snapshot).
4. ExItS Platform usage charge (Organization → Platform) must **not** enter the borrower Loan as interest, fee, proceeds deduction, or subledger amount.
5. Schedule allocation: **oldest due obligation first**.
6. Component order within that obligation: **Due Interest / Finance Charge → Due Principal → Due Scheduled Fees → Due Penalties**. Not organization-editable in MVP. Zero components skipped. Deducted finance charge is not outstanding scheduled interest.
7. Partial and multiple payments are supported. Advance payment satisfies future scheduled obligations in chronological order without silently regenerating the schedule or reducing contracted flat finance charge. Excess is not inferred as principal prepayment. No borrower wallet for true overpayment.

Canonical text: [../Product/money-precision-and-rounding-policy.md](../Product/money-precision-and-rounding-policy.md), [../Product/fees-and-net-proceeds-policy.md](../Product/fees-and-net-proceeds-policy.md), [../Product/payment-allocation-and-prepayment-policy.md](../Product/payment-allocation-and-prepayment-policy.md).

---

## Consequences

**Closed**

- PLM-D-00-12 — PHP posted precision, intermediate precision, ToEven midpoint, final-installment reconciliation

**Still open**

- operational subledger schema, GL, settlement accounting, write-off/recovery, cash refund details (PLM-D-00-07 remainder)
- calendar, penalties, excused days, early-settlement rebate (PLM-D-00-08 remainder / PLM-DOC-03)
- legal/compliance/disclosure (PLM-D-00-11)

No database, posting engine, or implementation is authorized by this ADR.
