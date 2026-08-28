# POS-EXPIRATION-TRACKING-INITIALIZATION-01

Make enabling expiration tracking with existing stock **safe, atomic, and clear**.

| Field | Value |
| --- | --- |
| Status | **Complete** |
| Branch | `feat/organization` |
| Start SHA | `74983205800c3a7e085928f0731eeeb02892ecf5` |

## Concepts (do not mix)

| Concept | Meaning |
| --- | --- |
| **Product configuration** | Track expiration + near-expiry warning days |
| **Existing-stock initialization** | Assign current OnHand into expiry lots **before** tracking turns ON when OnHand &gt; 0 |
| **Normal inventory ops** | Opening / Direct Buy / PO receive / Manual Increase / Decrease |

**Expiration initialization ≠ Opening Stock ≠ Manual Increase ≠ Purchase Receipt.**

Initialization does **not** change OnHand and does **not** create product-level stock or purchase movements.

## Rules

### `ZERO_STOCK_ENABLE`

OnHand = 0 → enable tracking immediately; no lots; no movements.

### `EXISTING_STOCK_INITIALIZATION`

OnHand &gt; 0 → require lot lines whose quantities **sum exactly** to authoritative OnHand; then enable tracking atomically.

### `MULTI_LOT_INITIALIZATION`

Multiple rows allowed (e.g. 6 + 4 = 10).

### `NO_ONHAND_MUTATION`

Before = After OnHand. No `ApplyMovementEffect(+qty)`. No OpeningStock / ManualIncrease / PurchaseReceipt product movements.

### `ATOMIC_ENABLE`

Product tracking flag + lot creates commit together. Failure rolls back; never leave TracksExpiration ON with unallocated stock.

### `CONCURRENCY_GUARD`

`expectedOnHandQuantity` + reload account; stock change while assigning → reject (`ExpirationAllocationStockChanged`). Tracking stays OFF.

### `FEFO_PRESERVED`

After init, FEFO / manual lot decrease unchanged.

### `DISABLE_ONLY_AT_ZERO`

Server rejects disable when OnHand &gt; 0 (`ExpirationDisableRequiresZeroOnHand`).

## API

`POST /api/v1/pos/inventory/products/{productId}/expiration-tracking/enable`

Capability: `ManageInventory`

Catalog PUT `tracksExpiration: true` with OnHand &gt; 0 → `ExpirationInitializationRequired` (must use enable endpoint).

## Audit

`EXPIRATION_INITIALIZATION_AUDIT=EXISTING_LOT_ACTOR`

Lot ledger rows use `StockMovementType.ExpirationInitialization` with authenticated `RecordedBy`. No product `stock_movements` row (avoids fake +qty business events).

## Migration note

`20260828*_AddExpirationInitializationMovementTypeCheck` updates `ck_stock_movements_movement_type` to include `ExpirationInitialization` (enum/Codes alignment). Product `stock_movements` rows are still **not** written for initialization — only lot ledger rows.

## UI

Inventory Detail: status header → stock lots (when ON) → adjust form (Increase requires expiry; Decrease uses FEFO/choose lot only).

Dialog when enabling with stock: default row qty = full OnHand; batch/lot optional; allocated/remaining live totals.
