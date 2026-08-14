# P8-WP04 — Basic Inventory

Phase marker: `P8-WP04-basic-inventory`

## Status

**Complete with documented risks.** Organization-isolated basic inventory with immutable stock movements, optional per-product tracking, atomic sale deduction/void restoration for Cash, ManualGCash, and Utang. **No** suppliers, warehouses, costing, offline inventory, or negative-stock override. P8-WP05 was not started.

Feature commit: `64f05e7fd5ab868beb62c7cce88ad7a15e21c7b8`

## Delivered capability

| Area | Delivered |
|---|---|
| Account | One `InventoryAccount` per org/product; `IsTracked`, optional `ReorderLevel`, denormalized `OnHandQuantity` |
| Movements | OpeningStock, ManualIncrease, ManualDecrease, SaleDeduction, SaleVoidRestoration (append-only) |
| Enable/disable | Optional opening qty (zero valid); duplicate opening blocked; disable only at zero on-hand |
| Adjustments | Stock In / Out with required reason; cannot go negative |
| Sales | Tracked products deduct atomically on checkout; untracked sell without movement |
| Voids | Compensating restoration; Utang void coordinates sale + credit + stock |
| Low stock | Tracked and OnHand ≤ ReorderLevel (no auto-reorder) |
| UOM | Catalog precision reused; UOM change blocked after inventory activity |
| Persistence | Migration `AddPosBasicInventory` (`20260730193916`) |
| API | `/api/v1/pos/inventory/*` |
| MAUI | `/inventory`, detail, adjust, low-stock; More-menu link |
| Features | `store-inventory-view` / `store-inventory-manage` |

## Inventory invariants

- On-hand is derived from movement effects; denormalized quantity is a protected projection only.
- Movement rows are never edited or deleted; corrections use compensating movements.
- Sale source uniqueness: `(organization_id, source_id, product_id, movement_type)` where `source_type = Sale`.
- Opening stock unique per org/product.
- Checkout stock validation is part of authorized sale create (not a bypassable client inventory grant).
- Platform SaaS billing and Utang repayments never affect stock.

## Quantity semantics

Whole: Piece, Pack, Box, Bottle, Can, Sachet. Measured (≤3 dp): Kilogram, Gram, Liter, Milliliter, Meter. Opening/adjustment quantities > 0 (except zero opening). No UOM conversion at the time of P8-WP04.

> **Later note:** Product-specific purchase/sell unit conversion (Dynamic Product Units) supersedes the historical “no UOM conversion” limitation for inventory/sales/purchasing paths. See [product-units-and-inventory-behavior.md](../engineering/product-units-and-inventory-behavior.md). This report remains an accurate historical record of P8-WP04 delivery.

## Commercial matrix

| State | View | Manage |
|---|---:|---:|
| Trialing / Active / GracePeriod | Grant | Grant |
| PastDue / Cancelled / Expired | Grant | Deny |
| Suspended / missing / stale / unknown | Deny | Deny |

## Explicit exclusions

Suppliers/purchasing/POs/receiving, warehouses/branches/bins, transfers, batches/lots/serials/expiry, cost/valuation/profit, returns/exchanges/refunds/damaged goods, reservation, auto-reorder, barcode labels, offline inventory/sync, negative-stock override, POS operational roles.

## Online-only

No inventory queue types, local stock projections, or offline adjustments. Offline inventory screens require reconnection; checkout remains unavailable offline.

## Tests and Android

| Suite | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| Full `ExItS.slnx` Release (test projects) | **805** | **0** | **0** |

Baseline 775 preserved and exceeded.

Android Release APK:

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/com.exits.pinoybusinesspos-Signed.apk`

Interactive device validation **not** claimed (`adb` unavailable) — **R-109** remains open.

## Risks

| ID | Notes |
|---|---|
| R-109 | No interactive Android inventory validation |
| Dev headers | Org/commercial/actor headers Development/Testing-only |
| Concurrent last unit | Serializable/xmin protected; dedicated race stress test not expanded |
| Online-only | Multi-device offline stock reconciliation deferred |

## Portfolio independence

Root `HealthCare/` must remain absent/untracked and outside `ExItS.slnx`.

## Documentation and Git

| Field | Value |
|---|---|
| Feature commit | `64f05e7fd5ab868beb62c7cce88ad7a15e21c7b8` |
| Docs hash-record commit | `df36203728aa244037b0bf83ad45b7a6f7504f50` |
| Final working tree | clean after push |

## Later addition

Intra-organization branch inventory transfers were added later as an overlay (`InventoryTransfer`, `InventoryBranchBalance`). Org `InventoryAccount` remains the sellable on-hand used by sales/PO/counts. See [pos-branch-inventory-transfers.md](../engineering/pos-branch-inventory-transfers.md).

Optional per-product expiration lots (`TracksExpiration`, `InventoryLot`, FEFO) were added later. See [pos-expiration-aware-inventory.md](../engineering/pos-expiration-aware-inventory.md). Historical P8-WP04 still did not deliver expiry.

### Catalog Track stock (Create + Edit)

`IsTracked` remains an inventory-account flag (not a catalog column). MAUI product forms expose **Track stock for this product** when the actor has `ManageInventory` and the device is online:

| Screen | Behavior |
|---|---|
| Create (`/catalog/products/new`) | Optional switch (default **off**). After catalog create, enable calls `POST .../inventory/{productId}/enable` with opening qty 0 when the switch is on. |
| Edit (`/catalog/products/{id}/edit`) | Same switch, seeded from `PosCatalogProductDto.IsTracked`. Catalog PUT runs first; then enable or disable when the value changed. |
| Disable guard | Turning tracking off requires on-hand **0** (same domain rule as Inventory detail). The Edit page blocks before save and shows `Catalog_TrackStockDisableRequiresZero`. |
| Failure after catalog save | If enable/disable fails after a successful product update, Edit stays on the form with an error; the catalog save is not rolled back. |

Stock In / Out, reorder, and movements stay on Inventory screens. Stock Count lists **tracked** products only — turn tracking on here (or in Inventory) before counting.

## Exact next work package

**P8-WP05 — Expenses** completed separately; next authorized WP is **P8-WP06 — Dashboard and Reports**.
