# POS-DASHBOARD-REPORT-BRANCH-CLARITY-01

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**TASK:** POS-DASHBOARD-REPORT-BRANCH-CLARITY-01  
**START_SHA:** `ef2d10ad55ede5d138ed0d397f851c9dd351cb32`  
**FEATURE_SHA:** `7060521fbaea55fb84d8303b6c597d97896c4123`

## ROOT_CAUSE

The Organization dashboard mixed **branch-scoped sale metrics** (period panel, honoring `branchId` query) with **organization-wide metrics** (today overview, utang, expenses, low stock) in a single undifferentiated card grid. Operators with a branch selected could reasonably assume every card reflected that branch.

Backend semantics were already honest (`DashboardQueryService` comments and queries); the gap was **UI scope ambiguity**, not incorrect aggregation.

## DASHBOARD_SCOPE_MODEL

Two explicit scopes only:

| Scope | Meaning |
|-------|---------|
| **BRANCH** | Metric honors dashboard period branch filter (`branchId` on sale aggregates). |
| **ORGANIZATION** | Metric is organization-wide; branch filter does not apply. |

UI expresses scope via text badges (`Organization-wide`, `Branch: {name}`, `All branches`) and grouped sections — never color-only.

## METRIC SCOPE TABLE (pre-change audit)

| METRIC | CURRENT_SCOPE | BRANCH_FILTER_SUPPORTED | SOURCE | SHOULD_CHANGE | UI_LABEL_REQUIRED |
|--------|-----------------|-------------------------|--------|---------------|-------------------|
| Today sales | ORGANIZATION | No | `ManagementOverviewReadStore` | No (label only) | Yes |
| Today cash/utang | ORGANIZATION | No | same | No | Yes |
| Payments received | ORGANIZATION | No | same | No | Yes |
| Open utang | ORGANIZATION | No | same | No | Yes |
| Low stock (today) | ORGANIZATION | No | `InventoryAccount` org-level | No | Yes |
| Expired/near-expiry lots | ORGANIZATION | No | org-level lots | No | Yes |
| Open shifts/registers | ORGANIZATION | No | org-level counts | No | Yes |
| Completed sales (period) | BRANCH | Yes | `DashboardQueryService` + `branchId` | No | Yes |
| Cash/GCash/utang sales (period) | BRANCH | Yes | same | No | Yes |
| Voided sales (period) | BRANCH | Yes | same | No | Yes |
| Sales by day chart | BRANCH | Yes | same | No | Yes |
| Payment breakdown | BRANCH | Yes | same | No | Yes |
| Expenses (period) | ORGANIZATION | No | no `Expense.BranchId` | No | Yes |
| Utang outstanding/overdue (period) | ORGANIZATION | No | org credit ledger | No | Yes |
| Low stock (period) | ORGANIZATION | No | org inventory accounts | No | Yes |

## BRANCH_SCOPED_METRICS

- Completed sales, transaction count, cash/GCash/utang sales, voided sales
- Sales-by-day chart, payment breakdown, period comparison trend

## ORGANIZATION_SCOPED_METRICS

- Entire today overview panel (all cards)
- Period: expenses, utang outstanding, overdue utang, low stock

## BACKEND_SCOPE_CHANGE_REQUIRED

**NO** — existing queries unchanged. No fake branch filtering added.

## MIGRATION

**N/A**

## DASHBOARD_QUERY_MODEL

Unchanged aggregated model:

- `GET /api/v1/pos/management/overview` — org-wide snapshot (no `branchId`)
- `GET /api/v1/pos/dashboard?fromDate&toDate&branchId?` — mixed DTO; sales honor optional `branchId`; expenses/utang/low-stock remain org-wide in service

Two TanStack Query keys (unchanged count):

- `["management-overview", organizationId]`
- `["pos-dashboard", organizationId, reportBranchId ?? "all", fromDate, toDate]`

## QUERY_KEY_BRANCH_SAFETY

Branch identity included in dashboard period query key. Overview query intentionally excludes branch (org-only). Branch switch refetches period sales only.

## N_PLUS_ONE

**NO** — no additional API calls; reuses existing overview + dashboard queries.

## BRANCH_SWITCH_BEHAVIOR

Switching branch scope selection updates branch performance cards and refetches dashboard with new `branchId`. Organization overview cards keep org-wide labels and values.

## SINGLE_BRANCH_BEHAVIOR

Scope badges still shown (`Branch: {name}` vs `Organization-wide`); no extra complexity beyond honest labels.

## MULTI_BRANCH_BEHAVIOR

Grouped **Branch performance** and **Organization overview** sections; filter note explains branch filter applies to sales only.

## REPORT_SCOPE_AUDIT

Existing report pages already use `ReportScopeControls` with `reportScopeModeForClassic` / `reportScopeModeForOperational` and org-only notes. No report redesign required.

| Report | Scope mode | Status |
|--------|------------|--------|
| sales-summary, profitability, product-profitability, sales-by-* | branch | OK |
| utang, inventory, expenses (classic) | organization_only | OK |
| shifts, cash-variance, inventory-status, expenses-summary, purchasing-* | organization_only | OK |

## REPORT_SCOPE_CHANGES

**None** — dashboard-only UX clarity package.

## RESPONSIVE_UX

Scope badges use compact text; mobile media query allows badge wrap without clipping card titles.

## I18N_KEYS_ADDED

All five locales (`en`, `fil-PH`, `ceb-PH`, `hil-PH`, `ilo-PH`):

- `dashboard.scope.branch`
- `dashboard.scope.organization`
- `dashboard.scope.allBranches`
- `dashboard.scope.branchNamed`
- `dashboard.section.branchPerformance`
- `dashboard.section.organizationOverview`
- `dashboard.scope.filterNote`
- `dashboard.scope.periodOrgNote`
- Updated `dashboard.lede`

## PRODUCTION_GUARDS_WEAKENED

**NO** — organization/branch scoping, RBAC, and fail-closed workspace rules unchanged.

## REACT_TARGETED_TESTS

- `dashboard-scope.test.ts` (6)
- `ManagementDashboardPage.test.tsx` (5): org-wide labels, branch labels, branch switch refetch, org section persistence, grouped sections

## REACT_FULL_SUITE

| Metric | Value |
|--------|-------|
| TOTAL | 1267 |
| PASS | 1267 |
| FAIL | 0 |

## TYPECHECK / LINT / BUILD

| Check | Result |
|-------|--------|
| TYPECHECK | PASS |
| LINT | PASS (pre-existing warnings only) |
| BUILD | PASS |

## NEXT

**POS-I18N-LOCALE-PARITY-02** — PH locale movement-label mojibake repair (explicitly out of scope here).
