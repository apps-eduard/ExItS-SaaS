# P10-WP03 — Advanced Inventory

Phase marker: `P10-WP03-advanced-inventory`

## Status

**Complete.** Reorder configuration with audit trail, derived stock states, stock counts with variance movements, enhanced movement filters, reconciliation, API/MAUI surfaces, PostgreSQL migration, and tests. **P10-WP04 not started.**

## Delivered capability

| Area | Delivered |
|---|---|
| Reorder | `ReorderQuantity` on `InventoryAccount`; `InventoryReorderChange` audit; `SetReorderConfiguration`; PUT `/api/v1/pos/inventory/{productId}/reorder` |
| Stock states | Derived `InStock`, `LowStock`, `OutOfStock`, `ReorderSuggested`; GET `/reorder-suggestions` |
| Stock counts | `StockCount` Draft → InProgress → Completed; Draft/InProgress → Cancelled; `CNT-YYYYMMDD-NNNNNN` advisory lock |
| Count lines | `SystemOnHandSnapshot`, `CountedQuantity`, `Variance`; start/complete/cancel; idempotent complete |
| Movements | `StockCountVarianceIncrease` / `StockCountVarianceDecrease`; `SourceType` `StockCount`; unique index; no negative on-hand |
| Queries | Enhanced movement filters; GET reconciliation (on-hand vs movement sum) |
| Persistence | Migration `AddPosAdvancedInventory` after `EnrichPosGoodsReceiptFields` |
| API / client | `/api/v1/pos/inventory/...`; `PosInventoryClient` extended |
| MAUI | `/inventory/{id}/reorder`; `/inventory/counts` list/create/detail |
| Authorization | Reuses `ViewInventory` / `ManageInventory`; online-only |

## Explicit exclusions

Warehouses, branches, transfers, costing, valuation, batches, serials, expiry, purchase returns, automatic PO creation, demand forecasting, **P10-WP04+**.

## Build and test evidence

| Suite | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| Full `ExItS.slnx` Release (test projects; MAUI app build skipped — no Android SDK on agent) | **1073** | **0** | **0** |

Prior baseline: **1067 / 0 / 0** (post P10-WP02 gap-fix docs).

Release build of `ExItS.slnx` succeeds for all non-Android targets; Android SDK unavailable locally (R-109 unchanged).

## Portfolio independence

- No `HealthCare/` tree; `git ls-files -- HealthCare/` empty.
- No cross-product DB access.

## Exact next work package

**P10-WP04 — Cashier Shifts** — do **not** begin until explicitly authorized.
