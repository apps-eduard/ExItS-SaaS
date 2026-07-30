# P10-WP03 — Advanced Inventory

Phase marker: `P10-WP03-advanced-inventory`

## Status

**Complete.** Reorder configuration with audit trail, derived stock states, stock counts with variance movements, enhanced movement filters, reconciliation, API/MAUI surfaces, PostgreSQL migration, and tests. **P10-WP04 not started.**

Feature commit: `5c62133`  
Docs commit: `8af7a14`  
Gap-fix: `31d809c` (stock status alignment, CountDate, migration Down safety)

## Delivered capability

| Area | Delivered |
|---|---|
| Reorder | `ReorderQuantity` on `InventoryAccount`; `InventoryReorderChange` audit; `SetReorderConfiguration`; PUT `/api/v1/pos/inventory/{productId}/reorder` |
| Stock states | Primary `InStock` / `LowStock` / `OutOfStock`; separate `IsReorderSuggested` + `SuggestedOrderQuantity`; GET `/reorder-suggestions` |
| Stock counts | `StockCount` with `CountDate`; Draft → InProgress → Completed; Draft/InProgress → Cancelled; `CNT-YYYYMMDD-NNNNNN` advisory lock |
| Count lines | `SystemOnHandSnapshot`, `CountedQuantity`, `Variance`; start/complete/cancel; idempotent complete |
| Movements | `StockCountVarianceIncrease` / `StockCountVarianceDecrease`; `SourceType` `StockCount`; unique index; no negative on-hand |
| Queries | Enhanced movement filters; GET reconciliation (on-hand vs movement sum) |
| Persistence | Migrations `AddPosAdvancedInventory`, `EnrichPosStockCountDate` |
| API / client | `/api/v1/pos/inventory/...`; `PosInventoryClient` extended |
| MAUI | `/inventory/{id}/reorder`; `/inventory/counts` list/create/detail |
| Authorization | Reuses `ViewInventory` / `ManageInventory`; online-only |

## Explicit exclusions

Warehouses, branches, transfers, costing, valuation, batches, serials, expiry, purchase returns, automatic PO creation, demand forecasting, **P10-WP04+**.

## Build and test evidence

| Suite | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| Full `ExItS.slnx` Release (test projects) | **1079** | **0** | **0** |

Prior baseline: **1067 / 0 / 0** (post P10-WP02). Feature tip `5c62133` reported **1073**; gap-fix below raises suite to **1079**.

Release build of POS API succeeds; MAUI `net10.0-android` compiles after CreateStockCount named `Notes` fix. R-129 (NU1903) unchanged.

## Portfolio independence

- No `HealthCare/` tree; `git ls-files -- HealthCare/` empty.
- No cross-product DB access.

## Exact next work package

**P10-WP04 — Cashier Shifts** — do **not** begin until explicitly authorized.
