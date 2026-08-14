# Product units and inventory behavior — implementation report

**Status:** Implemented  
**Starting SHA:** `64d177c827e16d4d8421d6c9852368c42acdb0b5`  
**Feature SHAs:**
- `b77031a4` — feat(catalog): add product units, usage behavior, and conversions
- `ff3896b5` — docs(inventory): record product units feature commit hash

**Final SHA:** `ff3896b5180e03434367e3bbf6a33efbf584ab5a`  
**Related:** [engineering note](../engineering/product-units-and-inventory-behavior.md)

## Architecture audit (pre-implementation)

| Area | Finding |
|------|---------|
| Quantity type | `decimal` / PostgreSQL `numeric(18,3)` everywhere (inventory, PO, GRN, sales, lots, transfers, counts) |
| Cost method | Purchase cost **operational only** — no WA/FIFO/COGS valuation engine; preserved |
| UOM model | Single `UnitOfMeasure` enum per product; no pack conversion before this package |
| SellingMode | `PerItem` / `ByWeight` (kg); ByWeight maps to custom measured sell unit |
| Connected suppliers | Phase 1 link left conversion-ready; extended with multiplier/package metadata |

## Delivered

- Product usage flags + presets (Buy and sell / Bulk / Ingredient / Made product / ingredient+sellable)
- Authoritative **base inventory unit** = existing `UnitOfMeasure`
- Product-specific purchase & sell units (`pos.product_units`) with decimal multiplier to base
- Historical conversion snapshots on sales, PO lines, GRN; inventory movements use **base** qty
- PurchaseStockService uses `GoodsReceiptLine.BaseQuantity`
- Connected supplier link conversion metadata; selective offline projection only
- Adjust Stock optional purchase-unit helper → base qty
- LocalStore **v9** sell units + linked conversion columns
- MAUI: plain-language usage + progressive package editors + selling-option picker
- Migration `20260814200000_AddPosProductUnitsAndBehavior` seeds 1:1 units for existing products

## Explicit exclusions (deferred)

- Recipe / BOM editor
- Production batches / automatic ingredient consumption / yield / waste
- New costing method
- Full supplier catalog offline
- Global (non-product-specific) package conversions

## Validation gates

| Gate | Result |
|------|--------|
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Next

- Apply migration on validation databases / Local Validation reset
- Optional Testcontainers smoke for the new migration
- Recipe/BOM phase when authorized
