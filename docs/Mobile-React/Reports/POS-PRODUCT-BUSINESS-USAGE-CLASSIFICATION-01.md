# POS-PRODUCT-BUSINESS-USAGE-CLASSIFICATION-01

## Summary

Buyer-owned product business usage classification for PinoyBusinessPOS:

| UI | Code | Sell floor | Connected share |
|----|------|------------|-----------------|
| Sell as-is | `Resale` | Yes (when Active + other rules) | Eligible |
| Ingredient / raw material | `Ingredient` | No | Excluded |
| Internal use | `InternalUse` | No | Excluded |

## Design decisions

```
PRODUCT_USAGE_MODEL=ProductBusinessUsage mapped onto existing ProductUsageCapabilities
PRODUCT_USAGE_VALUES=Resale | Ingredient | InternalUse
LEGACY_USAGE_DEFAULT=RESALE (existing BuyAndSell / CanBeSold=true products)
USAGE_INVENTORY_COUPLING=SEPARATE (InventoryAccount.IsTracked unchanged)
SELLABILITY_SOURCE_OF_TRUTH=CanBeSold (derived from ProductBusinessUsage; no new IsSellable boolean)
SELL_FLOOR_FILTER=server CatalogProductFilter.CanBeSold=true + client belt-and-suspenders
SERVER_SALE_GUARD=SaleUseCases rejects !CanBeSold (ApplicationErrorCodes.SaleProductNotSellable)
TODAYS_PRICES_BEHAVIOR=list with canBeSold=true
CONNECTED_CATALOG_ELIGIBILITY=CanBeSold && !IsBlockedFromConnectedBuyers (AllEligible bootstrap skips non-resale)
SUPPLIER_ONBOARDING_USAGE_REQUIRED=YES (CreateBuyerProductAndLink requires BusinessUsage / usage flags)
DIRECT_BUY_USAGE=N/A (Direct Buy receives existing products only; no create path)
NORMAL_PO_USAGE=all usages purchasable (no Resale-only restriction)
GOODS_RECEIPT_USAGE=all usages receivable under existing stock semantics
OFFLINE_USAGE_SUPPORT=OFFLINE_SCHEMA_VERSION=7; businessUsage on cached PosCatalogProductDto; sell list requests canBeSold=true
FUTURE_PRODUCED_ITEM_EXTENSION=MadeProduct preset already exists for produced sellables; do not add ProducedItem business-usage value until separately approved
```

### Existing model (no duplicate column)

```
EXISTING_PRODUCT_USAGE_MODEL=ProductUsageCapabilities (CanBePurchased/CanBeSold/CanBeUsedAsIngredient/IsProduced + UsagePreset)
EXISTING_SELLABLE_FLAG=CanBeSold
EXISTING_INVENTORY_TRACKING_FLAG=InventoryAccount.IsTracked
```

`ProductBusinessUsage` is the buyer-facing classification. It maps to presets:

- Resale → BuyAndSell
- Ingredient → Ingredient
- InternalUse → InternalUse (new preset; purchasable, not sold, not ingredient)

No new `business_usage` DB column — flags + `usage_preset` already persist. DTO exposes computed `businessUsage`.

### Explicit non-goals

- Recipes / manufacturing / automatic consumption
- Automatic expense posting for InternalUse
- ProducedItem as a fourth business-usage choice (extension point documented only)

## Migration

```
MIGRATION=NONE (no schema change; InternalUse uses usage_preset + flags)
MIGRATION_APPLIED_LOCAL=N/A
LEGACY_USAGE_DEFAULT=RESALE
```

## Validation notes

Targeted domain/unit tests cover classification mapping and InternalUse validity.
React: product-business-usage unit tests; sell/catalog/prepare wiring.
Full solution suite / lint not claimed green unless recorded in commit report.
