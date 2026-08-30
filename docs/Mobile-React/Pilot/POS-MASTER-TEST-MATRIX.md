# POS Master Test Matrix

Companion to `POS-MASTER-VALIDATION-LEDGER.md`. Prefer **EVIDENCE_REUSED** over re-execution.

| Field | Value |
|-------|-------|
| **START_SHA** | `ad1f9171dbafc1e71c6ca94f2732dfda787b81ce` |
| **INVALIDATION_BASELINE** | Completion FEATURE_SHA `0abc15dadaed545a962d36819fd375a013772fff` — tip after is docs-only |

Columns: ID · DOMAIN · SCENARIO · TEST_TYPE · SOURCE_REPORT · SOURCE_SHA · LAST_PASS_SHA · CURRENT_CODE_TOUCHED · EVIDENCE_REUSED · RERUN_REQUIRED · RERUN_EXECUTED · RESULT · NOTES

---

## Identity / RBAC

| ID | DOMAIN | SCENARIO | TEST_TYPE | SOURCE_REPORT | SOURCE_SHA | LAST_PASS_SHA | CURRENT_CODE_TOUCHED | EVIDENCE_REUSED | RERUN_REQUIRED | RERUN_EXECUTED | RESULT | NOTES |
|----|--------|----------|-----------|---------------|------------|---------------|----------------------|-----------------|----------------|----------------|--------|-------|
| ID-001 | IDENTITY | Org create + Owner login | CONTROLLED_PILOT | COMPLETION-01 | `0abc15da` | `0abc15da` | NO | YES | NO | NO | PASS | Live smoke also OK at master start |
| RBAC-001 | RBAC | Live Cashier/Inv/Reporting | CONTROLLED_PILOT | COMPLETION SC19 | `0abc15da` | `0abc15da` | NO | YES | NO | NO | PASS | BUG_01/02 fixed |
| RBAC-002 | RBAC | Staff invite productRole | POSTGRES_INTEGRATION | COMPLETION BUG_01 | `0abc15da` | `0abc15da` | NO | YES | NO | NO | PASS | |
| RBAC-003 | RBAC | ReportingUser sales-summary | POSTGRES_INTEGRATION | COMPLETION BUG_02 | `0abc15da` | `0abc15da` | NO | YES | NO | NO | PASS | Entitlement-only advanced reports |

---

## Sell / Customer / Shift

| ID | DOMAIN | SCENARIO | TEST_TYPE | SOURCE_REPORT | SOURCE_SHA | LAST_PASS_SHA | CURRENT_CODE_TOUCHED | EVIDENCE_REUSED | RERUN_REQUIRED | RERUN_EXECUTED | RESULT | NOTES |
|----|--------|----------|-----------|---------------|------------|---------------|----------------------|-----------------|----------------|----------------|--------|-------|
| SELL-001 | SELL | Cash / ManualGCash / Utang | CONTROLLED_PILOT | CONTROLLED-01 SC02–06 | `54a25b0d` | `54a25b0d` | NO | YES | NO | NO | PASS | API pilot |
| SELL-002 | SELL | Shift open/close | CONTROLLED_PILOT | CONTROLLED-01 SC20 | `54a25b0d` | `54a25b0d` | NO | YES | NO | NO | PASS | |
| SELL-003 | SELL | Idempotency | CONTROLLED_PILOT | CONTROLLED-01 SC22 | `54a25b0d` | `54a25b0d` | NO | YES | NO | NO | PASS | |
| ROP-UI | SELL | Real-operator UI ROP_01–15 | MANUAL_UI | REAL-OPERATOR-01 | START_SHA | START_SHA | NO | NO | YES | YES | See ROP report | UI operator proxy |

---

## Inventory / Purchasing / Payables

