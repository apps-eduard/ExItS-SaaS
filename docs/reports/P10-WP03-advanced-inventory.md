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
| Stock counts | `StockCount` with `CountDate` and `Title`; Draft → InProgress → Completed; Draft/InProgress → Cancelled; new `CNT-YYYYMMDD-NN` advisory lock (historical 6-digit numbers remain valid) |
| Count lines | `SystemOnHandSnapshot`, `CountedQuantity`, `Variance`; start/complete/cancel; idempotent complete |
| Movements | `StockCountVarianceIncrease` / `StockCountVarianceDecrease`; `SourceType` `StockCount`; unique index; no negative on-hand |
| Queries | Enhanced movement filters; GET reconciliation (on-hand vs movement sum) |
| Persistence | Migrations `AddPosAdvancedInventory`, `EnrichPosStockCountDate`, `AddPosStockCountTitle` |
| API / client | `/api/v1/pos/inventory/...`; `PosInventoryClient` extended |
| MAUI | `/inventory/{id}/reorder`; `/inventory/counts` list/create/detail |
| Authorization | Reuses `ViewInventory` / `ManageInventory`; online-only |

## Explicit exclusions

Warehouses, costing, valuation, batches, serials, expiry, purchase returns, automatic PO creation, demand forecasting, **P10-WP04+**.

Later overlay (not part of this WP’s original delivery): intra-organization branch inventory transfers. See [pos-branch-inventory-transfers.md](../engineering/pos-branch-inventory-transfers.md). P10-WP03 still did not deliver warehouses or a POS branches table.

Later overlay (not part of this WP’s original delivery): optional per-product expiration lots. See [pos-expiration-aware-inventory.md](../engineering/pos-expiration-aware-inventory.md). Historical exclusions above remain accurate for the original WP.

## Stock Count UX refinement

Stock Count means checking what is physically present and correcting the system quantity to match. Completing a count still posts the existing variance movements; it is not a manual Adjust Stock flow.

| Topic | Behavior |
|---|---|
| Title | Required user-facing name for the count. Presets: Weekly, Monthly, Quarterly, Midyear, Year-end count, plus Custom. Custom cannot be blank. Max 80 characters after trim. |
| Notes | Separate optional field for extra context. Title answers “What count is this?”; Notes answers “Anything else we should know?” |
| Products | Multi-select checkboxes of tracked products only. Select all / Clear all apply to eligible tracked products. Duplicate products are rejected. Untracked catalog items do not appear until **Track stock** is enabled on Product Create/Edit (or Inventory enable). |
| Count reference | New counts allocate `CNT-YYYYMMDD-NN` server-side (organization + UTC business date, advisory lock, unique `(organization_id, count_number)`). Sequence expands past 99 (`-100`). Historical `CNT-YYYYMMDD-000001` values stay readable and are not rewritten. |
| Timestamps | Stored UTC unchanged. UI shows local time as `MMM d, yyyy · h:mm tt` (English AM/PM). |
| UI wording | Presentation only: Draft → Preparing, InProgress → Counting, On hand → System qty, Counted → Actual count, Variance → Difference, Complete count → Finish count, Count lines → Products. Domain enums stay unchanged. |
| Historical rows | Migration `AddPosStockCountTitle` backfills missing titles as `Stock count`. |

Create still saves a Draft internally; the primary button says **Create count**.

## Build and test evidence

| Suite | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| Full `ExItS.slnx` Release (test projects) | **1079** | **0** | **0** |

Prior baseline: **1067 / 0 / 0** (post P10-WP02). Feature tip `5c62133` reported **1073**; gap-fix below raises suite to **1079**.

Release build of POS API succeeds; MAUI `net10.0-android` compiles after CreateStockCount named `Notes` fix. R-129 (NU1903) unchanged.

## Portfolio independence

- No unauthorized nested product tree; No unauthorized nested product tree is tracked.
- No cross-product DB access.

## Exact next work package

**P10-WP04 — Cashier Shifts** — do **not** begin until explicitly authorized.
