# POS-INVENTORY-COST-PROFIT-HARDENING-01

## Summary

Harden authoritative inventory cost resolution and immutable **Sale COGS** / **Gross Profit** reporting. Stock Use and Waste/Loss remain separate operational costs — never folded into Sales COGS. No general ledger.

| Field | Value |
|--------|--------|
| EXISTING_INVENTORY_COST_MODEL | `StockMovement.UnitCost` on acquisition inflows (Opening/PO/DirectBuy/ProductionOutput) |
| EXISTING_COST_RESOLVER | `GetLatestAcquisitionUnitCostAsync` (+ batch `GetLatestAcquisitionUnitCostsAsync` / `InventoryCostResolver`) |
| EXISTING_SALE_COST_SNAPSHOT | NONE before this package → added |
| EXISTING_SALE_COGS_MODEL | NONE → Sale/SaleLine snapshots |
| EXISTING_GROSS_PROFIT_MODEL | NONE → `SaleProfitability` + profitability report |
| EXISTING_INVENTORY_VALUATION_MODEL | Estimated via last UnitCost × OnHand (label: Estimated stock value) — not formal GL valuation |
| INVENTORY_COST_RESOLUTION_POLICY | Last authoritative acquisition UnitCost (Opening/PurchaseReceipt/DirectPurchaseReceipt/ProductionOutput); never SellingPrice; never ManualIncrease without stored cost |
| COST_SOURCE_TRACEABILITY | Movement UnitCost + SaleLine snapshots; lot UnitCost not stored |
| PRODUCED_INVENTORY_LAYER_ACCURACY | LAST_AUTHORITATIVE (ProductionOutput UnitCost); not exact multi-run lot-layer FIFO |
| SALE_COST_SNAPSHOT_MODEL | `SaleLine.UnitCostSnapshot` / `LineCostSnapshot`; `Sale.CostStatus` / `TotalCostSnapshot` |
| SALE_COGS_MODEL | Sum of immutable line cost snapshots at checkout |
| SALE_COST_STATUS_MODEL | Reuses `ProductionCostStatus` Complete/Partial/Unavailable |
| NET_SALES_SOURCE_OF_TRUTH | Existing report NetSales = completed `Sale.Total` − voids − refunds; sale-level GP uses `Sale.Total` |
| GROSS_PROFIT_FORMULA | NetSales − COGS (Complete only) |
| GROSS_MARGIN_FORMULA | GrossProfit / NetSales × 100 (NetSales > 0) |
| LEGACY_SALE_COST_POLICY | Null cost_status → Unavailable; **no speculative backfill** |
| SALE_VOID_COGS_POLICY | EXCLUDE_VOIDED_FROM_ACTIVE_AGGREGATES; snapshots retained |
| RETURN_REFUND_COGS_INTEGRATION | PASS (period profitability subtracts return qty × original SaleLine UnitCostSnapshot when known) |
| PRODUCTION_COST_TO_COGS | Via ProductionOutput UnitCost as inventory acquisition source at sale time |
| WASTE_COST_INTEGRATION | Separate period Waste/Loss known cost (Posted only) |
| STOCK_USE_COST_INTEGRATION | Separate period Stock Use known cost (Posted only) |
| INVENTORY_VALUATION_ACCURACY | ESTIMATED (last acquisition UnitCost) |
| INVENTORY_VALUATION_LABEL | Estimated stock value |
| COST_VISIBILITY_PERMISSION | `ViewReports` (sale summary internal section) |
| PROFIT_VISIBILITY_PERMISSION | `ViewReports` (`/reports/operational/profitability`) |
| PRODUCT_PROFITABILITY_REPORT | IMPLEMENTED (POS-PRODUCT-PROFITABILITY-RANKING-01) |
| UTANG_PROFIT_REPORT_POLICY | Utang Completed sales count in NetSales/COGS like Cash/GCash (receivables not redesigned) |
| COST_QUERY_N_PLUS_ONE | PASS (batch `GetLatestAcquisitionUnitCostsAsync`) |
| REPORT_QUERY_N_PLUS_ONE | PASS (header aggregates + return/waste/stock-use projections) |
| COST_OFFLINE_AUTHORITY | SERVER |
| GENERAL_LEDGER_INTEGRATION | DEFERRED |
| OPERATING_PROFIT | DEFERRED |
| FULL_ACCOUNTING | DEFERRED |
| SELLING_PRICE_USED_AS_COST | NO |

## API

- Sale DTO (organization): optional `costStatus`, `totalCostSnapshot`, `grossProfit`, `grossMarginPercent`, line cost snapshots
- `GET /api/v1/pos/reports/profitability` — ViewReports

## React

- `/reports/operational/profitability`
- Transaction summary internal cost/profit when `canViewReports`
- Customer/personal receipts unchanged (no cost fields)

## Migration

`20260829200000_AddPosSaleCostSnapshots`

`MIGRATION_APPLIED_LOCAL=YES` (Local Validation `exits_pos` @ 15534).

## Validation (this package)

| Check | Result |
|--------|--------|
| Backend SaleCostProfit tests | 15 passed |
| Backend related inventory/sale filter | 82 passed |
| React targeted (reports + sales client) | 26 passed |
| React typecheck / lint / build | PASS (lint: 0 errors, existing warnings) |
| React full suite | 1096 passed / 88 failed / 1184 total (pre-existing personal/platform harness failures; not introduced here) |
| POS API `/health` | Healthy (`:8092`) |
| Conflict markers | 0 |
| `git diff --check` | clean |

## Explicit deferrals

- Exact lot-layer / FIFO multi-source COGS (lots lack UnitCost)
- Product profitability ranking report → **IMPLEMENTED** (`POS-PRODUCT-PROFITABILITY-RANKING-01`)
- Formal accounting inventory valuation / GL
- Operating profit / expense allocation
- Client offline cost authority
