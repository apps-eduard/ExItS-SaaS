# P27-WP01 — Buyer-Specific Product Sharing & PO Pricing

Package: **P27-WP01 — Buyer-Specific Product Sharing & PO Pricing**  
Phase: [Phase 27 — Connected Supplier Commerce & Purchasing](../phases/phase-27-connected-supplier-commerce-and-purchasing.md)  
Design: [connected-exits-suppliers.md](../engineering/connected-exits-suppliers.md)  
Companion detail: [connected-supplier-buyer-specific-sharing-and-pricing.md](connected-supplier-buyer-specific-sharing-and-pricing.md)

## Status

**Code Complete + UX Hardening Complete.** Phase 27 remains **Open / In Progress**. WP02–WP07 **Not Started**. This UX hardening is **not** P27-WP02.

| Gate | Value |
|---|---|
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

**Starting SHA (original WP01):** `62b73307c753388d0006acc8d580db52dc483434`
**UX hardening starting SHA:** `8e2ef20e5bb3ef9be42404fc8e983b1355691880`
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

## Delivered capability

### Level 1 — product eligibility

- `CatalogProduct.CanExposeToConnectedBuyers` + `DefaultConnectedPoPrice`
- Synced to supplier-wide `SupplierProductExposure` (Default PO Price = `SupplierOrderPrice`)
- First enable initializes Default PO Price from retail `SellingPrice`; afterward independent
- MAUI catalog create/edit/detail B2B section (EN + fil-PH)

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

Recorded after push in the Implementation commits table below.

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
| Unit (`ConnectedSupplier` + `CatalogDomain` filters) | **53 passed**, 0 failed |
| MAUI guards (ConnectedBuyer / Catalog / Browse-related) | **15 passed**, 0 failed |
| Integration migration (`AddConnectedBuyerProductSharing*`, connected suppliers) | **2 passed**, 0 failed |

Full-solution Android packaging may still hit local SDK/AAR environment issues unrelated to this WP.

## Risks / open decisions

- Org Web share management parity deferred
- Owner must validate Accept → share prompt → Browse/pricing on device
- Remaining connected-PO delivery/sync gaps tracked under P27-WP02+

## Exact next

**P27-WP02 — Connected PO Delivery & Atomic Submission** when explicitly authorized. Do not begin WP03–WP07 as part of WP02 unless authorized.

## Portfolio independence

No HealthCare tree; Platform/Product DB boundaries unchanged; no secrets committed.
