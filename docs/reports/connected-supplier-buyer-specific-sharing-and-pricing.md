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

### Level 1 — global eligibility / block (supplier-wide)

- Active catalog products are **eligible by default** (`IsBlockedFromConnectedBuyers = false`, `CanExposeToConnectedBuyers = true`).
- Optional **global block** hides a product from all connected-buyer Level-2 sharing without deleting share rows.
- `CatalogProduct.DefaultConnectedPoPrice` is optional until first share; it is **never** silently copied from retail `SellingPrice`.
- Synced to `SupplierProductExposure` only when allowed **and** Default PO is set (`IsExposed` + `SupplierOrderPrice` = Default PO Price).
- Primary MAUI path: **Catalog → Connected Buyer Availability** (bulk Allow / Block / Default PO Price).
- Product create/edit: default allowed; optional “Block from connected buyers”; Default PO always optional.

**GLOBALLY ALLOWED ≠ SHARED.** Eligibility alone does not make a product visible to any buyer. Bulk Level-1 Allow does **not** create `ConnectedBuyerProductShare` rows.

### Default PO Price (first share)

Default PO may be staged anytime (including while blocked). The first time a product is shared to a buyer without a Default PO, the supplier must confirm `EstablishDefaultPoPrice` in the same save as the share (exposure + share atomic). Runtime order pricing never falls back to `SellingPrice`.

### Level 2 — per-buyer sharing

New entity `ConnectedBuyerProductShare`:

- Unique `(RelationshipId, SupplierProductId)`
- `IsShared`
- `BuyerSpecificPoPrice` (nullable)

Manage Products lists **all active** supplier products (including blocked, for UI disable), left-joined to shares/exposures.

### Effective PO price

1. Buyer-specific PO price, if set  
2. Else Default PO Price (`SupplierProductExposure.SupplierOrderPrice`)  
3. Never retail `SellingPrice`

### Connection acceptance

Accept only activates the relationship. MAUI opens a share prompt with **eligible (non-blocked) active products**. Sharing persists only after Confirm (and may collect Default PO for unconfigured products). **Not now** leaves Active with zero new assignments.

New products after connection remain **Not shared** until the supplier explicitly shares them.

## Schema / migration

| Migration | Detail |
|---|---|
| `20260816070746_AddConnectedBuyerProductSharingAndPricing` | `can_expose` / `default_connected_po_price`; `connected_buyer_product_shares`; backfilled exposed products + Active-relationship shares |
| `20260816205520_AddConnectedBuyerGlobalBlock` | `is_blocked_from_connected_buyers`; default-eligible model |

### Global-block backfill (`AddConnectedBuyerGlobalBlock`)

| Legacy state | Result |
|---|---|
| `can_expose=false` **and** exposure row exists | **Blocked** (intentional historical disable) |
| `can_expose=false`, **no** exposure row | **Eligible** (unconfigured → default allowed) |
| `can_expose=true` | **Eligible** |

Ambiguous staged-price-never-enabled rows (price set, no exposure, can_expose=false) are treated as **eligible** (not blocked). Default PO is preserved.

`can_expose` is kept as the inverse of the block flag for API compatibility (`can_expose = NOT is_blocked`). Column default for `can_expose` becomes **true**.

Global block deactivates supplier-wide exposure; buyer browse/revalidate fail closed even if share rows remain. Unblock restores prior explicit share intent without creating new shares.

## API changes

| Route | Role |
|---|---|
| `GET /catalog/products/connected-buyer-availability` | Level-1 paged eligibility list (search, category, available/not) |
| `POST /catalog/products/connected-buyer-availability/bulk` | Bulk Enable/Disable (IDs or SelectAllMatching) |
| `POST /catalog/products/connected-buyer-availability/pricing/preview` | Bulk Default PO preview (retail baseline) |
| `POST /catalog/products/connected-buyer-availability/pricing/apply` | Apply Default PO pricing rule |
| `POST /connected-suppliers/exposures` | Level 1 supplier-wide expose/update (legacy single-product path) |
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

- Catalog hub tile → `/catalog/connected-buyer-availability` (global Allow/Block + optional Default PO)
- Catalog create/edit/detail: default allowed; optional block; Default PO optional
- Accept → `/share-products` (Confirm / Review / Not now); missing Default PO collected before share
- Manage: `/shared-products` lists all active products; first-share / bulk Default PO sheets when needed
- Browse: only products explicitly shared to that buyer

## UX hardening notes

### Level 2 — Manage Products (default-eligible)

No Level-1 visit required before share. First share may establish Default PO inline. Bulk share returns `NeedsDefaultPo` for products missing a confirmed Default PO.

### Level 1 — Connected Buyer Availability (global restriction)

Reframed as optional global block + optional Default PO administration. Allow/Block labels. Bulk Allow creates zero shares.

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
