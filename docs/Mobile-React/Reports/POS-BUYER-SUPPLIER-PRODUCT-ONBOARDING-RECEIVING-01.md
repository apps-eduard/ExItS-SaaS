# POS-BUYER-SUPPLIER-PRODUCT-ONBOARDING-RECEIVING-01

## Status

Buyer can order shared supplier products before local catalog mapping exists. Receiving is blocked until explicit prepare (link existing or create & link). Silent create-on-receive is not implemented.

## CURRENT_LINK_ARCHITECTURE

- Supplier `CatalogProduct` + `SupplierProductExposure` / share policy
- Buyer `CatalogProduct` (org-owned)
- `BuyerSupplierProductLink` maps relationship + supplier product ↔ buyer product
- Connected PO lines keep **supplier** product identity; buyer PO lines may omit `ProductId` until prepare binds it (`supplier_product_id` + nullable `product_id`)

## PRODUCT_READINESS_MODEL

Derived (not persisted):

| Status | Meaning |
|--------|---------|
| Ready | Active link + buyer product bound on PO line |
| New | No credible match — create from supplier |
| Review | Possible match — user confirms link |
| Conflict | Ambiguous / unavailable prior link |

API: `GET /api/v1/pos/purchase-orders/{id}/receiving-readiness` (`ClassifyConnectedPurchaseReceivingReadiness`)

## BUYER_PRODUCT_CREATE_FLOW

`CreateBuyerProductAndLink` — transactional create + link; optional `PurchaseOrderId` binds PO lines. Buyer fields: name, UOM, **explicit SellingPrice**, optional category/brand/barcode/tracksExpiration. Never copies supplier CategoryId/BrandId.

## MATCHING_RULES

`BuyerSupplierProductMatchClassifier` — exact barcode / SKU / name+UOM; `CanAutoLink` only for unique exact Name+SKU+Barcode+UOM. React no longer auto-links first suggestion.

## CATEGORY_MAPPING / BRAND_MAPPING

Buyer CategoryId / BrandId only (org-scoped). Supplier ids never written as FKs.

## BARCODE_MATCHING

Classifier + create path use buyer barcode uniqueness via catalog create core.

## PURCHASE_PRICE_SEMANTICS / SELLING_PRICE_SEMANTICS

PO / receipt use accepted connected unit price snapshot. Selling price is buyer-controlled; create form does not default to supplier price.

## PACKAGE_CONVERSION

Link `BuyerPurchaseUnitId` / `MultiplierToBase` / `PackageLabel` retained; GRN uses multiplier snapshots.

## RECEIVING_INVENTORY_EFFECT

Only `ReceivePurchaseOrder` / goods receipt mutates OnHand. Create/link leave OnHand unchanged. Receive fails if readiness incomplete.

## SUPPLIER_MASTER_DATA_SYNC

`LinkedSupplierProductSyncService` = local projection / snapshot refresh (`LastKnownOrderPrice`, name snapshot). Does **not** overwrite buyer master. **SUPPLIER_MASTER_DATA_SYNC=SNAPSHOT_ONLY**

## SUPPLIER_IMAGE_TO_BUYER

**DEFERRED** — no binary image copy.

## DISCONNECTED_WITH_OPEN_PO_BEHAVIOR

Existing lifecycle: disconnect does not invent cancel of accepted CPO; new ordering requires Active relationship. Historical accepted lines remain for commercial follow-through per current receive/status gates.

## READINESS_N_PLUS_ONE

Batch link list + paged buyer catalog + supplier product ids by ListByIds — **NO**

## Migration

`20260829140000_AddPoLineSupplierProductIdentity` — nullable `product_id`, `supplier_product_id`, `sku_snapshot`; drop product FK; filtered uniques; filtered unique on active link `(relationship_id, supplier_product_id)`.

## React

- `/purchasing/:id/prepare-products` — PrepareConnectedProductsPage
- PO detail banner when setup required
- Connected catalog: no weak auto-link; create selling price defaults to 0 (buyer sets later / prepare form)

## Explicit exclusions / follow-ups

- UpdatePurchaseOrder path still primarily buyer-product oriented for edits
- Full create form (brand/category quick-create) on prepare page is minimal (selling price + link/create)
- Supplier change notification UX = NEXT
- Manual E2E on Local Validation after migration apply
