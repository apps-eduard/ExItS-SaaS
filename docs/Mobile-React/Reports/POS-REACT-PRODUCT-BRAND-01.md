# POS-REACT-PRODUCT-BRAND-01

Add first-class **Brand** support to the POS product catalog.

| Field | Value |
| --- | --- |
| Status | **Complete** |
| Branch | `feat/organization` |
| Start SHA | `9941e4788c2aa34b18f482bcbc1e0158010bc23f` (after onboarding/migrate fix) |

## Model

| Concept | Meaning |
| --- | --- |
| **Category** | WHAT kind of product it is (classification) |
| **Brand** | WHICH commercial brand the product belongs to |
| **Supplier** | WHERE the organization buys from (unchanged purchasing) |

Example:

- Coca-Cola 1.5L → Category = Beverages, Brand = Coca-Cola, Supplier = ABC Wholesale
- Nestlé Fresh Milk → Category = Dairy, Brand = Nestlé
- Nescafé Classic → Category = Beverages, Brand = Nescafé

Same Brand may appear across many Categories. There is **no** Brand↔Category or Brand↔Supplier ownership FK.

## Decisions

| Flag | Value |
| --- | --- |
| `BRAND_REQUIRED` | **NO** — products may have `BrandId = null` |
| `BRAND_CATEGORY_OWNERSHIP` | **NO** |
| `BRAND_SUPPLIER_OWNERSHIP` | **NO** |
| `BRAND_ORG_SCOPED` | **YES** — each org owns its brand list |
| `BRAND_LOGO` | **DEFERRED** |
| `MANUFACTURER_MODEL_ADDED` | **NO** |
| `SUBCATEGORY_ADDED` | **NO** |
| `SELL_BRAND_FILTER` | **DEFERRED** — sell search matches brand name; no extra filter chips |
| `INVENTORY_BRAND_DISPLAY` | **DEFERRED** — inventory list DTO has no brand; catalog owns brand |
| `BRAND_PRODUCT_COUNT` | **DEFERRED** — avoid N+1 counts on Brands page |
| `PRODUCT_IMPORT_BRAND` | **DEFERRED** — Platform brand strings remain display-only on import |

## Backend

- Domain: `ProductBrand` / `ProductBrandId` / `ProductBrandStatus` (Active/Inactive), mirror Category normalize + deactivate
- Table: `pos.product_brands`; nullable `pos.products.brand_id` (Restrict FK)
- Unique: Active-only `(organization_id, normalized_name)`
- API: `/api/v1/pos/catalog/brands` (list/get/create/update/deactivate/reactivate)
- Products: `brandId` on create/update; list filter `brandId`; search matches brand `NormalizedName`
- DTO: `brandId` + `brandName` (batch resolve, no N+1)
- Capability: same as Category (`ViewCatalog` / `ManageCatalog`)
- Migration: `20260828172131_AddProductBrands`

## React

- Product create/edit: Brand select + inline quick-add (auto-select; friendly duplicate)
- Product list: brand meta + Category/Brand filters
- `CatalogBrandsPage` at `/catalog/brands`
- Today's Prices: subtle `brandName`
- i18n: en, fil-PH, ceb-PH, ilo-PH, hil-PH

## Non-goals

- Brand logo / manufacturer / subcategory
- Changing selling price, inventory qty, or supplier model via Brand
- Offline brand mutation store
