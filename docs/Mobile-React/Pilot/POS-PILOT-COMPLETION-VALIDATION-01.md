# POS-PILOT-COMPLETION-VALIDATION-01

Controlled completion of remaining single-branch pilot gaps after Stock Use remediation.

| Field | Value |
|-------|-------|
| **TASK** | POS-PILOT-COMPLETION-VALIDATION-01 |
| **START_SHA** | `6962fcd42cb03cf10a61c25edbfa842aa21b7206` |
| **FEATURE_SHA** | `0abc15dadaed545a962d36819fd375a013772fff` |
| **FINAL_SHA** | `0abc15dadaed545a962d36819fd375a013772fff` |
| **REMOTE_SHA** | _(after push)_ |
| **PILOT_BASELINE_SOURCE** | `docs/Mobile-React/Pilot/POS-CONTROLLED-SINGLE-BRANCH-PILOT-01.md` |
| **ORIGINAL_PILOT_RESULT** | NO (`PILOT_PASS=NO`; SCENARIO_14 FAIL; 19/21 NOT_RUN) |
| **COMPLETION_VALIDATION_RESULT** | YES |
| **TECHNICAL_PILOT_STATUS** | PASSED_AFTER_REMEDIATION |

Original pilot history is **not** rewritten. That document remains `PILOT_PASS=NO`.

---

## Scenario 14 — Stock Use fix retest

| Field | Value |
|-------|-------|
| **SCENARIO_14_STATUS** | PASS |
| **SCENARIO_14_OPENING_STOCK_RESULT** | PASS — Product A opening 20 → Stock Use 5 → on-hand **15**; HTTP 201 |
| **SCENARIO_14_DIRECT_PURCHASE_RESULT** | PASS — Product B DP qty 10 → Stock Use 3 → on-hand **7** |
| **SCENARIO_14_REGRESSION_RESULT** | PASS — Waste/Loss then Cash sale still succeed after EnsureBalance wiring |

Evidence: live POS API against controlled org (recreated after LocalValidation Platform purge). Harness log local `.tmp-pilot-completion-results.json` (not committed).

---

## Scenario 19 — Live role / permission validation

| Field | Value |
|-------|-------|
| **SCENARIO_19_STATUS** | PASS |

Invited real Organization staff via Platform invitations (productRole Cashier / Viewer / Manager→POS InventoryStaff assignment). Auth context confirmed per role (separate `@ORG######` logins; not Owner token).

### Cashier

| Field | Value |
|-------|-------|
| **CASHIER_ALLOWED_STATUS** | PASS — operational-setup, catalog, open shift, Cash sale, Manual GCash |
| **CASHIER_DENIED_STATUS** | PASS — suppliers, purchasing, stock adjust/use/waste/count, RBAC, sales-summary report → **403** |
| **CASHIER_UI_GATES** | PASS — deep-link deny validated in Playwright session (inventory/purchasing/staff invite); Sell reachable |
| **CASHIER_API_GATES** | PASS |

Customer list GET returned 403 for Cashier (matrix has CreateCredit without ViewCustomers); sell path still validated.

### InventoryStaff

| Field | Value |
|-------|-------|
| **INVENTORY_STAFF_ALLOWED_STATUS** | PASS — inventory view, Direct Purchase, Stock Use, Waste, Stock Count, inventory-status report |
| **INVENTORY_STAFF_DENIED_STATUS** | PASS — sale, RBAC → **403** |
| **INVENTORY_STAFF_UI_GATES** | PARTIAL — API authoritative; Manager-like nav covered under responsive Owner/Manager session |
| **INVENTORY_STAFF_API_GATES** | PASS |

Platform product-local roles are Owner/Manager/Cashier/Viewer only; InventoryStaff is assigned via POS `POST /api/v1/pos/permissions/assignments` after invite (authoritative POS role DB).

### ReportingUser

| Field | Value |
|-------|-------|
| **REPORTING_USER_READ_STATUS** | PASS — sales-summary, inventory-status, purchasing-summary, supplier-payables |
| **REPORTING_USER_MUTATION_DENIAL_STATUS** | PASS — sale, DP, stock use/waste/count/adjust, RBAC → **403** (supplier payment 404/403) |
| **REPORTING_USER_UI_GATES** | PARTIAL — API mutation denials primary; Cashier UI gates cover non-report deep links |
| **REPORTING_USER_API_GATES** | PASS |

Safe role refs (no secrets): Cashier / InventoryStaff / ReportingUser userIds recorded in local harness JSON only.

---

## Scenario 21 — Responsive UI

| Field | Value |
|-------|-------|
| **SCENARIO_21_STATUS** | PASS |
| **RESPONSIVE_360_STATUS** | PASS — manager page tour + Sell cart primitives @360 |
| **RESPONSIVE_768_STATUS** | PASS |
| **RESPONSIVE_1440_STATUS** | PASS |
| **PAGES_WITH_RESPONSIVE_ISSUES** | _(none blocking)_ |

