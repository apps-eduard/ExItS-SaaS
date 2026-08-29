# POS-SUPPLIER-CATALOG-SHARING-PRICING-UX-01

## Status

Partial delivery of connection-level catalog sharing policy + customer discount + accept UX confirmation.
Legacy visibility preserved (`SelectedOnly` default). React shared-products density redesign and full filter/bulk polish remain follow-ups.

## CURRENT_DOMAIN_AUDIT

Prior model was **explicit SELECTED_ONLY**:
- Buyer catalog required `connected_buyer_product_shares.is_shared = true`
- Accept never auto-shared
- Effective PO price = `BuyerSpecificPoPrice ?? exposure.SupplierOrderPrice` (Default PO)
- SellingPrice was not in PO pricing path
- No connection-level sharing mode or discount columns

## SHARING_POLICY_MODEL

| Mode | Visibility |
|------|------------|
| `SelectedOnly` (0, legacy default) | Shared iff share row exists and `IsShared` |
| `AllEligible` (1) | Shared iff share is null **or** `IsShared`; `IsShared=false` = excluded |

Sparse exceptions: exclusions and fixed overrides still use share rows; AllEligible does not materialize thousands of `shared=true` rows.

## LEGACY_CONNECTION_MIGRATION

Migration `20260829120000_AddConnectedCatalogSharingPolicy`:
- `catalog_sharing_mode` int NOT NULL **default 0** (SelectedOnly)
- `customer_discount_percent` numeric(5,2) NULL

Existing relationships stay SelectedOnly → **LEGACY_CONNECTION_VISIBILITY_PRESERVED=PASS**

## EFFECTIVE_PRICE_PRECEDENCE

Server `ConnectedPoPricing.TryResolveEffectivePrice`:

1. Product override (`BuyerSpecificPoPrice`) → `ProductOverride`
2. Else baseline = SellingPrice if &gt; 0 else exposure `SupplierOrderPrice`
3. If connection `CustomerDiscountPercent` &gt; 0 → discount on baseline → `CustomerDiscount`
4. Else baseline → `SellingPrice` or `DefaultPoPrice`

Money: `decimal.Round(..., 2, AwayFromZero)`.

## SHARED_PRICE_UNIT_SEMANTICS

Buyer effective price applies to the **exposure orderable unit** (`UnitOfMeasureCode` / Default PO unit). Package multipliers on buyer links remain inventory conversion only; they do not redefine the published PO unit price.

## BUYER_PROJECTION

Buyer catalog DTO still exposes effective order price via mapped exposure price; cost, inventory qty, and discount formula are not added.

## PO_PRICE_SNAPSHOT

Unchanged: connected PO lines store `UnitPriceSnapshot` at submit; later catalog changes do not rewrite historical lines. Draft revalidate compares live effective vs client snapshot.

## RESPONSIVE_UX

- Incoming accept: catalog setup step with AllEligible (default) / SelectedOnly + optional discount; **Accept & start sharing** required (`ConfirmCatalogSharing`).
- Shared-products page density/filters (Excluded/Overrides) and catalog-settings page: backend APIs exist; full UI redesign is NEXT.

## APIs

- `POST .../relationships/{id}/approve` body: `catalogSharingMode`, `customerDiscountPercent`, `confirmCatalogSharing`
- `GET/PUT .../relationships/{id}/catalog-settings`

## Validation notes

Run targeted Connected* unit tests after build. Full React suite / e2e not claimed in this partial package.