| ID | DOMAIN | SCENARIO | TEST_TYPE | SOURCE_REPORT | SOURCE_SHA | LAST_PASS_SHA | CURRENT_CODE_TOUCHED | EVIDENCE_REUSED | RERUN_REQUIRED | RERUN_EXECUTED | RESULT | NOTES |
|----|--------|----------|-----------|---------------|------------|---------------|----------------------|-----------------|----------------|----------------|--------|-------|
| INV-001 | INVENTORY | Opening → Stock Use | POSTGRES_INTEGRATION | STOCK-USE-BRANCH-BALANCE-FIX-01 | `bc625cfb` | `34d6f093` | NO | YES | NO | NO | PASS | Model A EnsureBalance |
| INV-002 | INVENTORY | SC14 retest | CONTROLLED_PILOT | COMPLETION SC14 | `0abc15da` | `0abc15da` | NO | YES | NO | NO | PASS | |
| INV-003 | INVENTORY | StockUse/Waste/Prod Postgres | POSTGRES_INTEGRATION | INVENTORY-OPS-POSTGRES-01 | `722e9634` | `722e9634` | NO | YES | NO | NO | PASS | 12 facts |
| INV-004 | INVENTORY | Transfer A→B | POSTGRES_INTEGRATION | PosInventoryTransferApiTests | transfer pkg | transfer pkg | NO | YES | YES | YES | PASS | MB_04 re-proof |
| INV-005 | INVENTORY | Non-primary cannot spend unallocated | POSTGRES_INTEGRATION | PosStockUseApiTests | `bc625cfb` | `34d6f093` | NO | YES | YES | YES | PASS | MB_02/03 |
| PUR-001 | PURCHASING | DP / PO / GRN | CONTROLLED_PILOT | CONTROLLED-01 SC07–09 | `54a25b0d` | `54a25b0d` | NO | YES | NO | NO | PASS | |
| PUR-002 | PURCHASING | Receipt full void | POSTGRES_INTEGRATION | PURCHASE-RECEIPT-REVERSAL-01 | `3eb5a041` | `3eb5a041` | NO | YES | NO | NO | PASS | |
| PAY-001 | PAYABLES | Partial/full payment suite | POSTGRES_INTEGRATION | SUPPLIER-PAYABLES-01 | `c045ea25` | `8366a8ec` | NO | YES | NO | NO | PASS | 7/7; org-scoped |
| LINK-001 | PURCHASING | CreateBuyerProductAndLink | BACKEND_UNIT | CONNECTED-BUYER-CREATE-LINK-01 | `f0156f85` | `6ec10659` | NO | YES | NO | NO | PASS | Fixture repair |

---

## Costing / Orders / Reports

| ID | DOMAIN | SCENARIO | TEST_TYPE | SOURCE_REPORT | SOURCE_SHA | LAST_PASS_SHA | CURRENT_CODE_TOUCHED | EVIDENCE_REUSED | RERUN_REQUIRED | RERUN_EXECUTED | RESULT | NOTES |
|----|--------|----------|-----------|---------------|------------|---------------|----------------------|-----------------|----------------|----------------|--------|-------|
| COST-001 | COSTING | SaleCostProfit | BACKEND_UNIT | INVENTORY-COST-PROFIT-01 | pkg | pkg | NO | YES | NO | NO | PASS | |
| COST-002 | COSTING | Discount reporting | BACKEND_UNIT | DISCOUNT-REPORTING-01 | `f9985d13` | `f9985d13` | NO | YES | NO | NO | PASS | |
| COST-003 | COSTING | Profitability ranking | REACT_TEST | PRODUCT-PROFITABILITY-01 | `00fc8ac9` | `00fc8ac9` | NO | YES | NO | NO | PASS | |
| COGS-001 | ORDERS | Customer-order settlement COGS | BACKEND_UNIT | CUSTOMER-ORDER-COGS-01 | `ea9334be` | `ea9334be` | NO | YES | NO | NO | PASS | |
| RPT-001 | REPORTING | Operational + CSV | CONTROLLED_PILOT | CONTROLLED SC17–18; REPORT-EXPORT | `753f5f81` | `0abc15da` | NO | YES | NO | NO | PASS | |
| RPT-002 | REPORTING | Branch scope badges | REACT_TEST | DASHBOARD-BRANCH-CLARITY | `7060521f` | `7060521f` | NO | YES | NO | NO | PASS | |

---

## UX / i18n / Build

| ID | DOMAIN | SCENARIO | TEST_TYPE | SOURCE_REPORT | SOURCE_SHA | LAST_PASS_SHA | CURRENT_CODE_TOUCHED | EVIDENCE_REUSED | RERUN_REQUIRED | RERUN_EXECUTED | RESULT | NOTES |
|----|--------|----------|-----------|---------------|------------|---------------|----------------------|-----------------|----------------|----------------|--------|-------|
| UI-RESP-001 | UX | Sell + manager 360/768/1440 | MANUAL_UI | COMPLETION SC21 | `0abc15da` | `0abc15da` | NO | YES | NO | NO | PASS | Playwright 4/4 |
| I18N-001 | I18N | Locale parity 5 locales | REACT_TEST | I18N-LOCALE-PARITY-02 | `dd775a97` | `dd775a97` | NO | YES | NO | NO | PASS | |
| REACT-001 | REACT | Full vitest 1344 | BUILD_VALIDATION | COMPLETION-01 | `0abc15da` | `0abc15da` | NO | YES | NO | NO | PASS | Do not re-run docs-only |
| MB-MAP | MULTI | MB_01–MB_14 | POSTGRES_INTEGRATION | MULTI-BRANCH-HARDENING-01 | START_SHA | START_SHA | NO | NO | YES | YES | See MB report | Targeted re-proof |

---

## Regression invalidation examples

| Change | Invalidate |
|--------|------------|
| Inventory mutation / BranchBalanceMutation | INV-001–005, SELL stock paths, MB stock |
| Permission policy / ReportingEndpoints auth | RBAC-*, RPT operational |
| Payables schema/service | PAY-001 |
| Docs / SHA stamp only | Nothing |
| i18n copy only | I18N-001 + affected React; not Postgres purchasing |
| Report CSV utility only | RPT CSV; not inventory mutations |
