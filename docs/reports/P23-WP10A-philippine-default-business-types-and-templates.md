# P23-WP10A — Philippine Default Business Types & Starter Templates

| Field | Value |
|---|---|
| Status | **Implemented** (default data/seed content only; WP11 not started) |
| Phase | [Phase 23](../phases/phase-23-multi-business-entitlements-and-variable-quantity-selling.md) |
| Date | 2026-08-11 |
| Device Verified | **No** |
| Production Ready | **No** |

## Status

WP10A populates the existing dynamic Platform `BusinessType` + `CatalogTemplate` + Global Catalog architecture with a strong default Philippine Pinoy Business POS setup. This is **default data / content / seeding**, not a redesign of Business Types, subscriptions, entitlement resolution, template model, catalog model, or onboarding.

## Exact 16 Business Types + codes

| # | Code | Display name | Origin |
|---|---|---|---|
| 1 | `SariSari` | Sari-Sari Store | Legacy (stable GUID preserved) |
| 2 | `MiniGrocery` | Mini Grocery | Legacy |
| 3 | `Bakery` | Bakery | Legacy |
| 4 | `Cafe` | Cafe / Coffee Shop | Legacy code kept (`Cafe`, not renamed) |
| 5 | `Pharmacy` | Pharmacy | Legacy |
| 6 | `GeneralRetail` | General Retail / Other | Legacy — fallback **choice**, not auto-assigned |
| 7 | `VegetableVendor` | Vegetable Vendor | WP10A additive |
| 8 | `FruitVendor` | Fruit Vendor | WP10A additive |
| 9 | `FishVendor` | Fish Vendor | WP10A additive |
| 10 | `MeatVendor` | Meat Vendor | WP10A additive |
| 11 | `RiceRetailer` | Rice Retailer | WP10A additive |
| 12 | `FrozenGoods` | Frozen Goods | WP10A additive |
| 13 | `Carinderia` | Carinderia / Eatery | WP10A additive |
| 14 | `StreetFoodVendor` | Street Food Vendor | WP10A additive |
| 15 | `FoodCart` | Food Cart | WP10A additive |
| 16 | `WaterRefilling` | Water Refilling Station | WP10A additive |

Stable GUID family: legacy `a1000001-…0001`–`0006`; additive `…0007`–`0010`.

## Exact 16 starter Templates

| Template | Slug | Primary BT | ~Items |
|---|---|---|---|
| Sari-Sari Starter | `sari-sari-starter` | SariSari | 22 |
| Mini Grocery Starter | `mini-grocery-starter` | MiniGrocery | 20 |
| Bakery Starter | `bakery-starter` | Bakery | 15 |
| Cafe / Coffee Shop Starter | `cafe-coffee-shop-starter` | Cafe | 15 |
| Pharmacy Starter | `pharmacy-starter` | Pharmacy | 15 |
| General Retail Starter | `general-retail-starter` | GeneralRetail | 16 |
| Vegetable Vendor Starter | `vegetable-vendor-starter` | VegetableVendor | 15 |
| Fruit Vendor Starter | `fruit-vendor-starter` | FruitVendor | 15 |
| Fish Vendor Starter | `fish-vendor-starter` | FishVendor | 15 |
| Meat Vendor Starter | `meat-vendor-starter` | MeatVendor | 15 |
| Rice Retailer Starter | `rice-retailer-starter` | RiceRetailer | 12 |
| Frozen Goods Starter | `frozen-goods-starter` | FrozenGoods | 14 |
| Carinderia / Eatery Starter | `carinderia-eatery-starter` | Carinderia | 16 |
| Street Food Starter | `street-food-starter` | StreetFoodVendor | 15 |
| Food Cart Starter | `food-cart-starter` | FoodCart | 15 |
| Water Refilling Starter | `water-refilling-starter` | WaterRefilling | 10 |

Templates remain **optional** curated starters (`SelectionMode=Curated`, Published). No second template model.

## Categories created/reused

Ensure creates/updates **19** top-level global categories when missing (by normalized name), including: Beverages, Snacks, Canned Goods, Condiments, Household Basics, Toiletries, Fresh Vegetables, Fresh Fruits, Fresh Fish, Fresh Meat, Rice, Frozen Goods, Baked Goods, Coffee & Drinks, Prepared Meals, Street Food, Water Refill, Pharmacy Basics, General Merchandise — each with multi-BT applicability.

## Product counts

| Metric | Count |
|---|---|
| Shared global products (unique SKUs) | **155** |
| Categories | **19** |
| Starter templates | **16** |

SKUs use internal codes `PH-…` (SuggestedSku). No duplicate global product per template.

## Shared-product strategy

```
Global Product (SKU)
   ↓
BusinessType M2M applicability
   ↓
CatalogTemplate product links (curation)
```

