# POS-CONNECTED-BUYER-CREATE-LINK-TEST-REPAIR-01

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**TASK:** POS-CONNECTED-BUYER-CREATE-LINK-TEST-REPAIR-01  
**START_SHA:** `bc1730063955aa3363dcf97cf119ddfe775da116`  
**FEATURE_SHA:** _(recorded at feature commit)_  
**FINAL_SHA:** _(recorded after docs stamp)_

## ORIGINAL_FAILURE

| Metric | Value |
|--------|--------|
| ORIGINAL_FAILURE_COUNT | 3 |
| ORIGINAL_PASS_COUNT | 9 |
| ORIGINAL_TOTAL | 12 |

Failing facts (before repair):

- `Create_and_link_creates_exactly_one_product_and_link_with_independent_selling_price`
- `Duplicate_create_and_link_retry_returns_already_linked_without_second_product`
- `Create_and_link_does_not_inject_inventory_dependencies_and_creates_catalog_product_only`

Error: `pos.catalog.bulk_validation: Choose how your business will use this product before creating it.`

## ROOT_CAUSE

**ROOT_CAUSE=STALE_TEST_FIXTURE**

Shared helper `Request(...)` built `CreateBuyerProductAndLinkRequest` without `BusinessUsage` (and without usage flags / `UsagePreset`). Production use case requires at least one of: `BusinessUsage`, `UsagePreset`, `CanBeSold`, `CanBeUsedAsIngredient`, `IsProduced`.

All three failures classified **STALE_TEST_FIXTURE**. No **PRODUCTION_BUG**.

## PRODUCTION_BUG_FOUND

**NO**

## BUSINESS_USAGE_CONTRACT

- Backend does **not** silently default to Resale.
- Create-and-link rejects when usage selection is entirely absent (`CatalogBulkValidation`).
- Supported values flow through `CatalogProductCreateCore` / BusinessUsage (Resale and other catalog usages).
- Normal connected buyer create-and-link is expected to send **Resale**.

## REACT_PAYLOAD_CONTRACT

Proven in code:

- `ConnectedCatalogPage.tsx` → `businessUsage: "Resale"`
- `pos-connected-suppliers-client.ts` posts `businessUsage`
- `pos-connected-suppliers-client.test.ts` asserts Resale payload
- `PrepareConnectedProductsPage.tsx` also captures BusinessUsage (including non-Resale when selected)

## TEST_FIX_MODEL

- Updated shared `Request(...)` helper: `BusinessUsage: "Resale"` (matches React contract).
- Added explicit negative: `Missing_business_usage_fails_before_product_is_added` (`with { BusinessUsage = null }`).
- Early-fail / link-existing / suggestion tests unchanged in intent.

## PRODUCTION_CODE_CHANGE_REQUIRED

**NO**

## CONNECTED_BUYER_FLOW_STATUS

**ALIGNED** — share → browse → create/link → local product + `BuyerSupplierProductLink`; React and backend agree on BusinessUsage for the happy path.

## VALIDATION

| Gate | Result |
|------|--------|
| CreateBuyerProductAndLinkTests | **13 / 13 PASS** (was 12; +1 negative) |
| ConnectedSuppliers unit filter | **122 / 122 PASS** |
| PosPurchasingScopeArchitectureTests | **7 / 7 PASS** |
| POSTGRES_TEST_STATUS | **NOT_ADDED** — no existing create-and-link API integration harness; out of scope for this repair |
| React targeted | `pos-connected-suppliers-client.test.ts`, `product-business-usage.test.ts` |
| REACT_FULL | **1344 / 1344 PASS** |
| TYPECHECK / LINT / BUILD | **PASS** |
| NEW_TEST_SKIPS / ONLY / EXCLUSIONS | **none** |

Note: an over-broad architecture filter matching `Supplier` also hit pre-existing unrelated Inventory/Sales scope string-guard failures (`SupplierId` / `PurchaseOrder` substrings). Those are **PRE_EXISTING_UNRELATED** to this package; purchasing-scope guards remain green.

## NEXT

**NEXT:** `POS-ORGANIZATION-PILOT-PREP-01`  
**NEXT_WHY:** Create/link unit contract repaired; Organization POS is controlled-pilot ready. Prefer small pilot/operator preparation over large features. Do not auto-pick device/offline, B2B checkout, real payment gateway, FIFO, or GL.
