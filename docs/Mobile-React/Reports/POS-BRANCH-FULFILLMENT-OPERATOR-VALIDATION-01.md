# POS-BRANCH-FULFILLMENT-OPERATOR-VALIDATION-01

**Status:** PASS  
**Branch:** `feat/organization`  
**TASK:** POS-BRANCH-FULFILLMENT-OPERATOR-VALIDATION-01  

| Field | Value |
|-------|-------|
| START_SHA | `d534dacd96762a9be3c2a40c0e46454f005ef626` |
| FEATURE_SHA | `40e2b029856793d92285368ec0140083b53ba56c` |
| FINAL_SHA | `007e741952e56a54ef653d142dde61c9d06a075b` |
| REMOTE_SHA | `007e741952e56a54ef653d142dde61c9d06a075b` |

## Goal

Real runtime/API operator validation of Branch Fulfillment setup for **Joe store / Main Branch**, fixing any P0/P1 defects discovered in-package.

## Organization under test

| Field | Value |
|-------|-------|
| ORGANIZATION | Joe store |
| BRANCH | Main Branch |
| ORG_ID | `37acc96a-d5f3-4e0c-8233-d104790caf30` |
| BRANCH_ID | `70fddbbb-0208-4be9-a543-426f1b217bfc` |
| CUSTOMER_ORDERING_ENTITLEMENT | `true` |
| DELIVERY_ENTITLEMENT | `true` |

## Validation matrix

| Check | Status |
|-------|--------|
| BRANCH_LIST_STATUS | API_READY_FIELDS_PRESENT |
| PICKUP_INCOMPLETE_GATE | PASS_DETAILS_HOURS_ALREADY_SET |
| DELIVERY_INCOMPLETE_GATE | PASS |
| BACKEND_GATE | PASS (delivery enable rejected when DeliveryReady=false) |
| BRANCH_DETAILS_STATUS | PASS |
| OPERATING_HOURS_STATUS | PASS |
| PICKUP_READY_STATUS | PASS |
| PICKUP_ENABLE_STATUS | PASS |
| PICKUP_WITHOUT_DELIVERY_CONFIG | PASS |
| DELIVERY_LOCATION_STATUS | PASS |
| DELIVERY_POLICY_STATUS | PASS |
| DELIVERY_AREA_ADD_STATUS | PASS |
| DELIVERY_AREA_DUPLICATE_STATUS | PASS |
| DELIVERY_AREA_REMOVE_STATUS | PASS |
| DELIVERY_SETUP_SECTION_STATUS | 5/5 |
| DELIVERY_READY_STATUS | PASS |
| DELIVERY_ENABLE_STATUS | PASS |
| LAST_AREA_REMOVAL_BEHAVIOR | DEACTIVATE_ALLOWED_READY_FALSE_ENABLED_MAY_REMAIN |
| ZERO_AREA_DELIVERY_OPERATIONAL | false |
| STOREFRONT_STATUS | PASS |
| DELIVERY_AREA_SELECTOR_STATUS | PASS_CONFIGURED_AREAS_EXPOSED |
| VALID_QUOTE_STATUS | PASS |
| DELIVERY_FEE_RESULT | 30.00 (base fee; inside included distance) |
| OUTSIDE_DISTANCE_STATUS | PASS |
| UNCONFIGURED_CITY_STATUS | PASS_NO_FREE_TEXT_AUTHORITY |
| NONEXISTENT_AREA_GUARD | PASS |
| CROSS_BRANCH_AREA_GUARD | PASS |
| CROSS_ORG_AREA_GUARD | PASS |
| INACTIVE_AREA_GUARD | PASS |
| DELIVERY_ORDER_STATUS | PASS |
| DELIVERY_SNAPSHOT_STATUS | PASS (city=`Bacolod City`) |
| HISTORICAL_AREA_SNAPSHOT_STATUS | PASS |
| PAUSE_STATUS | PASS |
| RESUME_STATUS | PASS |
| CLOSED_STATE_STATUS | PASS |
| RESPONSIVE_360 | PASS (no body horizontal overflow on `/org/branches`, `/`, `/activate-account`) |
| RESPONSIVE_768 | PASS |
| RESPONSIVE_1440 | PASS |
| CITY_COORDINATE_GEOMETRIC_VALIDATION | DEFERRED |

## Bugs found and fixed

