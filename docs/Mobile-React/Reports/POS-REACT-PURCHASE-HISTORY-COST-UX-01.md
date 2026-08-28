# POS-REACT-PURCHASE-HISTORY-COST-UX-01

Improve internal purchase/inventory **cost visibility and receipt history** without introducing a costing engine.

| Field | Value |
| --- | --- |
| Status | **Complete** |
| Branch | `feat/organization` |
| Depends on | [POS-PURCHASE-RECEIVING-CONSISTENCY-01](POS-PURCHASE-RECEIVING-CONSISTENCY-01.md), [POS-REACT-ACTOR-TRACEABILITY-UI-01](POS-REACT-ACTOR-TRACEABILITY-UI-01.md) |

## Goal

Owners/managers can see on **detail/history** surfaces:

- What was purchased, how many, purchase cost, total
- Who recorded/received, when, from whom
- Expiry/lot and discrepancies
- Inventory movement acquisition cost when present

## Cost ownership (unchanged)

| Information | Owner / unit |
| --- | --- |
| Selling price | Product / Today's Prices |
| Opening unit purchase cost | Opening Stock (base unit) |
| Direct Buy cost | Direct Purchase Receipt (base unit) |
| PO / Goods Receipt cost | Purchase unit (`unitPurchaseCost` / snapshot) |
| Inventory movement UnitCost | Base inventory unit when known |
| Manual adjustment | Correction — **no** purchase cost |

### Reconciliation example

2 Cases × ₱240 / Case = **₱480** (PO / GRN purchase-unit)

48 Pieces × ₱10 / Piece = **₱480** (inventory movement base-unit)

## UX surfaces

| Surface | Cost visibility |
| --- | --- |
| InventoryDetail movement history | Unit purchase cost + stock value when `unitCost != null`; friendly movement type labels; omit cost when null (never ₱0) |
| DirectPurchaseDetail | MoneyDisplay totals/lines; notes; expiry/lot; actor |
| PurchaseOrderDetail | Purchase-unit cost, ordered value, order total; enhanced GRN cards |
| Goods receipt history on PO detail | Receipt value, line snapshots, expiry/lot, nonzero discrepancies, actors |
| PurchaseOrderReceive | Context: outstanding + unit purchase cost (action screen, not history) |
| Lists | Remain compact (no actor/cost matrix) |

## Explicit non-goals

- FIFO / weighted-average / COGS / GL / profit / margin
- Automatic selling-price changes
- Cost editing from history
- RBAC broadening
- Customer/Personal/public storefront purchase-cost disclosure

`CUSTOMER_PURCHASE_COST_EXPOSURE=NO`

## Movement source links

`MOVEMENT_SOURCE_LINKS=DEFERRED` — DTO has `sourceType`/`sourceId` GUID only; no batch human-readable source projection in this package (avoids N+1). Friendly **movement type** labels are shown instead.

## Files

- `purchase-cost-display.ts` (+ tests)
- `InventoryDetailPage.tsx` (+ cost test)
- `DirectPurchaseDetailPage.tsx` (+ test)
- `PurchaseOrderDetailPage.tsx` (+ cost test)
- `PurchaseOrderReceivePage.tsx` (unit cost context)
- Locale keys: en, fil-PH, ceb-PH, ilo-PH, hil-PH
