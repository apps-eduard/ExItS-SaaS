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

## Dual entry points

| Information | Owner |
|-------------|-------|
| Selling price | Product / Today's Prices |
| Package selling price | Product package configuration |
| Opening quantity | Product create (optional) **or** Inventory → Add opening stock |
| Opening purchase cost | Opening inventory movement |
| New purchase quantity/cost | Receiving / Direct Buy / PO |
| Expiry / Batch-Lot | Inventory lot / Receiving |
| On-hand stock | Inventory |

### 1. Product create (convenience)

Optional **Add opening stock now** when track quantity is ON. Selling price stays in the Product section.

### 2. Inventory detail (after create)

When a product is tracked with zero stock and no opening movement yet, Inventory shows **Add opening stock** (quantity + unit purchase cost + optional expiry/lot). After the first opening movement, the page shows normal actions (stock adjustment, etc.) — not add-opening again.

## API

`POST /api/v1/pos/inventory/{productId}/enable` — enable tracking; optional opening on first enable.

`POST /api/v1/pos/inventory/{productId}/opening-stock` — add opening stock on an already tracked account with zero on-hand and no prior opening movement.

```json
{
  "openingQuantity": 24,
  "unitCost": 18,
  "expirationDate": "2027-12-30",
  "lotNumber": "LOT-A123"
}
```

Account DTO includes `hasOpeningStock` so clients can switch UI modes.

## Unit cost and valuation

- User-facing label: **Unit purchase cost** — what you paid per base inventory unit (not selling price)
- Stored on the opening `stock_movements.unit_cost` column (nullable; only set for opening stock when supplied)
- **Stock value** = opening quantity × unit cost (projected in API/UI; not a separate persisted inventory costing layer)
- Does **not** change catalog selling price or package sell prices
- Aligns with direct-purchase line cost rules (> 0, max bound, money rounding)

## Inventory movement

Opening stock uses existing `StockMovementType.OpeningStock` with `StockMovementSourceType.Opening`. On-hand is updated via movement effect on `InventoryAccount.Enable` or `InventoryAccount.RecordOpeningStock` — not a direct silent `OnHand` mutation without history.

When expiration tracking is ON, opening stock creates/uses `inventory_lots` via `InventoryLotStockService.ReceiveAsync`.

## Expiration

| Track expiration | Opening stock ON | Expiry | Batch/lot |
|------------------|------------------|--------|-----------|
| OFF              | ON               | Hidden | Hidden    |
| ON               | ON               | Required | Optional |

## Edit restrictions

Changing name, price, warning days, or packages on an existing product does not create stock movements.

## Idempotency

Create form uses React Query mutation pending state to block double submit. Enable and add-opening-stock endpoints remain single-shot per product (unique opening stock index).

## Tests

- `PosOpeningStockApiTests` — zero opening, cost required, movement projection, expiring lot, deferred add-opening-stock, duplicate rejection, edit does not re-open
- `InventoryAccountDomainTests` — `RecordOpeningStock` on tracked zero account
- `opening-stock-helpers.test.ts` — frontend validation and enable payload builder
- `inventory-detail-helpers.test.ts` — `canAddOpeningStock`

## Next

Verify PO receiving and direct-buy flows use the same cost/expiry lot rules for consistency.
