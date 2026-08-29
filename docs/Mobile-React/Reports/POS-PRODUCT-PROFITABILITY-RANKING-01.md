# POS-PRODUCT-PROFITABILITY-RANKING-01

## Summary

Authoritative **product profitability ranking** from immutable Sale/SaleLine commercial-discount and cost snapshots. Not a new costing engine; not FIFO; not GL.

| Field | Value |
|--------|--------|
| TASK | POS-PRODUCT-PROFITABILITY-RANKING-01 |
| START_SHA | `d3ff54f16c1252c1f8dc18bf81d5e1e6aa2df7c2` |
| FEATURE_SHA | `1e08f2d08f12cbf4c47752e7c036e7367bc27478` |
| HARDEN_SHA | `` |
| EXISTING_COST_MODEL | SaleLine UnitCostSnapshot / LineCostSnapshot; Sale CostStatus |
| EXISTING_DISCOUNT_MODEL | SaleLine GrossLineTotal / TotalLineDiscount / LineTotal |
| PRODUCT_PROFITABILITY_SOURCE_OF_TRUTH | Completed sale + return line snapshots (SQL aggregates) |
| PRODUCT_NET_SALES_FORMULA | Σ LineTotal − product RefundAmount (merchandise; no invented tax allocation) |
| PRODUCT_COGS_FORMULA | Σ known LineCostSnapshot − return qty × original UnitCostSnapshot |
| PRODUCT_GP_FORMULA | NetSales − TotalCogs **only when CogsStatus=Complete** |
| PRODUCT_MARGIN_FORMULA | GrossProfit / NetSales × 100 when NetSales > 0 and Complete |
| RETURN_POLICY | Period returns reduce NetSales + COGS via original sold-line cost |
| VOID_POLICY | Completed sales only |
| UTANG_POLICY | Completed Utang same as Cash/ManualGCash |
| PRICE_OVERRIDE_POLICY | Not commercial discount |
| LEGACY_COST_POLICY | No reconstruction from current acquisition cost |
| COMPLETE_COST_BEHAVIOR | TotalCogs + GrossProfit + Margin populated |
| PARTIAL_COST_BEHAVIOR | KnownCogs shown; TotalCogs/GP/Margin null |
| UNAVAILABLE_COST_BEHAVIOR | KnownCogs 0; GP/Margin null; ranked after complete for GP sorts |
| DEFAULT_RANKING | grossProfitDesc (null GP last) |
| BRANCH_SCOPE | Optional branchId; omit = org-wide |
| ALL_BRANCHES_SCOPE | Omit branchId |
| CROSS_ORG_GUARD | ValidateReportBranchAsync fail closed |
| QUERY_MODEL | SQL group-by sale lines + return×sale-line cost join |
| N_PLUS_ONE_STATUS | PASS |
| MOBILE_UX | Summary cards + per-product cards |
| TABLET_UX | Summary grid + cards / table breakpoint |
| DESKTOP_UX | Summary + sortable table |
| I18N_PARITY | en / fil-PH / ceb-PH / ilo-PH / hil-PH |
| BACKEND_TESTS | 24 passed (ProductProfitability + Discount + SaleCostProfit) |
| REACT_TARGETED_TESTS | 12 passed |
| REACT_FULL_SUITE | TOTAL=1256 PASS=1182 FAIL=74 |
| PRODUCT_PROFITABILITY_RELATED_FAILURES | 0 |
| TYPECHECK | PASS |
| LINT | PASS (0 errors) |
| BUILD | PASS |
| MIGRATION | N/A |
| CONFLICT_MARKERS | 0 |
| PERMISSION | ViewReports |

## API

`GET /api/v1/pos/reports/product-profitability?fromDate&toDate&branchId&rankBy`

## React

`/reports/operational/product-profitability`

## Explicit exclusions

FIFO; GL; operating profit; expense allocation; Waste/Stock Use in product GP; session harness repair.

## NEXT

`POS-REACT-TEST-HARNESS-ORG-SESSION-REPAIR-01`