Playwright: `e2e/pilot-completion-validation.spec.ts` — overflow ≤1px across Sell, Products, Inventory, Direct Purchase, POs, Receive, Payables, Stock Count/Use, Waste, Reports, Shifts, Customers, More.

---

## Bugs found and fixed in this package

| Metric | Count |
|--------|------:|
| **BUGS_FOUND_COUNT** | 2 |
| **BUGS_FIXED_COUNT** | 2 |
| **P0_COUNT** | 0 |
| **P1_COUNT** | 2 |
| **P2_COUNT** | 0 |
| **P1_UNRESOLVED_COUNT** | 0 |

### BUG_01 — Staff invitations dropped `productRole`

| Field | Value |
|-------|-------|
| **BUG_ID** | BUG_01 |
| **SEVERITY** | P1 |
| **SCENARIO** | 19 |
| **CLASS** | PERMISSION_BUG |
| **REPRODUCTION** | `POST /api/v1/organizations/{org}/staff-invitations` with `productRole=Cashier` → response `productRole=null`; accept → `product_assignment_missing` on POS token |
| **ROOT_CAUSE** | Endpoint called `CreateOrganizationInvitation` without forwarding `ProductRole` / profile fields (platform `/invitations` path was correct) |
| **FILES_CHANGED** | `BusinessCustomerEndpoints.cs`; test `Staff_invitation_persists_product_role_on_create_wire` |
| **FIX** | Pass full invitation body including `ProductRole` into use case |
| **TEST_ADDED** | YES |
| **ORIGINAL_STATUS** | FAIL |
| **RETEST_STATUS** | PASS (live invite + integration) |

### BUG_02 — Operational reports required role capability `ViewAdvancedReports`

| Field | Value |
|-------|-------|
| **BUG_ID** | BUG_02 |
| **SEVERITY** | P1 |
| **SCENARIO** | 19 |
| **CLASS** | PERMISSION_BUG |
| **REPRODUCTION** | ReportingUser / InventoryStaff with Growth advanced-reports entitlement → `GET .../reports/sales-summary` or `inventory-status` → **403** Role denied |
| **ROOT_CAUSE** | `TryAuthorizeReport` intersected `ViewAdvancedReports` through `PosRoleAuth`; matrix never grants that capability to ReportingUser/StoreManager/InventoryStaff (React treats advanced reports as **plan entitlement** only) |
| **FILES_CHANGED** | `ReportingEndpoints.cs`; `PosReportingApiTests` operational role coverage |
| **FIX** | Entitlement-only `CommercialAccessGuard.Require(ViewAdvancedReports)`; role still filtered via `AllowsReport` |
| **TEST_ADDED** | YES |
| **ORIGINAL_STATUS** | FAIL |
| **RETEST_STATUS** | PASS (live + integration) |

---

## Code / migration

| Field | Value |
|-------|-------|
| **PRODUCTION_CODE_CHANGED** | YES |
| **BACKEND_CODE_CHANGED** | YES |
| **REACT_CODE_CHANGED** | YES (Playwright e2e only) |
| **MIGRATION_REQUIRED** | NO |

---

## Tests

| Field | Value |
|-------|-------|
| **BACKEND_TARGETED_TESTS** | Platform `Staff_invitation_persists_product_role_on_create_wire` PASS; POS `Operational_sales_summary_allows_ReportingUser...` PASS |
| **POSTGRES_INTEGRATION_TESTS** | Targeted POS reporting integration PASS |
| **REACT_TARGETED_TESTS** | Playwright `pilot-completion-validation.spec.ts` — 4 passed (360/768/1440 + Sell mobile) |
| **REACT_FULL_TEST_COUNT** | 1344 |
| **REACT_FULL_PASS** | 1344 |
| **REACT_FULL_FAIL** | 0 |
| **TYPECHECK** | PASS |
| **LINT** | PASS (0 errors; existing warnings only) |
| **BUILD** | PASS |
| **NEW_TEST_SKIPS** | NO |
| **NEW_TEST_ONLY** | NO |
| **TEST_EXCLUSIONS_ADDED** | NO |

---

## Pilot decision

| Field | Value |
|-------|-------|
| **PILOT_PASS** | YES |
| **PILOT_PASS_REASON** | SCENARIO_14/19/21 PASS after remediating BUG_01 and BUG_02; P0=0; P1 unresolved=0; Cashier/InventoryStaff/ReportingUser API separation verified; responsive critical pages usable |
| **NEXT** | REAL_OPERATOR_SINGLE_STORE_PILOT |
| **NEXT_WHY** | Technical controlled pilot complete; next is day-to-day owner/cashier use (speed, training, edge workflows), not another synthetic hardening package |

---

## Notes

- Unrelated local tools left untouched: `tools/Start-PlatformApiOnly.ps1`, `tools/Start-PosApiOnly.ps1`.
- Restarting Platform LocalValidation **purges** transactional Platform data (`LocalValidationBaselinePurge`); controlled org was recreated for this package.
- Do not treat `/reports/utang-summary` harness aliases as product bugs when `/reports/utang` works (training/harness only).
