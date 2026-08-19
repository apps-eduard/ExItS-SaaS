# ADR-007 — Early settlement and prepayment policy

**Status:** Accepted product policy (PLM-DOC-04); not implemented
**Date:** 2026-08-19
**Decisions:** Settlement and principal-prepayment product rules accepted; **PLM-D-00-08** Open / Partially Resolved; **PLM-D-00-11** Open

---

## Context

PLM needed Product Owner rules for full early settlement, Settlement Quotes, unearned finance-charge treatment, and explicit partial principal prepayment. Prior docs distinguished ordinary advance payment from principal prepayment without choosing rebate formulas.

---

## Decision

1. Full early settlement and partial principal prepayment are supported. MVP imposes **no** settlement penalty, prepayment penalty, or hidden settlement fee.
2. Ordinary advance payment follows PLM-DOC-02 allocation and does not silently become principal prepayment.
3. Full settlement requires a Settlement Quote. MVP validity = until end of stated Branch-local business date. Material events invalidate the quote. Consumption occurs only when settlement posting succeeds and Total Outstanding reaches zero.
4. Flat/Add-On added-interest: unearned future finance charge is rebated and not collected at full settlement. Earning uses snapshotted installment finance-charge components vs quote effective date.
5. Flat/Add-On deducted-interest: unearned portion becomes an auditable Finance-Charge Rebate Credit. Excess credit is Refund Payable, not a wallet.
6. Reducing-balance: future interest after the quote effective date is not charged. Current-period interest uses Actual-Days-in-Current-Period proration (`P_open × i × ElapsedDays / PeriodDays`).
7. Fees follow snapshot refundable/earned rules. Valid penalties remain unless waived/reversed. No new settlement penalty.
8. Partial prepayment default schedule treatment = **Reduce Term**. Recalculation creates a new schedule version; prior schedule remains visible.
9. Settlement Amount floors at PHP 0.00. No negative balances. No borrower wallet.

Canonical text: [../Product/early-settlement-and-principal-prepayment-policy.md](../Product/early-settlement-and-principal-prepayment-policy.md).

---

## Consequences

Settlement and prepayment have an approved calculation contract without default rates or penalties.

**Still open:** restructuring, write-off/recovery, legal/compliance including statutory rebate (PLM-D-00-11).

No settlement engine or schema is authorized by this ADR.
