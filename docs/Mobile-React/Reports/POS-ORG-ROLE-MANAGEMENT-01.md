# POS-ORG-ROLE-MANAGEMENT-01

TASK=POS-ORG-ROLE-MANAGEMENT-01  
START_SHA=ff39879df983e5eaf726de7a46d77fb21998d8c4  
FEATURE_SHA=(see FINAL_SHA)  
FINAL_SHA=(recorded after push)  
REMOTE_SHA=(recorded after push)

## Architecture

AUTH_ARCHITECTURE_AUDITED=YES  
ORG_MEMBERSHIP_SEPARATE_FROM_POS_ROLE=YES  
POS_OWNER_SEPARATE_FROM_ORG_OWNER=YES  
ROLE_CATALOG_SERVER_AUTHORITY=YES  
ROLE_PERMISSION_MATRIX_SOURCE=PinoyBusinessPosProductLocalRoleCatalog + PosRoleMatrix (server)  
SYSTEM_ROLES=Owner, Manager, Cashier, InventoryStaff, ReportingUser

### Role labels (merchant-facing)

POS_OWNER=POS Owner — full operational POS access; does not transfer organization ownership  
MANAGER=Manager — day-to-day store operations  
CASHIER=Cashier — selling and checkout  
INVENTORY_STAFF=Inventory Staff — stock, purchasing, inventory operations  
REPORTING_USER=Reporting User — reports/read without operational writes

## UI / routes

ROLE_MANAGEMENT_ROUTE=/org/roles  
ROLE_DETAIL_UI=/org/roles/:roleCode — read-only permission groups  
ROLE_PERMISSION_GROUPS=Selling, Customers & Utang, Inventory, Purchasing & suppliers, Operations, Reports, Settings

CUSTOM_ROLES_IMPLEMENTED=NO  
CUSTOM_ROLES_STATUS=DEFERRED_UNTIL_GRANULAR_AUTH_ENGINE

## Semantics

ONE_EFFECTIVE_POS_ROLE_PER_USER=YES (unique active ProductLocalRoleGrant per org+user+product; assign/PUT replaces atomically)  
ROLE_REPLACEMENT_ATOMIC=YES (PUT /api/v1/organizations/{id}/product-local-roles/users/{userId})  
ROLE_HISTORY_PRESERVED=YES (revoked grants retain audit fields)

ORG_OWNER_PROTECTED=YES  
POS_OWNER_TRANSFERS_ORG_OWNERSHIP=NO

STAFF_ROLE_ASSIGNMENT=OrgStaffAssignPage — server catalog, POS Owner confirmation  
STAFF_ROLE_CHANGE=atomic PUT via changeProductLocalRole  
REMOVE_POS_ACCESS=revokeProductLocalRole; membership preserved  
MEMBERSHIP_PRESERVED_ON_POS_REVOKE=YES

INVITE_ALL_ROLES=YES (OrgStaffInvitePage — server catalog)  
INVITE_ROLE_FORWARDING=YES (productRole on invitation create)  
INVITE_ROLE_ACCEPTANCE=YES (existing OrganizationScopedStaffIdentityTests)

SESSION_ROLE_REFRESH=session grant recalculated on next workspace/session fetch (no auth weakening)

## Role management authority

CAN_ORG_OWNER_MANAGE_ROLES=YES (EnsureCanManageMemberships)  
CAN_ORG_ADMIN_MANAGE_ROLES=NO  
CAN_POS_OWNER_MANAGE_ROLES=NO  
CAN_MANAGER_MANAGE_ROLES=NO  
CAN_CASHIER_MANAGE_ROLES=NO  
CAN_INVENTORY_STAFF_MANAGE_ROLES=NO  
CAN_REPORTING_USER_MANAGE_ROLES=NO

## Security

CAN_UNKNOWN_ROLE_BE_ASSIGNED=NO  
CAN_ROLE_CHANGE_CROSS_ORG=NO  
CAN_REVOKED_MEMBER_USE_OLD_ROLE=NO  
CAN_SUSPENDED_MEMBER_USE_ROLE=NO

BRANCH_STAFF_ACCESS_MANAGEMENT=DEFERRED

MIGRATION_REQUIRED=NO

## Validation evidence

PLATFORM_TESTS=PinoyBusinessPosProductLocalRoleCatalogTests (6) + ProductAuthorizationAndDiscoveryTests + OrganizationScopedStaffIdentityTests — 40 passed  
POS_AUTHORIZATION_TESTS=PosRoleMatrixTests — 60 passed  
INVITATION_TESTS=OrganizationScopedStaffIdentityTests (included above)  
SESSION_GRANT_TESTS=ProductAuthorizationAndDiscoveryTests (included above)  
POSTGRES_TESTS=not required (no schema change)

TARGETED_REACT_TESTS=OrgStaffPage.test.tsx, OrgRolesPage.test.tsx — 5 passed  
REACT_TOTAL=1377  
REACT_PASS=1377  
REACT_FAIL=0

PLAYWRIGHT_TESTS=not added this package  
PLAYWRIGHT_LIVE_STATUS=BLOCKED_AUTH

RESPONSIVE_360=PASS (staff access grid, role cards, assign radiogroup)  
RESPONSIVE_768=PASS  
RESPONSIVE_1440=PASS

TYPECHECK=PASS  
LINT=PASS (0 errors)  
BUILD=PASS  
DOTNET_BUILD=PASS (Platform + unit tests Release)

P0=0  
P1=0  
P0_UNRESOLVED=0  
P1_UNRESOLVED=0

ROLE_MANAGEMENT_STATUS=COMPLETE_VALIDATED_BASELINE

DEFERRED=custom roles; branch staff access management; Playwright live harness

NEXT=PRODUCT_EXPANSION_REASSESSMENT  
NEXT_WHY=Role management V1 baseline complete; reassess adjacent product scope before new packages.

## Key files

- `src/Platform/ExItS.Platform.Application/Access/PinoyBusinessPosProductLocalRoleCatalog.cs`
- `src/Platform/ExItS.Platform.Api/Access/ProductNavigationEndpoints.cs`
- `src/Platform/ExItS.Platform.Domain/Organizations/ProductLocalRoleGrant.cs`
- `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React/src/features/staff/OrgRolesPage.tsx`
- `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React/src/features/staff/OrgRoleDetailPage.tsx`
- `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React/src/features/staff/OrgStaffPage.tsx`
- `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React/src/features/staff/OrgStaffAssignPage.tsx`
- `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React/src/features/staff/OrgStaffInvitePage.tsx`

## Preserved (not modified)

Branch fulfillment setup tabs, single-branch redirect, PH/Asia/Manila read-only defaults — unchanged from START_SHA.
