# POS-PRODUCT-PROFITABILITY-RANKING-01

## Summary

Per-product profitability ranking for Organization POS using immutable sale/return line snapshots. Completes the deferred `PRODUCT_PROFITABILITY_REPORT` from inventory cost/profit hardening.

| Field | Value |
|--------|--------|
| START_SHA | `d3ff54f16c1252c1f8dc18bf81d5e1e6aa2df7c2` |
| FEATURE_SHA | `1e08f2d08f12cbf4c47752e7c036e7367bc27478` |
| NET_SALES_BASIS | Σ completed `SaleLine.LineTotal` − period refunds (post-discount line money) |
| SALES_BEFORE_DISCOUNTS | Σ `GrossLineTotal` |
| COMMERCIAL_DISCOUNTS | Σ line `LineDiscountAmount + SaleDiscountAllocatedAmount` |
| KNOWN_COGS | Σ known `LineCostSnapshot` − return qty × original `UnitCostSnapshot` |
| COGS_STATUS | Complete / Partial / Unavailable (qty-weighted; unknown return cost → not Complete) |
| GROSS_PROFIT | NetSales − KnownCogs **only when Complete** |
| GROSS_MARGIN | GrossProfit / NetSales × 100 when NetSales > 0 and Complete |
| COST_COMPLETENESS | KnownCostQuantity / QuantitySold × 100 |
| VOID_POLICY | Completed sales only |
| RETURN_POLICY | Reduce Net Sales and COGS; never reconstruct cost from catalog |
| WASTE_STOCK_USE | Excluded (remain on period profitability only) |
| UTANG_POLICY | Same as Cash/ManualGCash once Completed |
| BRANCH_SCOPE | Optional `branchId`; omit = org-wide; invalid/cross-org fail closed |
| RANK_BY | `grossProfitDesc` (default), `grossProfitAsc`, `netSalesDesc`, `grossMarginDesc` |
| QUERY_MODEL | SQL group-by sale lines + return×sale-line cost join |
| N_PLUS_ONE | PASS |
| MIGRATION | N/A |
| PERMISSION | `ViewReports` (same as period profitability) |

## API

`GET /api/v1/pos/reports/product-profitability?fromDate&toDate&branchId&rankBy`

## React

`/reports/operational/product-profitability` — sortable table + rank selector; branch scope; i18n en/fil/ceb/ilo/hil.

## Explicit exclusions

FIFO / lot-layer COGS; GL; operating profit; expense allocation to products; Waste/Loss or Stock Use in product GP.

## NEXT

See gaps audit / owner backlog (Personal suite / Expenses CRUD / etc.).
