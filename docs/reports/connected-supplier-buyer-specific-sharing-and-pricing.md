# Connected supplier buyer-specific sharing and pricing

Date: 2026-08-16  
Starting SHA: `62b73307c753388d0006acc8d580db52dc483434`  
**Canonical WP report:** [P27-WP01-buyer-specific-product-sharing-and-po-pricing.md](P27-WP01-buyer-specific-product-sharing-and-po-pricing.md)  
Phase: [Phase 27](../phases/phase-27-connected-supplier-commerce-and-purchasing.md)  
Related design: [connected-exits-suppliers.md](../engineering/connected-exits-suppliers.md)  
Phase 1 baseline: [connected-exits-suppliers-phase-1.md](connected-exits-suppliers-phase-1.md)

## Status

**Code Complete (P27-WP01).** Phase 27 **Open / In Progress**. Not Device Verified. Not Browser Verified. **Not Production Ready.**

This file retains engineering-detail Q&A and migration notes. Status and roadmap authority live on the P27-WP01 report and phase page.

## Implementation commits

| SHA | Message |
|---|---|
| `25a33bf5` | `feat(suppliers): add buyer-specific product sharing and PO pricing` |
| `2b79c41d` | `feat(maui): add connected buyer product sharing workflow` |
| `9988fb16` | `test(suppliers): cover connected buyer sharing and pricing rules` |
| `337df110` | `docs(suppliers): document buyer-specific sharing and pricing` |

## Business model

### Level 1 — product eligibility (supplier-wide)

- `CatalogProduct.CanExposeToConnectedBuyers`
- `CatalogProduct.DefaultConnectedPoPrice`
- Synced to `SupplierProductExposure` (`IsExposed` + `SupplierOrderPrice` = Default PO Price)

**EXPOSABLE ≠ SHARED.** Eligibility alone does not make a product visible to any buyer.

### Default PO Price initialization

When availability is first enabled and `DefaultConnectedPoPrice` is null, it is initialized from current retail `SellingPrice`. After that, Default PO Price is independent — later retail price changes do not rewrite it. Runtime order pricing never falls back to `SellingPrice`.

### Level 2 — per-buyer sharing

New entity `ConnectedBuyerProductShare`:

- Unique `(RelationshipId, SupplierProductId)`
- `IsShared`
- `BuyerSpecificPoPrice` (nullable)

### Effective PO price

1. Buyer-specific PO price, if set  
2. Else Default PO Price (`SupplierProductExposure.SupplierOrderPrice`)  
3. Never retail `SellingPrice`

### Connection acceptance

Accept only activates the relationship. MAUI opens a share prompt with **all currently exposable products selected by default**. Sharing persists only after Confirm. **Not now** leaves Active with zero new assignments.

Future products made exposable later are **not** auto-shared to existing buyers.

## Schema / migration

Migration: `20260816070746_AddConnectedBuyerProductSharingAndPricing`

| Change | Detail |
|---|---|
| `pos.products` | `can_expose_to_connected_buyers` (bool, default false), `default_connected_po_price` (nullable) |
| `pos.connected_buyer_product_shares` | new table + unique relationship/product |

### Backward compatibility

1. Existing `IsExposed` exposures → product flags + Default PO Price backfilled from `supplier_order_price`.
2. Every **Active** relationship receives explicit `IsShared=true` shares for currently exposed products (preserve historical visibility).
3. Future Active relationships do **not** inherit shares automatically.

Disabling product exposability deactivates the supplier-wide exposure; buyer browse/revalidate fail closed (Unavailable) even if a share row remains.

## API changes

| Route | Role |
|---|---|
| `POST /connected-suppliers/exposures` | Level 1 supplier-wide expose/update |
| Legacy `POST /relationships/{id}/exposures` | Guidance error (no longer creates ambiguous relationship-scoped exposure) |
| `GET /relationships/{id}/buyer-product-shares` | Eligible exposures + share state (legacy full list when unfiltered) |
| `GET /relationships/{id}/buyer-product-shares?query&category&shareFilter&page&pageSize` | Paged supplier Manage Products query + category facets |
| `GET /relationships/{id}/eligible-products` | Same for post-accept UI |
| `PUT /relationships/{id}/buyer-product-shares` | Batch share/unshare + buyer prices |
| `POST /relationships/{id}/buyer-product-shares/confirm` | Confirm selected products after Accept |
| `POST /relationships/{id}/buyer-product-shares/bulk` | Bulk Share/Unshare (IDs or SelectAllMatching) |
| `POST /relationships/{id}/buyer-product-shares/pricing/preview` | Bulk pricing preview (Default PO baseline) |
| `POST /relationships/{id}/buyer-product-shares/pricing/apply` | Apply previewed pricing rule |
| `GET /relationships/{id}/catalog` | **Only shared** products; `SupplierOrderPrice` = effective price |

## MAUI

- Catalog create/edit/detail: Available to connected buyers + Default PO Price
- Accept → lightweight `/share-products` card (Share all / Review / Confirm & share / Not now)
- Manage: `/shared-products` mobile bulk list (search, share chips, category sheet, multi-select, sticky Share/Unshare/Price + preview)
- Browse empty copy: supplier hasn’t shared with your business yet

## UX hardening note (2026-08-16)

P27-WP01 follow-up only — **not** WP02. Domain rules unchanged (Exposable ≠ Shared; buyer price → Default PO; Accept ≠ auto-share).
## Organization Web

Backend rules apply. Org Web Connected buyers UI does **not** yet include share/price management (gap). Suppliers must use MAUI for sharing UX.

## Offline / PO

- Selective linked-product offline cache unchanged (no full catalog download).
- Revalidate uses effective price; unshare / non-exposable → Unavailable.
- Connected PO submit validates links + shares + effective prices **before** Ordering; CPO lines snapshot effective price in the same submit path (no silent Ordered-without-CPO).
- External supplier PO flow unchanged.
- Inventory never mutates on expose/share/price/Accept/Decline — only Goods Receipt.

## Security / IDOR

Relationship ownership + org direction validated on share/catalog/link/revalidate/submit paths. Buyer A cannot read Buyer B assignments by guessing IDs.

## Tests (this validation pass)

| Suite | Result |
|---|---|
| Unit (`ConnectedSupplier` + `CatalogDomain`) | **53 passed**, 0 failed |
| MAUI guards (ConnectedBuyer / Catalog / Browse) | **15 passed**, 0 failed |
| Integration migration test | present (`AddConnectedBuyerProductSharingAndPricingMigrationTests`) |

Device Verified: **No**. Browser Verified: **No**. Production Ready: **No**.

## Explicit Q&A

| Question | Answer |
|---|---|
| Does accepting a connection automatically share products? | **No.** Prompt defaults selection; confirmation required. |
| What is selected by default after Accept? | All products already marked available to connected buyers. |
| Can supplier deselect before confirming? | **Yes.** |
| Different prices for Buyer A vs B? | **Yes** (buyer-specific override). |
| No override price? | Default PO Price. |
| Does SellingPrice stay synced to PO price? | **No.** |
| Is SellingPrice a runtime fallback? | **No.** |
| Future newly-exposable products auto-shared? | **No.** |
| Inventory from exposure/share/pricing/Accept? | **No.** Goods Receipt only. |

## Exact next

Owner device validation of Accept → share prompt → Browse visibility + buyer-specific prices. Optional Org Web share UI when authorized.
