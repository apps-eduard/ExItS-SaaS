# POS-REPORTS-BRANCH-SCOPING-01

## Meta

| Field | Value |
|--------|--------|
| TASK | POS-REPORTS-BRANCH-SCOPING-01 |
| START_SHA | `07c305b72bd46b945f46cac22be6b012ded23b01` |
| BRANCH | `feat/organization` |
| MIGRATION | N/A |

---

## Contract

| Key | Value |
|-----|--------|
| CURRENT_BRANCH_SOURCE | React `boundWorkspace.branchId` → `X-Pos-Branch-Id` for operations; **reports use explicit query `branchId` only** |
| BRANCH_SWITCH_MODEL | Workspace bind / `/workspace` switcher; report “current” mode follows acting branch |
| BRANCH_REPORT_PERMISSION_MODEL | Same as ViewReports / ViewAdvancedReports + role matrix; no separate branch-report capability |
| ALL_BRANCH_REPORT_PERMISSION_MODEL | Owner / Manager / ReportingUser / org-management (no new RBAC invented) |
| REPORT_SCOPE_CONTRACT | Optional query `branchId`. Absent = organization-wide. Present = validated org branch then SQL filter. Never ignore invalid branch → org totals |
| BRANCH_HEADER_QUERY_CONFLICT_POLICY | Acting `X-Pos-Branch-Id` **ignored** for report aggregation; query `branchId` is sole report scope authority |

---

## Policies

| Key | Value |
|-----|--------|
| DASHBOARD_SCOPE_POLICY | MIXED_LABELED — sale metrics honor `branchId`; expenses / utang outstanding / low-stock remain organization-wide |
| MANAGEMENT_OVERVIEW_SCOPE_POLICY | ORGANIZATION_ONLY (labeled All branches) |
| SALES_REPORT_SCOPE | Branch / Organization (`Sale.BranchId`) |
| SALES_SUMMARY_SCOPE | Branch / Organization (sales + returns via original sale branch) |
| SALES_BY_PRODUCT_SCOPE | Branch / Organization |
| SALES_BY_CATEGORY_SCOPE | Branch / Organization (classic) |
| SALES_BY_PAYMENT_SCOPE | Branch / Organization |
| SALES_BY_CASHIER_SCOPE | Branch / Organization |
| RETURNS_SCOPE | Branch via original `Sale.BranchId` |
| PROFITABILITY_SCOPE | Branch / Organization (sale/return/waste/stock-use already branch-aware) |
| SHIFT_SUMMARY_SCOPE | ORGANIZATION + actor restriction (CashierShift has RegisterId, not BranchId) |
| CASH_VARIANCE_SCOPE | ORGANIZATION + actor restriction |
| WASTE_LOSS_BRANCH_TRUTH | `WasteLoss.BranchId` (used in profitability) |
| STOCK_USE_BRANCH_TRUTH | `StockUse.BranchId` (used in profitability) |
| PRODUCTION_BRANCH_TRUTH | `ProductionRun.BranchId` exists; no dedicated production report changed |
| BRANCH_INVENTORY_BALANCE_COMPLETENESS | INCOMPLETE (GRN / DirectBuy / StockCount do not update branch balances) |
| INVENTORY_STATUS_SCOPE_POLICY | ORGANIZATION_ONLY (honest; no fake branch stock) |
| INVENTORY_MOVEMENT_BRANCH_POLICY | Branch filter uses `StockMovement.BranchId` only; null BranchId rows appear in org view only |
| STOCK_COUNT_VARIANCE_SCOPE | ORGANIZATION |
| PURCHASING_BRANCH_MODEL | PO / GRN / DirectPurchase organization-level |
| PURCHASING_REPORT_SCOPE | ORGANIZATION_ONLY |
| EXPENSE_BRANCH_MODEL | No Expense.BranchId |
| EXPENSE_REPORT_SCOPE | ORGANIZATION_ONLY |
| UTANG_BRANCH_SCOPE_POLICY | Outstanding balances organization-level; do not fake branch debt |
| BRANCH_REPORT_QUERY_N_PLUS_ONE | PASS (single scoped queries) |
| DISCOUNT_REPORT_FIELDS | UNCHANGED |
| BACKEND_CHANGE_REQUIRED | YES |
| MIGRATION_REQUIRED | N/A |

---

## Report matrix

| Report | Before | After | Branch truth source | All branches allowed | Notes |
|--------|--------|-------|---------------------|----------------------|-------|
| Dashboard (sales) | Organization | Branch/Organization | Sale.BranchId | Yes (authorized) | Expenses/utang/low-stock still org |
| Management overview | Organization | Organization | n/a | n/a | Labeled org-wide |
| Classic sales | Organization | Branch/Organization | Sale.BranchId | Yes | Default current branch in React |
| Classic utang | Organization | Organization | ledger | n/a | Labeled |
| Classic inventory | Organization | Organization | OnHand | n/a | Labeled |
| Classic expenses | Organization | Organization | n/a | n/a | Labeled |
| Sales summary / overview | Organization | Branch/Organization | Sale + return→Sale | Yes | |
| Sales by payment/product/cashier | Organization | Branch/Organization | Sale.BranchId | Yes | |
| Returns | Organization | Branch/Organization | Sale.BranchId | Yes | |
| Profitability | Optional branchId | Hardened + validated | Sale/Waste/StockUse | Yes | |
| Inventory movements | Organization | Branch/Organization | StockMovement.BranchId | Yes | Unscoped rows org-only |
| Inventory status | Organization | Organization | OnHand | n/a | Incomplete branch balances |
| Stock count variance | Organization | Organization | StockCount org | n/a | No fake filter |
| Shifts / cash variance | Org + actor | Org + actor | Register | n/a | No BranchId |
| Purchasing / expenses summary / utang-by-product | Organization | Organization | n/a | n/a | Labeled |

---

## Security

- Invalid / empty `branchId` → fail closed (400 / not_found)
- Testing env: `ExistsInOrganization` accepts any non-empty GUID (same as transfers); still filters sales by BranchId within org — **no silent org-total fallback**
- Production: Platform branch directory membership required
