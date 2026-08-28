# POS-PURCHASE-RECEIVING-CONSISTENCY-01

Unify Direct Buy and PO goods receipt with opening-stock inventory ownership: base-unit quantity, acquisition UnitCost, and expiration lots.

## Ownership

| Information | Owner |
|-------------|-------|
| Selling price | Product / Today's Prices |
| Opening cost | Opening Stock |
| Direct purchase cost | Direct Buy receipt line → `StockMovement.UnitCost` (base unit) |
| PO purchase cost | PO / Goods Receipt `UnitPurchaseCostSnapshot` (purchase unit) → movement base UnitCost |
| Movement UnitCost | Cost per **base** inventory unit when known |
| Expiry / Batch-Lot | Received inventory lot (Opening / Direct Buy / PO GRN) |
| Manual adjustment | Stock **correction**, not purchasing |

## Flows

### Opening Stock
Optional, one-time, unit purchase cost per base unit, expiry required when tracked.

### Direct Buy
Quantity and unit cost are already in base inventory units. Movement now preserves `UnitCost`. Expiration products require expiry; lot optional; `InventoryLotStockService.ReceiveAsync` unchanged.

### PO Receiving
- Good qty in purchase units → `BaseQuantity = qty × MultiplierToBaseSnapshot`
- `BaseUnitCost = UnitPurchaseCostSnapshot ÷ MultiplierToBaseSnapshot` via `ProductUnitConversion.ToBaseUnitCost`
- Movement: `PurchaseReceipt` with base qty + base UnitCost
- Expiry/lot stored on **GoodsReceiptLine** (not PO line); required when product tracks expiration and good qty > 0
- Damaged/rejected/short-only lines do not require expiry and do not enter sellable inventory

### Manual adjustment
Does **not** require unit purchase cost. Expiration-tracked increases keep existing expiry/lot rules. UI copy clarifies adjustment is not a purchase.

## Example (PO case package)

Base: Piece · Package: Case × 24 · Receive 2 Cases @ ₱240/Case

| Field | Value |
|-------|-------|
| BaseQuantity | 48 |
| Movement.UnitCost | ₱10 / Piece |
| Stock value | ₱480 |
| LineTotalSnapshot | ₱480 (2 × 240) |

## MULTI_LOT_SAME_RECEIPT

**DEFERRED** — one inventory movement per product per goods receipt (unique index). Use separate partial receipts for different expiry lots of the same PO line.

## Migrations

- Existing: `20260828160000_AddStockMovementUnitCost` (`stock_movements.unit_cost`)
- Added: `20260828170000_AddGoodsReceiptLineExpiryLot` (`goods_receipt_lines.expiry_date`, `lot_number`)

## Out of scope

FIFO/weighted-average costing, COGS, GL — acquisition UnitCost on purchase movements only.
