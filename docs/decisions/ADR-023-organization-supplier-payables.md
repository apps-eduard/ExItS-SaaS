# ADR-023 — Organization Supplier Payables (AP) vs Customer Utang (AR)

[Decisions](README.md) | [POS-SUPPLIER-PAYABLES-01](../Mobile-React/Reports/POS-SUPPLIER-PAYABLES-01.md)

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-30 |
| Related | ADR-019, P10 purchasing/suppliers, POS-PURCHASE-RECEIPT-REVERSAL-01 |

## Context

PinoyBusinessPOS purchasing posts goods receipts and direct purchases with acquisition cost, but historically deferred **supplier accounts payable**. Architecture tests forbade AP table/type names to keep P10 WP01/WP02 scoped.

Operators need a small-business flow: receive stock → optionally pay part now → remaining balance owed to the supplier → record later supplier payments — without mixing **Customer Utang (AR)** or introducing GL/FIFO.

## Decision

1. **Supplier Payables** are organization-owned POS product data, separate from Customer Utang / Business Credit and from Personal Utang (ADR-019).
2. Canonical types: `SupplierPayable`, `SupplierPayablePayment` (tables `supplier_payables`, `supplier_payable_payments`). Do **not** reuse `CreditEntry`, `Repayment`, customer tables, or invent `AccountsPayable` / `SupplierInvoice` GL-style aggregates.
3. **PAYABLE_ORIGIN = POSTED_RECEIPT** — obligation arises when a GoodsReceipt or DirectPurchaseReceipt is posted, not when a PO is created/submitted.
4. Payables are **organization-scoped** (matching org-level purchase stock). No branch payable ledger in this ADR.
5. Supplier payments **record settlement only** — they must not change inventory quantity, acquisition UnitCost, Sale COGS, or receipt line costs.
6. Receipt reversal: if the payable has **no** supplier payments, void the payable with the receipt; if any payment exists, **block** receipt reversal (conflict) until a future payment-reversal package.
7. Architecture guards may forbid legacy/out-of-scope names (`AccountsPayable`, `SupplierInvoice`, `accounts_payable`, `supplier_invoices`, `supplier_payments`) while **allowing** `SupplierPayable*` and `supplier_payable*` tables authorized by this ADR.
8. UI language for PH operators may say “Supplier Credit” / “Balance Due”; internal/code naming remains SupplierPayable (never reuse “Utang” for supplier debt).
9. **Reporting** is organization **as-of** (current payable state), not a period sales report. Report endpoint returns summary + per-supplier balances + payable rows; CSV export is client-side via the existing operational report export stack (`store-export`).

## Consequences

### Positive

- Clear AR vs AP separation.
- Audit-friendly purchase → payable → payment history.
- Safe interaction with purchase receipt reversal.
- Practical org-wide “what do we owe?” report without inventing GL.

### Negative / Follow-on

- Supplier payment void/refund deferred.
- No GL journals, aging buckets beyond optional due date, or gateway execution.
- Architecture tests and prior “no AP” docs must be updated deliberately (this ADR).
- Historical period reconstruction of payables is out of scope for the initial as-of report.
## Rejected alternatives

- Reusing Customer Utang entities/endpoints for suppliers.
- Creating payables on PO create/submit without receipt.
- Mutable supplier balance field as sole authority without payment history.
- Automatic FIFO / weighted-average / GL coupling.
