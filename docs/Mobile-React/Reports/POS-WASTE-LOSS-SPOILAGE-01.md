# POS-WASTE-LOSS-SPOILAGE-01

## Summary

Durable **Waste / Loss** inventory document for intentional write-off of unusable, damaged, expired, spilled, or missing stock. Separate from Stock Use, Production, Sale, Manual Adjustment, and Stock Count variance. Never creates revenue.

| Field | Value |
|--------|--------|
| EXISTING_WASTE_LOSS_MODEL | NO (created this package) |
| EXISTING_WRITE_OFF_MODEL | YES (utang write-off only — not inventory) |
| EXISTING_MANUAL_DECREASE_MODEL | YES (`ManualDecrease`) |
| EXISTING_STOCK_COUNT_MODEL | YES (`StockCountVariance*`) |
| EXISTING_LOT_WRITE_OFF_MODEL | NO (explicit lot consume added here) |
| EXISTING_COST_LOSS_MODEL | NO (snapshots added here) |
| EXISTING_REVERSAL_MODEL | PARTIAL (StockUse / Production void patterns reused) |
| WASTE_LOSS_DOMAIN_MODEL | `WasteLoss` + `WasteLossLine` (Posted/Voided); number `WL-YYYYMMDD-NNNNNN` |
| WASTE_LOSS_REASON_MODEL | `Spoiled` / `Expired` / `Damaged` / `Broken` / `Spillage` / `MissingOrShrinkage` / `Other` (Other requires notes) |
| WASTE_LOSS_PRODUCT_ELIGIBILITY | Active + inventory-tracked; any `ProductBusinessUsage` (Resale, Ingredient, InternalUse, ProducedItem) |
| WASTE_LOSS_DATE_MODEL | `OccurredAtUtc` (optional; defaults to posting time) + immutable `CreatedAtUtc` |
| WASTE_LOSS_MOVEMENT_TYPE | `StockMovementType.WasteLoss` + `StockMovementSourceType.WasteLoss`; void → `WasteLossVoidRestoration` |
| WASTE_LOSS_ATOMICITY | Product reservation locks + single SaveChanges for all lines |
| WASTE_LOSS_CONCURRENCY | Same inventory product reservation locks as Sale / StockUse / Production |
| WASTE_LOSS_IDEMPOTENCY | Client `WasteLossId` + `inventory.waste_loss` idempotency scope |
| WASTE_LOSS_LOT_POLICY | Exact lot required when `TracksExpiration`; `ConsumeSpecificAsync` only (never FEFO) |
| EXPIRED_STOCK_QUICK_FLOW | DEFERRED |
| WASTE_LOSS_COST_SOURCE | Authoritative acquisition / ProductionOutput UnitCost via inventory cost resolver; never SellingPrice |
| WASTE_LOSS_COST_STATUS | `Complete` / `Partial` / `Unavailable` (Production cost semantics) |
| SELLING_PRICE_USED_AS_COST | NO |
| COST_HISTORY_IMMUTABLE | YES (line + document snapshots at post) |
| WASTE_LOSS_CORRECTION_MODEL | REVERSAL (`Void` → restore + `WasteLossVoidRestoration` + `RestoreSourceAsync`) |
| WASTE_LOSS_REVERSAL_LOT_POLICY | Exact lot restoration via `RestoreSourceAsync` |
| HARD_DELETE_POSTED_WASTE_LOSS | NO |
| WASTE_LOSS_PERMISSION_MODEL | `ManageInventory` (create/void) / `ViewInventory` (list/get) |
| WASTE_LOSS_OFFLINE_MODE | ONLINE_ONLY |
| SALE_INTEGRATION | Separate; Waste/Loss does not create Sale |
| STOCK_USE_INTEGRATION | SEPARATE |
| PRODUCTION_INTEGRATION | SEPARATE (no auto waste from production variance; no material restore on waste void of produced items beyond output stock restore) |
| PURCHASING_INTEGRATION | Separate post-receipt event; does not edit Goods Receipt |
| STOCK_COUNT_INTEGRATION | SEPARATE |
| LOW_STOCK_INTEGRATION | Natural OnHand update; existing low-stock UI |
| PROFIT_REPORT_INTEGRATION | DEFERRED |

## API

`/api/v1/pos/inventory/waste-losses` — list, get, create, void

## React

- `/inventory/waste-loss` list
- `/inventory/waste-loss/new` create (all tracked products; lot picker when expiration-tracked)
- `/inventory/waste-loss/:id` detail + void
- Inventory list chip + product detail shortcut
- i18n: en, fil-PH, ceb-PH, ilo-PH, hil-PH

## Migration

`20260829190000_AddPosWasteLoss` — tables `waste_losses`, `waste_loss_lines`, `waste_loss_number_sequences`; expands stock_movements check constraints; adds `ux_stock_movements_waste_loss_source` (DB unique index; EF snapshot keeps last identical-column filtered unique as transfer, per existing convention).

`MIGRATION_APPLIED_LOCAL=YES` (Local Validation `exits_pos` @ 15534).

## Validation (this package)

| Check | Result |
|--------|--------|
| Backend WasteLoss unit tests | 18 passed |
| Backend related inventory filter (WasteLoss/StockUse/Production/DirectPurchase) | 56 passed |
| React targeted (waste-loss + purchase-cost-display + stock-use-labels) | 17 passed |
| React typecheck | PASS |
| React lint | PASS (0 errors; existing warnings) |
| React build | PASS |
| React full suite | NOT RUN |
| POS API `/health` | Healthy (`:8092`, LocalValidation profile) |
| Conflict markers | 0 (real `<<<<<<<` markers) |
| `git diff --check` | clean |
| i18n wasteLoss key parity (en/fil/ceb/ilo/hil) | 71 keys each |

## Explicit deferrals

- Automatic expired-stock disposal
- Automatic production-waste generation
- Full accounting / expense ledger / profit dashboard rewrite
- Offline Waste/Loss mutation queue — ONLINE_ONLY
- Persisted `VoidReason` field (void uses actor/time + confirm UX; matches Stock Use)
- Expired-stock quick-flow subsystem — DEFERRED
