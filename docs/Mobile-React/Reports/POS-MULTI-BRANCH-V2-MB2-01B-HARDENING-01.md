# POS-MULTI-BRANCH-V2-MB2-01B-HARDENING-01

TASK=POS-MULTI-BRANCH-V2-MB2-01B-HARDENING-01
PROGRAM=POS-MULTI-BRANCH-COMMERCE-V2
PACKAGE=MB2-01B-H1

START_SHA=a7d13d5d37a2d6f68e7e708fe8ca6aa9af409a36
IMPLEMENTATION_SHA=b87ce6056ef56f7acd3a8ce355faf2ac90edffe3
FINAL_SHA=(docs commit)
REMOTE_SHA=(after push)

STATUS=COMPLETE_VALIDATED

## Findings closed

| ID | Finding | Fix |
|----|---------|-----|
| H1-01/04 | Post-pagination availability/scope filter; wrong TotalCount | EF `CatalogProductFilter` membership before Count/Skip/Take |
| H1-05 | canBeSold conflated with commerciallyOffered | Split flags; commercial only when `commerciallyOffered=true` |
| H1-07/08 | SKU/barcode foreign Local leak | Management visibility gate + optional commercial offering check |
| H1-10 | Image foreign Local leak | Same management visibility before image read |
| H1-11 | Optional governance fail-open | Mandatory governance+actor on mutation/import/image/CB use cases |
| H1-12/16 | Connected Buyer ManageCatalog bypass + Local select-all | Org governance required; Scope=OrganizationStandard before ID select |

## Pagination

- Filter before Count/Skip/Take: YES (SQL join on `branch_product_availabilities`)
- TotalCount = full filtered membership: YES
- Sell React sends `commerciallyOffered=true` with `canBeSold=true`

## Connected Buyer

BRANCH_LOCAL_CONNECTED_BUYER_EXPOSURE=NOT_SUPPORTED_V1_PROMOTE_FIRST

## Mutation matrix (OrganizationStandard / BranchLocal)

| PATH | Std Owner/Admin | Std Branch | Local Origin | Local Foreign |
|------|-----------------|------------|--------------|---------------|
| Product details | allow | deny | allow | deny |
| Today's Prices / price | allow | deny | allow | deny |
| Images set/remove | allow | deny | allow | deny |
| Activate/deactivate | allow | deny | allow | deny |
| Import | allow | deny | n/a | n/a |
| Promote | allow | deny | n/a | n/a |
| Availability write | allow | deny | n/a | n/a |
| Connected Buyer expose/PO | allow | deny | deny (promote first) | deny |
| Image GET | allow | allow (Std) | allow | not found |
| SKU/Barcode management | allow | allow (Std + own Local) | allow | not found |
| Commercial exact lookup | offered only | offered only | offered at origin | not found |

## Migration

MIGRATION_CREATED=NO
SCHEMA_CHANGES=NONE

## Tests

- Hardening focused + prior MB2-01B + Catalog unit: PASS (227 Catalog filter)
- React typecheck: PASS (Sell plumbing)
- No .skip/.only

## Deferred

- MB2-01C React governance UX
- Full Testcontainers pagination SQL proof (EF composition covered in repository; integration suite optional follow-up)
- MB2-02 / MB2-03

NEXT=MB2_01C
HARD_STOP=YES
