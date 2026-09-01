# POS-MULTI-BRANCH-V2-MB2-01C-H1-STRONG-PRODUCT-DUPLICATE-IDENTITY

TASK=POS-MULTI-BRANCH-V2-MB2-01C-H1-STRONG-PRODUCT-DUPLICATE-IDENTITY  
PROGRAM=POS-MULTI-BRANCH-COMMERCE-V2  
PACKAGE=MB2-01C-H1  

START_SHA=2e81a128b2c500259f28f7a19ad7fe11547f5d5d  
IMPLEMENTATION_SHA=fb93c41145e3283b5b6c84b28fc2e908ba6ae806  
FINAL_SHA=(docs commit)  
REMOTE_SHA=(after push)  

STATUS=COMPLETE_VALIDATED_PRODUCT_IDENTITY

## Rule locked

```
ONE ORGANIZATION + ONE NORMALIZED PRODUCT NAME = ONE CatalogProductId
```

Scope (OrganizationStandard / BranchLocal), Active/Inactive, and branch origin do **not** create a second identity. Exact normalized duplicates are hard-blocked; fuzzy similarity is non-blocking and out of scope for merge.

## Delivered

- Domain `CatalogProduct.NormalizedName` + `NormalizeProductName` (NFC → trim → collapse whitespace → uppercase invariant identity; display preserves casing after whitespace cleanup)
- Org-wide unique index `ux_products_org_normalized_name` (`organization_id`, `normalized_name`) — Active+Inactive, Standard+Local
- Migration `AddCatalogProductNormalizedNameIdentity`: backfill, abort on unresolved duplicate groups (`PRODUCT_MERGE=NO`), NOT NULL, unique index
- Central guard `FindProductNameConflictAsync` on create / rename / import / Connected Supplier create+link
- Advisory API `GET .../catalog/products/name-conflict` (`excludeProductId`; `canRevealExisting` privacy for foreign BranchLocal)
- DB unique violation mapped to `pos.catalog.product.name.conflict` (HTTP 409)
- React catalog form: debounced conflict panel, **Use existing** / hidden-foreign copy, **no Create anyway**, Save blocked, 409 refetch
- Identity mutations **ONLINE_REQUIRED**; `OFFLINE_PRODUCT_DRAFT=DEFERRED`; no offline-generated canonical ProductId

## Normalization

| Aspect | Contract |
|--------|----------|
| Unicode | NFC |
| Whitespace | trim + collapse internal runs to one space |
| Identity key | uppercase invariant |
| Display | casing preserved; whitespace cleaned |
| Punctuation | retained (exact key only; no semantic fuzzy) |

Examples: `"  Coke   1L  "` → display `Coke 1L` / identity `COKE 1L`; `"coke\t1l"` → identity `COKE 1L`.

## Schema / migration

| Item | Value |
|------|-------|
| Column | `pos.products.normalized_name` (NOT NULL, max 200) |
| Unique index | `ux_products_org_normalized_name` |
| Uniqueness scope | Organization-wide (not per-branch, not per-scope) |
| Auto-merge | **NO** — migration raises if duplicate groups remain |
| ProductId rewrite | **NO** |

## Guards

| Path | Guard |
|------|-------|
| Manual create (`CatalogProductCreateCore.StageAsync`) | YES — before persist |
| Rename (`UpdateCatalogProduct`) | YES — exclude self |
| Catalog / template import | YES — conflict → skip (no auto-create/merge) |
| Connected Supplier `CreateBuyerProductAndLink` | YES — conflict Failure (block duplicate ProductId) |
| Concurrent race | Application guard + unique DB index → same error code |

## Name-conflict API

- Endpoint: `GET /api/v1/pos/catalog/products/name-conflict?name=&excludeProductId=`
- Response: `{ isDuplicate, canRevealExisting, existingProduct? }`
- Visible: Standard or own/governable Local → product DTO (+ not-offered stamp when branch context present)
- Hidden foreign Local: `isDuplicate=true`, `canRevealExisting=false` — no ProductId / branch / origin leak
- Advisory only; create/update remain authoritative

## React UX

- Debounced server check; inline “Product already exists”
- Visible: Use existing (navigate to edit); inactive / not-offered messaging; no BranchLocal clone
- Foreign Local: privacy-safe message only; no link
- **Create anyway = NO**
- Offline create / identity edit: `OnlineRequiredCard` / Save disabled (`CatalogProductCreate` / `CatalogProductIdentityMutation`)

## Offline / merge locks

| Lock | Value |
|------|-------|
| PRODUCT_CREATE | ONLINE_REQUIRED |
| PRODUCT_RENAME / SKU / barcode | ONLINE_REQUIRED |
| PRODUCT_PROMOTION | ONLINE_REQUIRED |
| PRODUCT_BRANCH_AVAILABILITY_GOVERNANCE | ONLINE_REQUIRED |
| ORGANIZATION_PRODUCT_MASTER_MUTATION | ONLINE_REQUIRED |
| TODAYS_PRICES_MUTATION | ONLINE_REQUIRED_FOR_CURRENT_BASELINE |
| OFFLINE_PRODUCT_DRAFT | DEFERRED |
| PRODUCT_MERGE | NO |
| FUZZY_DUPLICATE_BLOCKING | NO |

## CREATION_PATH audit

| CREATION_PATH | USES_CENTRAL_GUARD | NOTES |
|---------------|--------------------|-------|
| `CreateCatalogProduct` → `CatalogProductCreateCore.StageAsync` | YES | Guard before `AddAsync` |
| `CreateBuyerProductAndLink` → `StageAsync` | YES | Blocks duplicate; candidate/link UX soft gap deferred |
| Catalog import → `CreateImportedSnapshot` | YES | Conflict → skip; no auto-merge |
| `UpdateCatalogProduct` (rename) | YES | Self excluded |
| Offline sync create dispatcher | YES (indirect) | Hits create API / StageAsync; create remains OnlineRequired |
| Direct Buy inline create | N/A | Selects existing products only — no CatalogProduct create |
| PO/purchasing inline create | YES (via StageAsync) | Buyer create+link only |
| Tests / fixtures `CatalogProduct.Create*` | N/A | Non-production setup |

## Out of scope / deferred

- MB2-01D full product-governance closure
- Fuzzy similar-name warnings beyond exact identity
- Offline product draft system
- Explicit legacy Product merge / remediation
- MB2-02 inventory / MB2-03 pricing / MB2-06 full offline matrix

## Validation (pending)

- Focused domain / application / persistence / API / import / React duplicate / offline guard evidence: (pending final SHA stamp)
- Migration lifecycle apply/rollback/re-apply: (pending if not yet recorded)
- Do not treat MB2-01 baseline as complete until MB2-01D

## NEXT

MB2-01D  

HARD_STOP=YES — do not start MB2-01D until explicitly authorized as a separate task.
