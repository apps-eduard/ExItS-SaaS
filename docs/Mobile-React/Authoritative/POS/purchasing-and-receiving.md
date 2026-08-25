# Purchasing and Receiving

## CURRENT — external / manual PO

| Topic | Status | Evidence |
|-------|--------|----------|
| `PurchaseOrder` aggregate | PROVEN_CURRENT | Domain |
| Statuses | Draft, Ordered, PartiallyReceived, Received, Cancelled | PROVEN_CURRENT |
| Lines + purchase unit conversion | PROVEN_CURRENT | `PurchaseUnitId`, `MultiplierToBaseSnapshot` |
| Submit / order | PROVEN_CURRENT | Does **not** create inventory movements |
| Goods receipt | PROVEN_CURRENT | Creates `StockMovementType.PurchaseReceipt` |
| Partial receipt | PROVEN_CURRENT | PartiallyReceived status path |
| Direct purchase receipt | PROVEN_CURRENT | Bypass PO; still movement-based stock-in |
| Idempotency / concurrency | PROVEN_CURRENT | Application patterns + DB constraints |

APIs: `/api/v1/pos/purchase-orders`, `/api/v1/pos/goods-receipts`, `/api/v1/pos/direct-purchase-receipts`

## CURRENT — connected PO lifecycle

Statuses include New, Accepted, Declined, Preparing, Fulfilled, Withdrawn, ChangesProposed (domain).

| Event | Inventory effect |
|-------|------------------|
| Connection accepted | None |
| Product exposed/shared/price changed | None |
| PO drafted/submitted | None |
| Supplier accepted/preparing/fulfilled | None until buyer goods receipt |
| Goods receipt / receive | Inventory increases (base qty) |
| Discrepancies | Short/Damaged/WrongItem/Expired/Rejected/Other — PROVEN_CURRENT |

## Invariant (preserve)

**BUYER INVENTORY CHANGES ONLY THROUGH GOODS RECEIPT / RECEIVE PO / DIRECT RECEIPT PATHS.**

## Offline

Purchasing admin OnlineRequired; `/purchasing/new` Queueable in policy matrix; linked products OfflineCapable read.

## MAUI / React

MAUI: `/purchasing*`, connected incoming orders.
React: **PARTIAL** — RMAP-17 buyer PO/GRN/direct purchase; seller connected incoming PO inbox remains deferred.

## Tests

`PurchaseOrderDomainTests`, `ReceiveStockInventorySemanticsTests`, connected PO lifecycle tests.
