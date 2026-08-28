# POS-REACT-OPENING-STOCK-01

Optional opening stock and unit cost during product create (React POS).

## Summary

Product create supports optional opening inventory when stock quantity tracking is enabled. Zero opening stock remains the default and is valid. Opening stock with quantity requires unit cost and creates a real `OpeningStock` movement; expiration-tracked products also require expiry and may include an optional batch/lot.

## Default behavior

- **Track stock quantity:** ON by default on create
- **Add opening stock now:** OFF by default
- Save with tracking ON and opening OFF → product tracked, on hand = 0, no movement, no lot, no unit cost required

## Create-time only

Opening stock UI appears only on **Create product**. Edit product shows tracking status (read-only) and directs users to **Inventory** for stock changes. Edit save never calls enable/opening APIs.

## Base inventory unit

Opening quantity and unit cost are always entered in the product **base inventory unit** (e.g. Pieces, Kilogram). Helper copy states the configured unit label dynamically.

Package conversion entry at create time: **DEFERRED** — no package picker for opening stock in v1.

## Unit cost and valuation

- User-facing label: **Unit cost** — purchase cost per base inventory unit
- Stored on the opening `stock_movements.unit_cost` column (nullable; only set for opening stock when supplied)
- **Stock value** = opening quantity × unit cost (projected in API/UI; not a separate persisted inventory costing layer)
- Does **not** change catalog selling price or package sell prices
- Aligns with direct-purchase line cost rules (> 0, max bound, money rounding)

## Inventory movement

Opening stock uses existing `StockMovementType.OpeningStock` with `StockMovementSourceType.Opening`. On-hand is updated via `InventoryAccount.Enable` movement effect — not a direct silent `OnHand` mutation without history.

When expiration tracking is ON, opening stock creates/uses `inventory_lots` via `InventoryLotStockService.ReceiveAsync`.

## Expiration

| Track expiration | Opening stock ON | Expiry | Batch/lot |
|------------------|------------------|--------|-----------|
| OFF              | ON               | Hidden | Hidden    |
| ON               | ON               | Required | Optional |

## Edit restrictions

Changing name, price, warning days, or packages on an existing product does not create stock movements.

## Idempotency

Create form uses React Query mutation pending state to block double submit. Enable endpoint remains single-shot per product (unique opening stock index).

## API

`POST /api/v1/pos/inventory/{productId}/enable`

```json
{
  "openingQuantity": 24,
  "unitCost": 18,
  "expirationDate": "2027-12-30",
  "lotNumber": "LOT-A123"
}
```

When `openingQuantity` > 0, `unitCost` is required.

## Tests

- `PosOpeningStockApiTests` — zero opening, cost required, movement projection, expiring lot, edit does not re-open
- `opening-stock-helpers.test.ts` — frontend validation and enable payload builder

## Next

Verify PO receiving and direct-buy flows use the same cost/expiry lot rules for consistency.
