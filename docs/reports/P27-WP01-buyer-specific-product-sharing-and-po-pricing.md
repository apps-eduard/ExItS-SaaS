# P27-WP01 — Buyer-Specific Product Sharing & PO Pricing

Package: **P27-WP01 — Buyer-Specific Product Sharing & PO Pricing**
Phase: [Phase 27 — Connected Supplier Commerce & Purchasing](../phases/phase-27-connected-supplier-commerce-and-purchasing.md)
Design: [connected-exits-suppliers.md](../engineering/connected-exits-suppliers.md)
Companion detail: [connected-supplier-buyer-specific-sharing-and-pricing.md](connected-supplier-buyer-specific-sharing-and-pricing.md)

## Status

**Code Complete + Level-1 + Level-2 Mobile UX Hardening Complete.** Phase 27 remains **Open / In Progress**. WP02–WP07 **Not Started**. This UX hardening is **not** P27-WP02.

| Gate | Value |
|---|---|
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

**Starting SHA (original WP01):** `62b73307c753388d0006acc8d580db52dc483434`
**Level-2 UX hardening starting SHA:** `8e2ef20e5bb3ef9be42404fc8e983b1355691880`
**Level-1 bulk availability UX hardening starting SHA:** `ba0d616ef39044cb77b5ec960b065398a6cd8f4a`
**Feature tip (docs hash record):** `cbd0005bf811c183f361444884dc328b40d0e393`
**Roadmap registration tip:** `22c8b6d5`

## Implementation commits

| SHA | Message |
|---|---|
| `25a33bf5` | `feat(suppliers): add buyer-specific product sharing and PO pricing` |
| `2b79c41d` | `feat(maui): add connected buyer product sharing workflow` |
| `9988fb16` | `test(suppliers): cover connected buyer sharing and pricing rules` |
| `337df110` | `docs(suppliers): document buyer-specific sharing and pricing` |
| `cbd0005b` | `docs(suppliers): record buyer-sharing feature commit hashes` |
| `4b67ce0a` | `feat(suppliers): add safe bulk buyer sharing and pricing operations` |
| `5b4cb814` | `feat(maui): add bulk connected buyer product management` |
| `84d3d688` | `test(suppliers): cover bulk buyer product management` |
| `22be62cf` | `docs(p27): document WP01 mobile sharing UX hardening` |

## Delivered capability

### Level 1 — product eligibility

- `CatalogProduct.CanExposeToConnectedBuyers` + `DefaultConnectedPoPrice`
- Synced to supplier-wide `SupplierProductExposure` (Default PO Price = `SupplierOrderPrice`)
- First enable initializes Default PO Price from retail `SellingPrice`; afterward independent
- Primary MAUI: **Catalog → Connected Buyer Availability** (bulk Enable / Disable / Default PO with preview)
- Secondary: MAUI catalog create/edit/detail B2B section (EN + fil-PH)

### Level 2 — per-buyer sharing

- `ConnectedBuyerProductShare` (relationship + product uniqueness; `IsShared`; optional `BuyerSpecificPoPrice`)
- Post-accept share prompt: all exposable products selected by default; Confirm / Not now
- Connected buyer → Shared products management (filters, toggles, buyer-specific prices)
- Buyer Browse returns only shared + exposable + orderable products with effective price

## UX hardening (2026-08-16 follow-up — not WP02)

Mobile bulk product management replaced the cumbersome one-by-one save-all editor while preserving WP01 domain rules.

### Delivered UX

- Post-Accept lightweight card: Share all N / Review products / Confirm & share / Not now (Accept still creates **zero** shares until Confirm)
- Single reusable Manage Products screen for Accept→Review and Connected Buyers → Manage
- Search + chips (All / Shared / Not shared / Custom price) + category bottom sheet
- Multi-select, select visible, select-all matching (distinct from page selection)
- Sticky bulk Share / Unshare / Price actions
- Bulk price preview (Use default / % discount / amount adjust from **Default PO only**) then Apply
- Individual exception editor via product bottom sheet

### API / application additions (no new migration)

- Query buyer-product-shares with paging/filters (`query`, `category`, `shareFilter`, `page`, `pageSize`)
- Bulk mutate Share/Unshare (`SelectAllMatching` or product IDs) — fail closed on foreign/non-exposable IDs
- Pricing preview + apply endpoints
- Legacy full-list GET retained when no filter/page params are supplied

