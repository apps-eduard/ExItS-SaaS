# POS-PURCHASE-RECEIPT-REVERSAL-01

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**TASK:** POS-PURCHASE-RECEIPT-REVERSAL-01  
**START_SHA:** `0db24c57af93741badca99d93766ca0ad55d8253`  
**FEATURE_SHA:** `3eb5a041903be5d737fc97636798b54c9e190f2f`  

## CURRENT_RECEIPT_MODEL

- **PO GRN:** immutable `GoodsReceipt` / `GoodsReceiptLine` posted via receive; stock via `StockMovementType.PurchaseReceipt` at line `BaseUnitCost`; lots via `InventoryLotStockService.ReceiveAsync` when expiration-tracked; receive uses org-level inventory (`branchId: null`).
- **Direct purchase:** separate `DirectPurchaseReceipt` / lines; `StockMovementType.DirectPurchaseReceipt`; same org-level stock pattern.
- **PO status:** derived from net line `ReceivedQty` / outstanding (`Draft` → `Ordered` → `PartiallyReceived` → `Received`; cancel only pre-receipt).

## ROOT_CAUSE / GAP

Posted receipts could not be undone without deleting history or rewriting movements. Operators needed a compensating void that preserves auditability and original cost.

## REVERSAL_MODEL

Posted receipt → mark `Voided` (status + actor/time/reason) → compensating stock movements at **original receipt unit cost** → unwind PO received/short quantities → recompute PO status. Original receipt and original purchase movements remain permanently.

## FULL_REVERSAL_STATUS

**IMPLEMENTED** — whole-document void for GoodsReceipt and DirectPurchaseReceipt.

## PARTIAL_REVERSAL_STATUS

**DEFERRED** — line/qty partial reversal not implemented (avoids schema/UX complexity).

## DIRECT_PURCHASE_REVERSAL_STATUS

**SUPPORTED** — same void pattern via `POST /api/v1/pos/direct-purchase-receipts/{id}/void` (`ManageInventory`).

## REVERSAL_COST_SOURCE

**ORIGINAL_RECEIPT** — `GoodsReceiptLine.BaseUnitCost` / direct line `UnitCost`. Never latest acquisition cost.

## LOT_EXPIRY_REVERSAL_POLICY

If any receipt line is expiration-tracked: `InventoryLotStockService.ReverseReceiveSourceAsync` once for the receipt source id (PurchaseReceipt → PurchaseReceiptReversal / DirectPurchase → DirectPurchaseReceiptReversal). Fail closed when attributable lot qty is unavailable. Non-lot lines require `OnHandQuantity >= BaseQuantity`.

## INVENTORY_GUARD / NEGATIVE_STOCK_GUARD

**PASS** — insufficient on-hand / lot → `409 Conflict` (`GoodsReceiptVoidInsufficient` / `DirectPurchaseReceiptVoidInsufficient`). No negative stock to force a void.

## PURCHASE_ORDER_STATUS_POLICY

`PurchaseOrder.UnwindGoodsReceipt` decreases `ReceivedQty` / short-close from that GRN, then recomputes status (`Ordered` / `PartiallyReceived` / `Received`).

## NET_RECEIVED_QUANTITY_POLICY

Net received = posted receipt quantities minus voided receipts (voided GRNs unwind PO lines; voided receipts are not counted as still received).

## AUDIT_TRAIL_MODEL

Receipt retains: original id/lines/costs/timestamps; `Status`, `VoidedAtUtc`, `VoidedByUserId`, `VoidReason`. Compensating movements keep original unit cost and dedicated movement types.

## REVERSAL_IDEMPOTENCY_MODEL

- Domain: already `Voided` → `200` success, no second decrement.
- HTTP: `Idempotency-Key` + `X-Pos-Payload-Hash` with `goods_receipt.void` / `direct_purchase_receipt.void`.

## DOUBLE_REVERSAL_GUARD

Already voided → idempotent success (no duplicate reversal movements). Not a hard 409 for replay; insufficient stock still 409 before first successful void.

## BRANCH_SCOPE

**ORIGINAL_RECEIPT_BRANCH preserved** — receive/void use org-level movements (`branchId: null`); operator cannot choose another branch. No invented branch adjustments.

## CROSS_ORG_GUARD / CROSS_BRANCH_GUARD

**PASS** — foreign org receipt → not found / denied. No cross-branch void selector.

## PERMISSION_GUARD

- Goods receipt void: `ManagePurchasing`
- Direct purchase void: `ManageInventory`
- View-only / ReportingUser: denied  
No new enterprise capability invented.

## REPORTING_EFFECT / INVENTORY_HISTORY_EFFECT

On-hand reflects net of purchase + reversal movements. History retains original `PurchaseReceipt` / `DirectPurchaseReceipt` plus `PurchaseReceiptReversal` / `DirectPurchaseReceiptReversal` labels. Acquisition-cost lookup still uses positive acquisition types only (reversals excluded). No Supplier Payables / GL.

## REVERSAL_QUERY_MODEL / N_PLUS_ONE

Load receipt + PO; batch `ListByIds` products; product reservation locks; one lot reverse-source call; per-line movements with `Has*ReversalAsync` guards. **N_PLUS_ONE=NO** for product catalog/accounts.

## BACKEND_CHANGE_REQUIRED

**YES** — domain void, movement types 22/23, use cases, endpoints, EF mapping, React clients/UI.

## MIGRATION

**REQUIRED** — `20260830120000_AddPurchaseReceiptReversal`

### MIGRATION_REQUIRED_REASON

Persisted Posted/Voided status + void audit columns; expand `ck_stock_movements_movement_type`.

### SCHEMA_CHANGE

- `goods_receipts` / `direct_purchase_receipts`: `status`, `voided_at_utc`, `voided_by_user_id`, `void_reason` (+ checks)
- Movement type check includes `PurchaseReceiptReversal`, `DirectPurchaseReceiptReversal`

## POSTGRES_INTEGRATION_TESTS

`PosGoodsReceiptReversalApiTests` (**3** facts):

1. Full PO GRN void restores stock (net of later cost-seed receipt), preserves GRN, original unit cost on reversal, PO reopens to Ordered
2. Insufficient stock 409; cross-org 404; view-only 403; double void + HTTP idempotency no double decrement
3. Direct purchase insufficient then successful void; reversal movement cost/qty

## BACKEND_REGRESSION_TESTS

Purchasing/inventory unit filter run; known unrelated fail: `CreateBuyerProductAndLinkTests` (catalog bulk validation) — pre-existing, not introduced by this package.

## REACT_TARGETED_TESTS

- `PurchaseOrderDetailPage.reverse.test.tsx` (4)
- `DirectPurchaseDetailPage.test.tsx` (+ reverse/permission/insufficient)

## REACT_FULL / QUALITY GATES

| Gate | Result |
|------|--------|
| REACT_FULL_TEST_COUNT | 1312 |
| REACT_FULL_PASS | 1312 |
| REACT_FULL_FAIL | 0 |
| TYPECHECK | PASS |
| LINT | PASS (0 errors; pre-existing warnings only) |
| BUILD | PASS |
| NEW_TEST_SKIPS / ONLY / EXCLUSIONS | none |

## NEXT

**POS-SUPPLIER-PAYABLES-01** (or pilot reassessment: real payment providers / B2B checkout / device enforcement). Keep payables separate from receipt reversal and Customer Utang. Do not auto-pick FIFO or GL.
