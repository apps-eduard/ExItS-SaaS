# POS-REACT-INVENTORY-EXPIRY-UX-01

Progressive expiration tracking UX on the React inventory product detail page.

## Track expiration OFF

When `TracksExpiration = false` and inventory is tracked:

- Shows on-hand quantity and **Enable expiration tracking**
- Stock adjustment uses quantity + reason only (no expiry, batch, lot summary, or lot table)
- **Disable tracking** still disables inventory tracking (requires zero on-hand per existing rule)

## Track expiration ON

When `TracksExpiration = true` and inventory is tracked:

- Header shows on-hand, **Expiration tracking ON**, near-expiry warning days, and **Disable tracking** (expiration)
- Disable expiration is blocked while `onHandQuantity > 0`
- **Expiration inventory** summary: Good / Near expiry / Expired (from API totals; Good = sellable − near expiry)
- **Stock lots** list sorted earliest expiry first (table on wide screens, cards on mobile)
- Expired lots remain visible until written off

## Increase (In)

When expiration tracking is on:

- **Stock details**: required expiry date, optional batch/lot number
- Helper: expiry is per stock lot, not on the product
- Button label: **Add stock**

## Decrease (Out)

When expiration tracking is on:

- No expiry date field
- **Deduct from**: automatic earliest-expiring (FEFO) or choose a specific lot (manual)
- Manual mode uses lot picker; quantity cannot exceed selected lot on-hand
- Backend: automatic mode calls `ConsumeFefoAsync`; manual mode uses `ConsumeSpecificAsync` with `LotId`

## Enable / disable expiration tracking

- **Enable**: updates catalog product (`tracksExpiration: true`, default warning days 7) and refetches inventory queries
- **Disable**: allowed only at zero on-hand; sets `tracksExpiration: false` on catalog product

Product edit page still owns configuration only (track flag + warning days). Expiry dates belong to lots / stock-in movements.

## Movement history

`PosStockMovementDto` includes optional `expirationDate` and `lotNumber` when the movement is linked to a lot (`stock_movements.inventory_lot_id`).

## Receiving-flow verification

| Flow | Status | Notes |
|------|--------|-------|
| Direct Buy / Receive Stock | PASS | Requires expiry when `tracksExpiration` |
| PO Receiving | GAP | No expiry/lot fields on receive API or UI |
| Inventory adjustment (In) | PASS | Requires expiry when tracked |

## Deferred gaps

- PO receiving expiry capture (backend + UI) — next package
- FEFO on manual decrease only applies to **sellable** lots; expired write-off requires manual lot selection

## FEFO

Reuses domain `InventoryLotFefo` / `ConsumeFefoAsync`. No second lot model. Frontend describes behavior only.

## Files

- `InventoryDetailPage.tsx` — progressive UI
- `InventoryLotList.tsx` — responsive lot table/cards
- `inventory-detail-helpers.ts` — Good count, sort, disable rules
- `InventoryUseCases.cs` — FEFO manual adjust Out; movement lot enrichment
- `InventoryClientDtos.cs` — movement DTO lot fields
