# Reports

## CURRENT operational reports

API prefixes: `/api/v1/pos/dashboard`, `/api/v1/pos/management/overview`, `/api/v1/pos/reports/*`

| Area | Status | Notes |
|------|--------|-------|
| Sales aggregates | PROVEN_CURRENT | by product/category/cashier/payment |
| Inventory | PROVEN_CURRENT | |
| Purchasing / supplier | PROVEN_CURRENT | |
| Shifts / cash variance | PROVEN_CURRENT | |
| Returns | PROVEN_CURRENT | |
| Expenses | PROVEN_CURRENT | |
| Business Utang / product utang | PROVEN_CURRENT | |
| Stock-count variance | PROVEN_CURRENT | |
| Connected supplier reports | PROVEN_PARTIAL | capability exists in commerce; treat report depth as verify-on-use |
| Customer ordering reports | PROVEN_PARTIAL | operational lists exist; dedicated analytics depth varies |
| Fake P&L / COGS / valuation | PROVEN_MISSING | Do not claim |

Offline: **OnlineRequired**.

Tests: `ManagementOverviewQueryServiceTests`, `SaleReportAggregateEquivalenceTests`, `ReportDateRangeTests`.

## MAUI / React

MAUI: `/reports*`, `/dashboard`.
React: **COMPLETE** (RMAP-20) — management overview + dashboard + operational/classic reports; Tax UI **NO**; Fake P&L **NO**; buyer purchase projection **NO**.
