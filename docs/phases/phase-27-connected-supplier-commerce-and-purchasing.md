# Phase 27 — Connected Supplier Commerce & Purchasing

[Phases](README.md) | [Portfolio](../portfolio-progress.md) | [Design](../engineering/connected-exits-suppliers.md) | [P27-WP01](../reports/P27-WP01-buyer-specific-product-sharing-and-po-pricing.md)

| Field | Value |
|---|---|
| Status | **Open / In Progress** — P27-WP01 Code Complete + UX Hardening Complete; WP02–WP07 Not Started |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |
| Closeout | **Not started** |

## Goal

Complete Connected ExItS supplier commerce for PinoyBusinessPOS: product eligibility and per-buyer sharing/pricing, safe connected purchase-order delivery, supplier response synchronization, cancellation/withdrawal, fulfillment/goods-receipt alignment, purchasing UX/notifications, and phase closeout hardening.

This phase builds on Phase 1 connected-supplier foundations ([connected-exits-suppliers-phase-1](../reports/connected-exits-suppliers-phase-1.md)) without claiming Device / Browser / Production Ready.

## Work packages

| Work package | Scope | Status |
|---|---|---|
| **P27-WP01** | Buyer-Specific Product Sharing & PO Pricing (+ mobile bulk UX hardening) | **Code Complete + UX Hardening Complete** ([report](../reports/P27-WP01-buyer-specific-product-sharing-and-po-pricing.md)) |
| P27-WP02 | Connected PO Delivery & Atomic Submission | **Not Started** |
| P27-WP03 | Supplier Accept/Decline Synchronization | **Not Started** |
| P27-WP04 | PO Cancellation & Withdrawal | **Not Started** |
| P27-WP05 | Fulfillment & Goods Receipt Flow | **Not Started** |
| P27-WP06 | Connected Purchasing UX & Notifications | **Not Started** |
| P27-WP07 | E2E Hardening & Phase 27 Closeout | **Not Started** |

**Current / active work package:** P27-WP01 (domain + mobile bulk sharing UX delivered; owner device validation still outstanding). Do **not** begin WP02–WP07 unless explicitly authorized, except for fixes strictly required to keep WP01 correct and safe.

## Invariants

- **EXPOSABLE ≠ SHARED.** Product-level availability to connected buyers does not grant catalog visibility; per-relationship sharing is required.
- Effective connected PO price = buyer-specific override → Default PO Price. Retail `SellingPrice` is never a runtime fallback.
- Accepting a connection does not silently share products; post-accept confirmation (Share all / Review products / Not now) is required.
- Newly exposable products are not auto-shared to existing buyers.
- Per-buyer product management is a **mobile-first bulk** experience (search, filters, category sheet, multi-select, bulk share/unshare/price with preview) — not one-by-one save-all editing.
- Buyer inventory changes only through Goods Receipt / Receive Purchase Order — never on expose, share, price change, connection Accept/Decline, PO draft, PO submit, or supplier Accept/Decline of a connected PO.
- No full supplier catalog offline download; selective linked-product projection only.
- No cross-product DB access; POS holds connected-supplier operational data.
- No marketplace discovery, inter-org payments, AP/invoicing, shipping/logistics, live-stock sharing, or auto-receive in this phase unless a later WP explicitly authorizes it.

## Explicit exclusions (phase-level)

Marketplace, inter-org payments, AP/invoices, live stock sharing, logistics/shipping, images in sync, Redis/message brokers, auto-accept, auto-receive, full offline supplier catalog, SignalR bell push (unless later authorized), Org Web parity for every MAUI surface (document gaps per WP).

## Related docs

- [connected-exits-suppliers.md](../engineering/connected-exits-suppliers.md)
- [offline-sync-design.md](../engineering/offline-sync-design.md) (linked-product delta)
- [purchasing-inventory-ux-mental-model.md](../engineering/purchasing-inventory-ux-mental-model.md)
- [connected-exits-suppliers-phase-1.md](../reports/connected-exits-suppliers-phase-1.md)
- [connected-supplier-browse-linked-products-ux.md](../reports/connected-supplier-browse-linked-products-ux.md)
- [connected-supplier-buyer-specific-sharing-and-pricing.md](../reports/connected-supplier-buyer-specific-sharing-and-pricing.md) (engineering detail companion to P27-WP01)

## Exact next

Owner device validation of P27-WP01 (Accept → share prompt → Manage Products bulk share/price → Browse). Then **P27-WP02 — Connected PO Delivery & Atomic Submission** when authorized (WP01 already includes atomic submit safety for connected lines; WP02 covers remaining delivery/sync gaps).