Example: `PH-BEV-WATER-500` (Bottled Water 500ml) applies to Sari-Sari, Mini Grocery, Cafe, Bakery, Pharmacy, Carinderia, Street Food, Food Cart, Frozen Goods, Rice Retailer, General Retail, Water Refilling — one row, many templates may link it.

## Selling-mode examples

| Example | Mode | Unit | Price meaning |
|---|---|---|---|
| Tomato / Bangus / Pork | `ByWeight` | `Kilogram` | PHP / kg |
| Soft Drink Can / Adobo plate / Hotdog sandwich | `PerItem` | Bottle/Can/Piece/… | PHP / unit |

No gram-as-product-unit storage for ByWeight.

## Barcode policy

- Seed barcodes are **always null** for WP10A products (fresh/local/generic safe).
- No invented GS1 numeric codes; no fake text barcodes.
- Existing barcode validators unchanged.
- SKUs remain flexible internal codes (`PH-VEG-TOMATO`, etc.).

## Price disclaimer / semantics

Starter `CostPrice` / `SellingPrice` are **editable example defaults**, not authoritative market prices. ByWeight selling price = PHP/kg. Money precision follows existing Global Catalog rules.

## Brands

Generic / Local Produce only. No fabricated branded barcode mappings; no manufacturer endorsement claim.

## Idempotent seeding behavior

`EnsurePhilippinePosStarterCatalog` (Application):

1. Ensure 16 Business Types by **code** (create or refresh name/sort/active).
2. Ensure categories by normalized name + BT applicability.
3. Ensure products by **SKU** (create/update; barcode left null on create; existing barcode preserved on update).
4. Ensure templates by **slug**; assign missing product links; Publish.
5. Safe to re-run: second run adds **0** types/products/templates/links when already present.

Wired into Local Validation dataset init (`InitializeLocalValidationDataset`) after built-in roles. Registered in Platform API DI.

## Reset / reseed behavior

| Action | Business Types | Templates / global products / categories |
|---|---|---|
| `scripts/dev/Reset-DisposableCustomerData.ps1` | **Preserved** | Deleted (disposable merchandise) |
| Local Validation API restart / seed | Additive Ensure recreates missing BTs; **reseeds** templates/products/categories | Idempotent Ensure |
| `tools/Reset-LocalValidation.ps1 -ConfirmReset` | Volume wipe → migrate + LV seed → Ensure runs | Full recreate |

**Do not** auto-run destructive reset as part of WP10A. Operator procedure:

1. Optional Development disposable reset (preserves BT definitions).
2. Restart Local Validation / run Platform API with `LocalValidation:Enabled=true` so `EnsurePhilippinePosStarterCatalog` executes.
3. Confirm 16 active BTs + 16 published `*-starter` templates in Admin.

See also [Reset-Products-And-Business-Templates.md](../../Reset-Products-And-Business-Templates.md).

## Entitlement behavior

WP03/WP04 unchanged. Ensure does **not** bypass merchant entitlement filters.

- Merchant discovery: entitled BT ∩ product/template applicability.
- Unentitled primary templates stay hidden from merchant published list.
- Platform Admin list remains unrestricted.

## Tests / counts

| Suite | Result |
|---|---|
| Unit `PhilippinePosStarterCatalogDataTests` | **5** passed |
| Unit `DynamicBusinessTypeTests` (+ Philippine 16 + name updates) | passed in focused run |
| Unit `LocalValidationOnboardingBaselineTests` (Ensure wired) | passed |
| Integration `EnsurePhilippinePosStarterCatalogTests` | **2** passed (idempotent seed + entitlement regression) |
| Integration `MerchantCatalogEntitlementFilteringTests` | **4** passed |

## Migration / schema impact

**None.** Data/seed/content only on existing models. No EF migration.

## Explicit exclusions

- WP11 onboarding / multi-BT activation UX — **not started**
- Hard-coding POS behavior by Business Type name
- Fake GS1 barcodes / branded barcode invention
- Restaurant recipe/ingredient workflows
- Production auto-`Migrate()`
- Device verification

## Implementation commit hash

`314eea90fb1ded0656ab0e5ea799ac94e8b89d10`

## Files

- `src/Platform/ExItS.Platform.Domain/GlobalCatalog/PhilippineBusinessTypeSeeds.cs`
- `src/Platform/ExItS.Platform.Domain/GlobalCatalog/BusinessType.cs` (legacy display names)
- `src/Platform/ExItS.Platform.Application/GlobalCatalog/PhilippinePosStarterCatalogData.cs`
- `src/Platform/ExItS.Platform.Application/GlobalCatalog/EnsurePhilippinePosStarterCatalog.cs`
- `src/Platform/ExItS.Platform.Application/LocalValidation/InitializeLocalValidationDataset.cs`
- `src/Platform/ExItS.Platform.Api/Program.cs`
- Tests under `tests/ExItS.Platform.UnitTests` / `IntegrationTests`
