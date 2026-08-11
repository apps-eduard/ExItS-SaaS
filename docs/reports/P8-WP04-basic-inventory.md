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

Whole: Piece, Pack, Box, Bottle, Can, Sachet. Measured (≤3 dp): Kilogram, Gram, Liter, Milliliter, Meter. Opening/adjustment quantities > 0 (except zero opening). No UOM conversion.

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

## Exact next work package

**P8-WP05 — Expenses** completed separately; next authorized WP is **P8-WP06 — Dashboard and Reports**.
