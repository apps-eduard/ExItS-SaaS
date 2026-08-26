# ADR-012 — Write-off, recovery, and collections case policy

**Status:** Accepted product policy (PLM-DOC-06); not implemented
**Date:** 2026-08-19
**Decisions:** Write-Off, Recovery, PTP, and Collection Case product rules accepted; **PLM-D-00-08 Closed for MVP Product business/calculation policy** (write-off/recovery/collections portion)

---

## Context

PLM needed Product Owner rules for Write-Off classification, post-write-off behavior, Recovery Payments, Promise to Pay, Collection Cases, and collection conduct boundaries. Prior docs listed write-off/recovery as open.

---

## Decision

1. **Write-Off** is an authorized operational classification. It does not delete Loan, Disbursement, Payment, or Borrower history and does not imply legal forgiveness.
2. Write-Off requires eligible condition, reason, effective date, component snapshot, high-risk approval (maker/checker or Owner Override), and audit. **No default DPD threshold.**
3. After Write-Off effective date: stop new finance charge, fees, and penalties; preserve existing balances and prior waivers/reversals; do not capitalize written-off components.
4. Written-off components are tracked separately: Principal, Interest/Finance Charge, Fees, Penalties.
5. **Recovery Payment** after Write-Off has its own identity, updates subledger and cash accountability, does not delete Write-Off, does not silently restore Active status.
6. Recovery allocation: oldest obligation first, then Interest → Principal → Fees → Penalties (unless future legal/accounting policy replaces).
7. When all written-off components reach zero: **Recovered / Closed After Write-Off**; Write-Off history remains.
8. **Promise to Pay** is operational metadata only — not Payment, not schedule change, not penalty waiver.
9. **Collection Case** supports Past Due, Matured Past Due, Broken PTP, hardship/restructuring, Written-Off recovery. Notes are auditable, not editable financial history.
10. Collection conduct boundaries documented; no harassment features; legal sufficiency remains PLM-D-00-11.

Canonical text: [../Product/write-off-and-recovery-policy.md](../Product/write-off-and-recovery-policy.md), [../Product/collections-case-and-promise-to-pay-policy.md](../Product/collections-case-and-promise-to-pay-policy.md).

---

## Consequences

Write-Off, Recovery, PTP, and Collection Case have approved MVP product contracts.

**Still open:** persistence schema, external journal/export, GL projection (PLM-D-00-07 remainder), legal/compliance (PLM-D-00-11).

No Write-Off/Recovery engine or schema is authorized by this ADR.
