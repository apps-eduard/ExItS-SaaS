# POS-MULTI-BRANCH-V2-MB2-01C-PRODUCT-GOVERNANCE-REACT-UX

TASK=POS-MULTI-BRANCH-V2-MB2-01C-PRODUCT-GOVERNANCE-REACT-UX  
PROGRAM=POS-MULTI-BRANCH-COMMERCE-V2  
PACKAGE=MB2-01C  

START_SHA=20b0fa6ad0adf1148afa627e2aa63cc3ad687159  
IMPLEMENTATION_SHA=10e586e6666a2b4724305bcf84e8248476bb1040  
FINAL_SHA=(docs commit)  
REMOTE_SHA=(after push)  

STATUS=COMPLETE_VALIDATED_UX  

## Delivered

- Merchant-facing Organization / Branch product terminology (not Global/Local)
- Catalog list scope filters (server `scope=` before pagination)
- Product card scope badges + not-offered management label
- Create scope UX (Owner choice; branch actor fixed BranchLocal; origin server-derived)
- OrganizationStandard read-only detail for normal branch actors
- BranchLocal edit + Owner promote confirmation
- Owner/Admin branch availability toggles (sparse overrides + Platform branch names)
- Today's Prices interim authority (Standard org-price Owner-only; Local editable)
- Global Catalog / Template import gated to org governance
- Connected Buyer share mutations gated to org governance; BranchLocal excluded from candidates when scope present

## Backend support (narrow)

| Capability | Decision |
|------------|----------|
| `scope=` list filter | YES — EF before Count/Skip/Take |
| `originBranchId=` list filter | YES (server) — React UI deferred (secondary) |
| Management `isOfferedAtBranch` stamp | YES — when `X-Pos-Branch-Id` present; does not filter membership |
| All-branch availability read | YES — `GET .../products/{id}/branch-availability` sparse `explicitRows`; React merges Active Platform branches |
| Migration | NO |

## React authority

- `canGovernOrganizationCatalog` = `hasOrganizationManagementAuthority`
- `canManageCatalog` unchanged (branch/local operational)

## Out of scope (locked)

- MB2-01D closure
- Branch pricing (MB2-03)
- Inventory redesign (MB2-02)
- New product authority model

## Validation (package)

- Catalog unit tests: PASS
- Scope/availability read unit tests: PASS
- React focused governance + catalog: PASS
- typecheck: PASS
- lint: 0 errors
- build: PASS
- Playwright: not run (auth harness / full closure deferred to MB2-01D)

## NEXT

MB2-01C-H1 (strong product duplicate identity) → then MB2-01D

Historical note: MB2-01C-H1 followed this UX package before validation closure.

HARD_STOP=YES — do not start MB2-01D in this package.