### UX hardening commits

| SHA | Message |
|---|---|
| `4b67ce0a` | `feat(suppliers): add safe bulk buyer sharing and pricing operations` |
| `5b4cb814` | `feat(maui): add bulk connected buyer product management` |
| `84d3d688` | `test(suppliers): cover bulk buyer product management` |
| `22be62cf` | `docs(p27): document WP01 mobile sharing UX hardening` |

## Level 1 Bulk Availability UX Hardening — 2026-08-16

Follow-up only — **not** P27-WP02. Domain semantics unchanged.

### Problem

Suppliers with large catalogs were forced through Product → Edit → “Available to connected buyers” one-by-one.

### Delivered UX

- Catalog hub tile → `/catalog/connected-buyer-availability`
- Mobile list: search, All / Available / Not available chips, category sheet
- Multi-select, select visible, select-all matching
- Sticky Enable / Disable / PO Price
- Default PO bulk pricing with preview (set from retail / % discount / adjust amount / fixed price)
- Product create/edit B2B fields retained as secondary one-off path

### Locked rules preserved

- First Enable (`false→true`) with null Default PO → initialize from **that product's** SellingPrice
- Later retail changes do not rewrite Default PO
- Disable / re-enable preserves Default PO
- Bulk Enable does **not** overwrite existing Default PO
- Bulk Enable creates **zero** `ConnectedBuyerProductShare` rows (Exposable ≠ Shared)
- Runtime pricing remains BuyerSpecific → Default PO (never SellingPrice)
- No inventory mutation; no migration

### API / application additions (no migration)

- `GET /api/v1/pos/catalog/products/connected-buyer-availability`
- `POST .../connected-buyer-availability/bulk`
- `POST .../connected-buyer-availability/pricing/preview`
- `POST .../connected-buyer-availability/pricing/apply`

### Level-1 UX hardening commits

| SHA | Message |
|---|---|
| `b6a34d92` | `feat(catalog): add bulk connected buyer availability management` |
| `230dad4f` | `feat(maui): add mobile bulk B2B catalog availability UX` |
| `a8832f46` | `test(catalog): cover bulk connected buyer availability rules` |
| `36a08afa` | `docs(p27): document Level 1 bulk availability UX` |

### Pricing & PO safety (WP01-required)

- Effective price: buyer-specific → Default PO Price; never retail runtime fallback
- Catalog search / link / revalidate enforce shares server-side
- Connected PO submit pre-validates shares + effective prices and snapshots effective price on CPO lines (no silent Ordered-without-CPO for connected suppliers)
- Inventory invariant preserved (no stock on expose/share/price/Accept/Decline)

### Persistence

Migration: `20260816070746_AddConnectedBuyerProductSharingAndPricing`

Backfill: existing exposed products → product flags; Active relationships → explicit shares (preserve historical visibility; not future buyers).

## Explicit exclusions

- P27-WP02+ scopes (broader delivery/sync, cancel/withdraw UX, fulfillment redesign, purchasing notification package, phase closeout) except WP01-required atomic submit safety already delivered
- Org Web per-buyer share/price UI (documented gap; MAUI is source of truth)
- Marketplace, payments, AP/invoicing, shipping, live-stock sharing, auto-receive
- Device / Browser / Production Ready claims

## Validation evidence

| Suite | Result |
|---|---|
| Unit (`CatalogDomain` + `ConnectedBuyerAvailability` + `BuyerProductShareBulk` + ConnectedSuppliers) | **36 + 35 passed** (targeted), 0 failed |
| MAUI guards (ConnectedBuyer / Catalog / ConnectedBuyerAvailability) | **20 passed**, 0 failed |
| Migration | **No** (Level-1 bulk UX) |

Device Verified: **No**. Browser Verified: **No**. Production Ready: **No**.

## Risks / open decisions

- Org Web share management parity deferred
- Owner must validate Catalog → Connected Buyer Availability → Accept → share prompt → Browse/pricing on device
- Remaining connected-PO delivery/sync gaps tracked under P27-WP02+

## Exact next

**P27-WP02 — Connected PO Delivery & Atomic Submission** when explicitly authorized. Do not begin WP03–WP07 as part of WP02 unless authorized.

## Portfolio independence

Platform/product database boundaries remained unchanged; no secrets were committed.
