# POS-ORG-BRANCH-MANAGEMENT-01

TASK=POS-ORG-BRANCH-MANAGEMENT-01  
START_SHA=a452226bbd788ef99d0d5a6987323b5ee31c9500  
FEATURE_SHA=7cd953dedfae6cd5f2cdc99a35c7df62e94f647b  
FINAL_SHA=7cd953dedfae6cd5f2cdc99a35c7df62e94f647b  
REMOTE_SHA=7cd953dedfae6cd5f2cdc99a35c7df62e94f647b

## Routes

BRANCH_MANAGEMENT_ROUTE=/org/branches  
BRANCH_CREATE_ROUTE=/org/branches/new  
BRANCH_DETAIL_ROUTE=/org/branches/{branchId}  
BRANCH_FULFILLMENT_ROUTE=/org/branches/{branchId}/fulfillment  

Fulfillment editor remains the existing `BranchFulfillmentEditPage` (setup tabs preserved).  
`/org/branches` is Branch Management (list/create/lifecycle), not the fulfillment list.

## Capacity

BRANCH_CAPACITY=GET .../branches/capacity (used / allowed from active POS plan)  
BRANCH_CAPACITY_SEMANTICS=ACTIVE_ONLY (`CountActiveAsync` = Active status only; Suspended/Archived do not consume MaxBranches)  
CAPACITY_REACTIVATION_HARDENED=YES (`ReactivateBranch` rejects when Active count ≥ MaxBranches)  
STAFF_COUNT_SEMANTICS=explicit active normal-staff assignments only; Owner/Admin explained as automatic org-wide access (not inflated into the assigned count)

## Create / edit

CREATE_BRANCH=YES (`/org/branches/new` → detail)  
EDIT_BRANCH=YES (Details tab; details-only update)  
BRANCH_CODE_IMMUTABLE=YES (read-only after create; server code remains identity)  

COUNTRY_MODE=PH_ONLY  
COUNTRY_EDITABLE=NO  
TIMEZONE=Asia/Manila  
TIMEZONE_EDITABLE=NO  

NEW_BRANCH_DEFAULTS=Active, IsPrimary=false, CustomerOrdering/Pickup/Delivery OFF, OnlineOrdersPaused=false  
NEW_BRANCH_AUTO_ASSIGNS_STAFF=NO  
NEW_BRANCH_INVENTORY_CLONED=NO  
NEW_BRANCH_DEVICES_CLONED=NO  
NEW_BRANCH_FULFILLMENT_CLONED=NO  

## Primary

PRIMARY_CHANGE_EXISTED_BEFORE=NO  
PRIMARY_CHANGE_IMPLEMENTED=YES (`SetPrimaryBranch` + POST .../set-primary)  
PRIMARY_CHANGE_ATOMIC=YES (demote current + promote target in one UoW)  
PRIMARY_DB_UNIQUENESS=YES (`ux_organization_branches_one_primary` filtered unique on organization_id WHERE is_primary = TRUE)  
PRIMARY_CHANGE_AUDITED=YES (`OrganizationBranchPrimaryChanged` + governance step-up + reason)  
PRIMARY_BRANCH_COUNT_AFTER_CHANGE=1  

## Staff access

STAFF_BRANCH_ACL_REUSED=YES (`organization_membership_branch_assignments`)  
STAFF_ACCESS_UI=YES (search/add + remove; no checkbox wall)  
OWNER_ALL_BRANCH_ACCESS=YES (implicit Active)  
ADMIN_ALL_BRANCH_ACCESS=YES (implicit Active per existing resolver)  
NORMAL_STAFF_EXPLICIT_ASSIGNMENT=YES  
LAST_ASSIGNMENT_PROTECTED=YES (server + UI messaging)  
STAFF_ACCESS_AGGREGATION=GET .../branches/{id}/staff-access  
BRANCH_MANAGEMENT_SUMMARY=GET .../branches/management-summary  

## Devices / lifecycle / fulfillment

DEVICES_TAB=YES (filtered existing POS devices)  
DEVICE_BRANCH_BINDING_PRESERVED=YES  
DEVICE_REASSIGNMENT_UI=NO (revoke + re-register remains the path)  

