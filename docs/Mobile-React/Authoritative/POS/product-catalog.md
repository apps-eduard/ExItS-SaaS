# Product Catalog

## CURRENT contract

| Topic | Behavior | Status | Evidence |
|-------|----------|--------|----------|
| Categories | Org-local `ProductCategory` | PROVEN_CURRENT | Domain + `/api/v1/pos/catalog/categories` |
| Products | `CatalogProduct` org-owned | PROVEN_CURRENT | `CatalogProduct.cs`, Catalog endpoints |
| Name/description/image | Supported; images via product image APIs | PROVEN_CURRENT | `CatalogProductImage`, ProductImage use cases |
| SKU / barcode | Lookup by SKU/barcode | PROVEN_CURRENT | `/by-sku/{sku}`, `/by-barcode/{barcode}` |
| Status / sellability | `CanBeSold`, `CanBePurchased`, ingredient/produced flags | PROVEN_CURRENT | Product behavior flags |
| Global Catalog relationship | Optional Platform refs (`PlatformGlobalProductId`, template, platform barcode) | PROVEN_CURRENT | Catalog product fields + Platform Global Catalog |
| Template/import | Catalog import jobs/items | PROVEN_CURRENT | `/api/v1/pos/catalog-imports` |
| Business type classification | Platform BT + template suggestions; not separate inventory engines | PROVEN_CURRENT | engineering units doc |
| Supplier-linked products | Connected buyer/supplier product links | PROVEN_CURRENT | connected supplier domain |
| Storefront exposure | Availability uses tracked Available qty; untracked orderable without qty | PROVEN_CURRENT | storefront availability engineering note |

## Product units (see UOM doc)

Products may define `CatalogProductUnit` rows (`Kind` Purchase/Sell) with `MultiplierToBase` and independent sell prices.

## Tables

`pos.product_categories`, `pos.products`, `pos.product_units`, `pos.product_images`, `pos.catalog_import_jobs`, `pos.catalog_import_items`

## Primary use cases / APIs

- `CatalogProductUseCases`, `ProductCategoryUseCases`, `CatalogImportUseCases`
- `/api/v1/pos/catalog/products` (+ `/prices` for Today’s Prices)
- Authorization: POS product roles (catalog admin typically Owner/Manager)

## Tests

`CatalogDomainTests`, `ProductUnitConversionTests`, `PosCatalogTodaysPricesApiTests`, Platform GlobalCatalog/import suites

## MAUI

Routes: `/catalog`, product create/edit/detail, categories, Today’s Prices, import, global catalog, barcode lookup. Offline: catalog admin OnlineRequired; product create Queueable.

## React

Read-only catalog fetch for sell floor (`pos-catalog-client.ts`). No create/edit/import/Today’s Prices UI. Status: **PROVEN_PARTIAL** (read) / **MISSING** (admin).

## OWNER-CONFIRMED

Merchants include sari-sari, meat, fish, produce, rice/feed, bulk powders — catalog must support weighted + package units (already CURRENT via units model). Do not model the same physical stock as unrelated products merely to support packages.
