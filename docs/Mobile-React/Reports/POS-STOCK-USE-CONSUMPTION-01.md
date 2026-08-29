# POS-STOCK-USE-CONSUMPTION-01

## Summary

Durable **Stock Use** inventory document for intentional business consumption without creating a sale, revenue, production, or waste event.

| Field | Value |
|--------|--------|
| STOCK_USE_DOMAIN_MODEL | `StockUse` + `StockUseLine` (Posted/Voided); number `SU-YYYYMMDD-NNNNNN` |
| STOCK_USE_PRODUCT_ELIGIBILITY | Active + inventory-tracked; any ProductBusinessUsage (UX defaults InternalUse) |
| STOCK_USE_REASON_MODEL | `InternalOperations` / `StaffUse` / `SampleOrTesting` / `Other` |
| STOCK_USE_INVENTORY_PATH | `ApplyMovementEffect` + `StockMovement.StockUse` (−qty); FEFO via `ConsumeFefoAsync` when expiration tracked |
| STOCK_USE_MOVEMENT_SOURCE | `StockMovementType.StockUse` + `StockMovementSourceType.StockUse` |
| STOCK_USE_ATOMICITY | Product reservation locks + single SaveChanges for all lines |
| STOCK_USE_UNIT_CONVERSION | ProductUnit → base quantity (existing conversion) |
| STOCK_USE_LOT_POLICY | FEFO (existing) when TracksExpiration |
| STOCK_USE_COST_SOURCE | Optional last acquisition UnitCost snapshot (Opening/PurchaseReceipt/DirectPurchase) |
| STOCK_USE_COST_ACCURACY | DEFERRED when no acquisition snapshot; never SellingPrice |
| STOCK_USE_CORRECTION_MODEL | REVERSAL (`Void` → `StockUseVoidRestoration` + lot RestoreSourceAsync) |
| STOCK_USE_PERMISSION | `ManageInventory` (mutate) / `ViewInventory` (list/get) |
| STOCK_USE_OFFLINE_MODE | ONLINE_ONLY (idempotency scope only) |
| STOCK_USE_SALES_REPORT_EFFECT | NONE |
| STOCK_USE_PRODUCT_USAGE_EFFECT | NONE |
| PRODUCTION_INTEGRATION | DEFERRED |
| WASTE_LOSS_INTEGRATION | DEFERRED |

## Existing architecture (preflight audit)

```
EXISTING_STOCK_USE_MODEL=NONE
EXISTING_INVENTORY_DECREASE_PATH=ManualDecrease / SaleDeduction / TransferOut / StockCountVarianceDecrease
EXISTING_COST_VALUATION=UnitCost on acquisition movements only
EXISTING_LOT_DEPLETION=InventoryLotStockService FEFO
EXISTING_REVERSAL_MODEL=Sale void / transfer cancel (manual adjust had none)
```

## API

`/api/v1/pos/inventory/stock-uses` — list, get, create, void

## React

- `/inventory/stock-use` list
- `/inventory/stock-use/new` create (Internal use | All stock picker)
- `/inventory/stock-use/:id` detail + void
- Inventory list chip + product detail shortcut

## Migration

`20260829170000_AddPosStockUse` — tables `stock_uses`, `stock_use_lines`, sequences; expands stock_movements check constraints; adds `ux_stock_movements_stock_use_source` (DB-only unique index; EF snapshot keeps last identical-column filtered unique as transfer, per existing convention).

`MIGRATION_APPLIED_LOCAL=YES` (Local Validation `exits_pos` @ 15534).

## Validation (this package)

| Check | Result |
|--------|--------|
| Backend StockUse unit tests | 13 passed |
| React stock-use labels + i18n parity | 15 passed |
| React typecheck / lint / build | PASS (lint: 0 errors, existing warnings) |
| POS API `/health` | Healthy (`:8092`) |
| Conflict markers | 0 (in package sources) |
| `git diff --check` | clean |

## Explicit deferrals

- Production / recipe material consumption — separate package
- Waste / loss / damage — separate package
- Offline Stock Use mutation queue — ONLINE_ONLY
- Lot-accurate cost when only last-acquisition UnitCost exists — DEFERRED accuracy when null
