# POS-MULTI-BRANCH-V2-MB2-01B-PRODUCT-AUTHORITY-AND-AVAILABILITY

TASK=POS-MULTI-BRANCH-V2-MB2-01B-PRODUCT-AUTHORITY-AND-AVAILABILITY
PROGRAM=POS-MULTI-BRANCH-COMMERCE-V2
PACKAGE=MB2-01B

START_SHA=5cca545c2b2956f5075e029e857ede5e5ee41fb4
IMPLEMENTATION_SHA=489e5d6870e6d21dccb614a2e75b7c5d8512193f
FINAL_SHA=(see after docs commit)
REMOTE_SHA=(must equal FINAL after push)

STATUS=COMPLETE_VALIDATED_AUTHORITY

## Delivered

- Central `CatalogProductGovernanceAuthority` + `ICatalogGovernanceActorAccessor` (Owner/Admin or Platform org management; not StoreManager ManageCatalog for Standard).
- Bulk `CatalogProductAvailabilityResolver` / `CatalogProductCommercialOfferingGate` (scope + branch offering; no N+1).
- Create scope resolution: Owner/Admin omitted → OrganizationStandard; branch actor omitted → BranchLocal with server-derived OriginBranchId from `X-Pos-Branch-Id`.
- Master mutation gates: UpdateCatalogProduct, UpdateCatalogProductPrices, product images, deactivate/reactivate.
- Standard org SellingPrice mutation: governance only until MB2-03 (`STANDARD_PRICE_BRANCH_MUTATION=DENIED_UNTIL_MB2_03`).
- Availability write: `PUT .../products/{id}/branches/{branchId}/availability` (Owner/Admin); sparse restore deletes override.
- Promote: `POST .../products/{id}/promote` — same ProductId; OriginBranchId retained; SellingPrice preserved; no BranchPriceOverride (`PROMOTION_CUSTOM_DEFAULT_WITH_ORIGIN_OVERRIDE=DEFERRED_TO_MB2_03`).
- Commercial enforcement: Sell list filter, CheckoutSale (when branchId present), storefront, PlaceCustomerOrder (FulfillmentBranchId). Delivery quote remains fee-only (no product lines); place revalidates independently.
- Import: OrganizationStandard only; require org governance (narrowed, not widened).
- DTO additive: Scope, OriginBranchId, IsOfferedAtBranch.
- Error codes: product_scope_forbidden, product_origin_branch_forbidden, product_not_offered_at_branch, product_promotion_forbidden, product_availability_forbidden, etc.

## Explicit exclusions

- MB2-01C React governance UX
- MB2-02 inventory authority redesign
- MB2-03 branch pricing / BranchPriceOverride
- New EF migration (none; schema from 01A)

## Migration

MIGRATION_CREATED=NO
SCHEMA_CHANGES=NONE

## Authority summary

| Concern | Rule |
|--------|------|
| Standard master | Owner/Admin (+ Platform org management) |
| BranchLocal create/edit | ManageCatalog + origin acting branch (Owner/Admin any) |
| Standard price (temp) | Governance only until MB2-03 |
| Promotion | Governance only |
| Standard availability write | Governance only |
| Import | Governance → OrganizationStandard |

## Availability summary

| Case | Result |
|------|--------|
| Standard + no row | Offered |
| Standard + false | Not offered |
| Local at origin | Offered (unless explicit false at origin) |
| Local at foreign | Never offered (malicious true ignored) |
| Restore offered | Delete sparse false row |
| Disable with stock | ALLOW + WARN; no stock delete |

## Commercial enforcement

| Surface | Behavior |
|---------|----------|
| Sell catalog | Bulk filter when canBeSold/commerciallyOffered + branch header |
| Checkout | Re-resolve when branchId present |
| Storefront | Bulk filter by fulfillment branch |
| Quote (delivery) | No merchandise lines — N/A for product offering |
| Place order | Bulk re-resolve vs FulfillmentBranchId |
| Returns/history | Not blocked solely by Not offered |

## Tests

- Domain/application unit: PGA-AUTH, PGA-AVL, PGA-CREATE, PGA-PROMOTE, PGA-PRICE, commercial gate (35 focused; Catalog suite 219 passed).
- React: not run (no React production changes).
- Broader API/integration: deferred where Docker/Testcontainers heavy; server rules covered in unit + wired use cases.

## P0 / P1

P0_UNRESOLVED=0
P1_UNRESOLVED=0

## Deferred

- MB2-01C React governance UX
- MB2-01D final product-governance closure
- MB2-02 branch inventory authority
- MB2-03 branch pricing/effective price

NEXT=MB2_01C
HARD_STOP=YES — do not start MB2-01C in this package.
