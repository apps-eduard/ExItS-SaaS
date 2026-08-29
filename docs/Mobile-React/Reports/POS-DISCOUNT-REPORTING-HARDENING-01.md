# POS-DISCOUNT-REPORTING-HARDENING-01

## Summary

Harden Organization POS **sales reporting** so commercial discount snapshots already stored on Sale/SaleLine are aggregated and labeled truthfully. No new discount engine. No COGS/inventory/Utang/expense side effects.

| Field | Value |
|--------|--------|
| START_SHA | `b78668b14da843fc83dc4082b58bc8176ed0a831` |
| FEATURE_SHA | `f9985d136f7bbe6e398ceea90e38c13e13893178` |
| DISCOUNT_MODEL | Manual commercial discount (Line + Sale scope; % / Fixed) |
| DISCOUNT_SOURCE_OF_TRUTH | Immutable Sale/SaleLine checkout snapshots |
| DISCOUNT_SNAPSHOT_MODEL | `GrossSubtotal` / `DiscountTotal` / `Subtotal` / `TaxAmount` / `Total`; line `GrossLineTotal` / `TotalLineDiscount` / `LineTotal` |
| SALE_GROSS_SUBTOTAL_MEANING | Σ GrossLineTotal — pre–commercial-discount merchandise |
| SALE_NET_SUBTOTAL_MEANING | Σ LineTotal — post-discount pre-tax (`Subtotal`) |
| SALE_TAX_MEANING | Snapshotted `TaxAmount` on post-discount tax base |
| SALE_TOTAL_MEANING | Customer payable/receivable after discount (+ tax when exclusive) |
| SALE_DISCOUNT_TOTAL_MEANING | `LineDiscountTotal + SaleDiscountTotal` (rounded) |
| LINE_GROSS_TOTAL_MEANING | UnitPrice × qty before commercial discount |
| LINE_DISCOUNT_TOTAL_MEANING | Line + allocated sale-level commercial discount |
| LINE_NET_TOTAL_MEANING | `GrossLineTotal − TotalLineDiscount` (refund basis) |
| CURRENT_REPORT_GROSS_SOURCE | Was `Sale.Total` / `LineTotal` under “Gross” labels |
| CURRENT_REPORT_NET_SOURCE | Was `CompletedTotal − VoidedTotal − Refunds` (void double-subtract) |
| CURRENT_REPORT_DISCOUNT_SOURCE | Missing (React placeholder) |
| CURRENT_GROSS_LABEL_TRUTHFUL_BEFORE | NO |
| REPORT_GROSS_DEFINITION | **Sales before discounts** = Σ Completed.`GrossSubtotal` |
| REPORT_DISCOUNT_DEFINITION | **Commercial discounts** = Σ Completed.`DiscountTotal` (sale period; not reduced by later returns) |
| REPORT_NET_SUBTOTAL_DEFINITION | Σ Completed.`Subtotal` |
| REPORT_TAX_DEFINITION | Σ Completed.`TaxAmount` |
| REPORT_SALE_TOTAL_DEFINITION | **Completed sales** = Σ Completed.`Total` (legacy `CompletedGrossSales` / `CompletedSalesTotal` kept) |
| REPORT_NET_SALES_DEFINITION | `CompletedTotal − Refunds` (voided already excluded from Completed) |
| GROSS_FIELD_COMPATIBILITY_POLICY | KEEP_POST_DISCOUNT_TOTAL_AS_LEGACY_GROSS + ADD_PREDISCOUNT_FIELDS; UI stops calling legacy field “Gross” |
| RETURN_DISCOUNT_REPORT_POLICY | Period discount = discounts on completed sales in range; returns reported separately |
| VOID_REPORT_MODEL | EXCLUDE_VOIDED_FROM_COMPLETED_AND_NET; `VoidedSales` informational |
| VOIDED_DISCOUNT_POLICY | Voided discounts excluded from `CommercialDiscountTotal` (`VoidedDiscountTotal` aggregated for audit only) |
| LEGACY_DISCOUNT_POLICY | Rehydrate: missing gross → subtotal; discount totals → 0; no reconstruction |
| PROFITABILITY_DISCOUNT_TREATMENT | NetSales already post-discount; COGS unchanged; expose `CommercialDiscountTotal` |
| DISCOUNT_DOUBLE_SUBTRACTION_GUARD | PASS |
| BRANCH_SCOPE | Existing report `branchId` query |
| ORGANIZATION_SCOPE | All branches when branch omitted |
| DATE_RANGE_MODEL | Existing `ReportDateRange` |
| DISCOUNT_REPORT_QUERY_MODEL | SQL header aggregates on sale columns; product/category from loaded report lines (existing pattern) |
| DISCOUNT_REPORT_N_PLUS_ONE | PASS (no per-sale discount query) |
| BACKEND_CHANGE_REQUIRED | YES |
| MIGRATION_REQUIRED | N/A |
| DASHBOARD_DISCOUNT | DEFERRED (dashboard keeps Completed sales card; detail in Sales reports) |

## Delivered

- `SalePeriodAggregate` / `SalePaymentAggregate` discount + pre-discount totals
- Additive DTO fields on overview, sales-summary, classic sales, dashboard, payment, product, category, cashier, profitability
- NetSales formula corrected (no void double-subtract; no discount re-subtract)
- React schemas + operational/classic labels + i18n (en/fil/ceb/ilo/hil)
- Gaps audit: discount period totals marked IMPLEMENTED

## Validation

| Check | Result |
|--------|--------|
| Backend DiscountReporting + SaleCostProfit + AggregateEquivalence | 20 passed |
| React discount-reporting + rmap-20 locale keys | 14 passed |
| React typecheck | PASS |
| React lint | PASS (0 errors; pre-existing warnings) |
| React build | PASS |
| React full suite | TOTAL=1255 PASS=1181 FAIL=74 |
| DISCOUNT_REPORT_RELATED_FAILURES | 0 |
| OTHER_ORGANIZATION_FAILURES | 0 |
| Unrelated FAIL delta vs prior baseline (70) | +4 (personal/platform/session/inventory/qr/connectivity/workspace; not discount) |
| MIGRATION | N/A |
| CONFLICT_MARKERS | 0 (CSS section rules only) |
| NEW_TEST_SKIPS / ONLY | 0 |

## NEXT

`POS-PRODUCT-PROFITABILITY-RANKING-01` (completed — see that report).