SUSPEND=YES  
REACTIVATE=YES  
ARCHIVE=YES  
HARD_DELETE=NO  

PRIMARY_SUSPEND=DENIED  
PRIMARY_ARCHIVE=DENIED  
ARCHIVED_REACTIVATE=NO (domain terminal)  

FULFILLMENT_REUSED=YES  
FULFILLMENT_DUPLICATED=NO  

## Migration

MIGRATION_REQUIRED=YES  
MIGRATION=20260831172853_AddOrganizationBranchPrimaryUniqueness  

## Security final review

CAN_STAFF_ACCESS_UNASSIGNED_BRANCH=NO  
CAN_BRANCH_ACCESS_GRANT_POS_PERMISSION=NO  
CAN_BRANCH_CREATION_CLONE_STOCK=NO  
CAN_BRANCH_CREATION_CLONE_DEVICES=NO  
CAN_BRANCH_CREATION_AUTO_ASSIGN_EXISTING_STAFF=NO  
CAN_PRIMARY_BE_SUSPENDED=NO  
CAN_PRIMARY_BE_ARCHIVED=NO  
CAN_ARCHIVED_BRANCH_ACCEPT_NEW_OPERATIONS=NO  
CAN_ARCHIVED_BRANCH_BE_REACTIVATED=NO  
CAN_BRANCH_LIFECYCLE_HARD_DELETE_HISTORY=NO  
CAN_CROSS_ORG_BRANCH_MUTATION=NO  
CAN_REACTIVATION_BYPASS_PLAN_LIMIT=NO  
PRIMARY_BRANCH_COUNT_AFTER_CHANGE=1  

## Validation evidence

PLATFORM_TESTS=OrganizationBranchAndPosDeviceTests + BranchPrimaryAndCapacityTests + OrganizationBranchAccessServiceTests (+ related Branch/MembershipBranch filters) — 61 passed  
POSTGRES_TESTS=NOT_RUN_IN_THIS_PACKAGE (filtered unique index shipped; apply via normal Platform migration path)  
POS_TESTS=N/A (no POS inventory mutation in this package; create-branch clone protections preserved by architecture)  

TARGETED_REACT_TESTS=src/features/branches — 31 passed  
REACT_TOTAL=1384  
REACT_PASS=1384  
REACT_FAIL=0  

PLAYWRIGHT_TESTS=existing fulfillment e2e path updates only (fulfillment under `/fulfillment`)  
PLAYWRIGHT_LIVE_STATUS=NOT_RUN (authenticated pilot harness not exercised in this package)  

RESPONSIVE_360=YES (single-column cards/meta; full-width Add)  
RESPONSIVE_768=YES (comfortable card stack)  
RESPONSIVE_1440=YES (2-column branch card grid)  

TYPECHECK=PASS  
LINT=PASS (0 errors; existing warnings only)  
BUILD=PASS  
DOTNET_BUILD=PASS (Platform.Api Release)  

SKIPPED_TESTS=0  
ONLY_TESTS=0  
NEW_EXCLUSIONS=0  

## Defects

P0=0  
P1=0  
P2=0  
P0_UNRESOLVED=0  
P1_UNRESOLVED=0  

## Status

BRANCH_MANAGEMENT_STATUS=COMPLETE_VALIDATED_BASELINE  

DEFERRED=  
- Live Joe-store Playwright BRANCH-UI-01..13  
- PostgreSQL uniqueness concurrency stress beyond filtered unique index  
- Device reassignment domain flow  

NEXT=PRODUCT_EXPANSION_REASSESSMENT  
NEXT_WHY=Branch Management baseline is complete; do not auto-start ROLE_MANAGEMENT redesign, custom roles, device offline, or branch accounting.

## Git

START_SHA=a452226bbd788ef99d0d5a6987323b5ee31c9500  
FEATURE_SHA=7cd953dedfae6cd5f2cdc99a35c7df62e94f647b
FINAL_SHA=7cd953dedfae6cd5f2cdc99a35c7df62e94f647b
REMOTE_SHA=(updated after push)