### BUG_FULFILLMENT_01 — P1

| Field | Value |
|-------|-------|
| SEVERITY | P1 |
| REPRODUCTION | PUT branch with only `latitude`/`longitude` after structured address saved |
| EXPECTED | Address retained; Delivery location can complete with existing details |
| ACTUAL | `UpdateAddress` cleared address fields → `branch_address_incomplete` / Delivery stuck below 5/5 |
| ROOT_CAUSE | `UpdateBranch` always applied null address fields as clears |
| FIX | Merge-patch: `command.X ?? branch.X`; React sends trimmed strings (incl. `""` to clear) |
| FILES_CHANGED | `BranchUseCases.cs`, `BranchFulfillmentEditPage.tsx`, `UpdateBranchPartialAddressTests.cs` |
| TEST_ADDED | `UpdateBranchPartialAddressTests` (2 PASS) |
| ORIGINAL_STATUS | FAIL |
| RETEST_STATUS | PASS (`COORD_UPDATE_PRESERVES_ADDRESS`) |

### BUG_FULFILLMENT_02 — P1

| Field | Value |
|-------|-------|
| SEVERITY | P1 |
| REPRODUCTION | Linked Personal buyer opens storefront / quote for Joe store |
| EXPECTED | Storefront `branches[]` includes Main Branch + public delivery service areas |
| ACTUAL | `branches: []`; quote/place returned 404 area/branch failures |
| ROOT_CAUSE | POS `PosCustomerOrderBranchDirectory` called org-membership `/branches` API (403 for Personal); no linked-merchant projection |
| FIX | Platform `GET /api/v1/personal/linked-merchants/{orgId}/fulfillment-branches` + POS directory prefers that path; credentials via `PlatformCallerCredentialForwarder` |
| FILES_CHANGED | `PersonalLinkedMerchantUseCases.cs`, `PersonalEndpoints.cs`, `Program.cs`, `BranchUseCases.cs` (`ExecuteForLinkedCustomerAsync`), `PosCustomerOrderBranchDirectory.cs` |
| TEST_ADDED | Runtime operator harness + existing DeliveryServiceArea quote/place unit suite (10 PASS) |
| ORIGINAL_STATUS | FAIL |
| RETEST_STATUS | PASS (storefront branches=1, quote fee=30, place order + historical snapshot) |

## Bug counts

| Metric | Value |
|--------|-------|
| BUGS_FOUND | 2 |
| BUGS_FIXED | 2 |
| P0_COUNT | 0 |
| P1_COUNT | 2 |
| P2_COUNT | 0 |
| P0_UNRESOLVED | 0 |
| P1_UNRESOLVED | 0 |

## Code / evidence flags

| Field | Value |
|-------|-------|
| PRODUCTION_CODE_CHANGED | YES |
| BACKEND_CODE_CHANGED | YES |
| REACT_CODE_CHANGED | YES (BranchFulfillmentEditPage merge-patch client) |
| MIGRATION_CHANGED | NO |
| PLATFORM_TESTS_RERUN | YES — `UpdateBranchPartialAddress` 2 PASS; related CustomerLinkConsent filter 17 PASS |
| POS_TESTS_RERUN | YES — `DeliveryServiceAreaQuotePlace` 10 PASS |
| REACT_TARGETED_TESTS | NO (no React test change; edit is trim/`""` send) |
| REACT_FULL_RERUN | NO — production React change is narrow; full 1353 reused from SETUP-01 |
| BUILD_EVIDENCE_REUSED | PARTIAL — Release rebuild of Platform.Api + POS.Api after fixes |
| TYPECHECK | REUSED (SETUP-01 PASS; React change trivial) |
| LINT | REUSED (SETUP-01 PASS) |
| BUILD | REUSED React; Platform/POS Release build PASS |
| DOTNET_BUILD | PASS |

## Explicit V1 limitation

`CITY_COORDINATE_GEOMETRIC_VALIDATION=DEFERRED`

V1 validates configured service-area selection + maximum Haversine distance. It does **not** prove coordinates lie inside an administrative city polygon.

## Pass decision

`FULFILLMENT_OPERATOR_VALIDATION=PASS`

**NEXT:** `FULFILLMENT_PILOT_COMPLETE`  
**NEXT_WHY:** Operator validation green with P0/P1 unresolved = 0; do not auto-start barangay/geofencing.
