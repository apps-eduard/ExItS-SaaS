# Phase 27 — Connected Supplier Commerce & Purchasing

[Phases](README.md) | [Portfolio](../portfolio-progress.md) | [Design](../engineering/connected-exits-suppliers.md) | [P27-WP01](../reports/P27-WP01-buyer-specific-product-sharing-and-po-pricing.md) | [WP02](../reports/P27-WP02-connected-po-delivery-and-reliability.md) | [WP03](../reports/P27-WP03-supplier-response-synchronization.md) | [WP04](../reports/P27-WP04-connected-po-cancellation-and-withdrawal.md) | [WP05](../reports/P27-WP05-fulfillment-goods-receipt-and-discrepancies.md)

| Field | Value |
|---|---|
| Status | **Open / In Progress** — P27-WP01 through P27-WP05 Code Complete; WP06–WP07 Not Started |
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
| **P27-WP01** | Buyer-Specific Product Sharing & PO Pricing (+ Level-1 + Level-2 mobile bulk UX hardening) | **Code Complete + Level-1 + Level-2 Mobile UX Hardening Complete** ([report](../reports/P27-WP01-buyer-specific-product-sharing-and-po-pricing.md)) |
| P27-WP02 | Connected PO Delivery & Reliability | **Code Complete** ([report](../reports/P27-WP02-connected-po-delivery-and-reliability.md)) |
| P27-WP03 | Supplier Response Synchronization | **Code Complete** ([report](../reports/P27-WP03-supplier-response-synchronization.md)) |
| P27-WP04 | Connected PO Cancellation & Withdrawal | **Code Complete** ([report](../reports/P27-WP04-connected-po-cancellation-and-withdrawal.md)) |
| P27-WP05 | Fulfillment, Goods Receipt & Discrepancies | **Code Complete** ([report](../reports/P27-WP05-fulfillment-goods-receipt-and-discrepancies.md)) |
| P27-WP06 | Connected Purchasing UX & Notifications | **Not Started** |
| P27-WP07 | E2E Hardening & Phase 27 Closeout | **Not Started** |

**Current / active work package:** **P27-WP06 — Connected Purchasing UX & Notifications** (Not Started). P27-WP01–WP05 are Code Complete; owner device/browser validation remains outstanding. Do not begin WP06 or WP07 unless explicitly authorized.

## Invariants

- **EXPOSABLE ≠ SHARED.** Product-level availability to connected buyers does not grant catalog visibility; per-relationship sharing is required.
- Effective connected PO price = buyer-specific override → Default PO Price. Retail `SellingPrice` is never a runtime fallback.
- Accepting a connection does not silently share products; post-accept confirmation (Share all / Review products / Not now) is required.
- Newly exposable products are not auto-shared to existing buyers.
- Per-buyer product management is a **mobile-first bulk** experience (search, filters, category sheet, multi-select, bulk share/unshare/price with preview) — not one-by-one save-all editing.
- Level-1 eligibility management is also **mobile-first bulk** (Catalog → Connected Buyer Availability → Enable/Disable/Default PO) — Product Edit remains secondary.
- Buyer inventory changes only through Goods Receipt / Receive Purchase Order — never on expose, share, price change, connection Accept/Decline, PO draft, PO submit, or supplier Accept/Decline of a connected PO.
- Connected PO lifecycle is `New → Accepted → Preparing → Fulfilled`, with terminal `Declined` or `Withdrawn`; buyer withdrawal is permitted only from `New`.
- Supplier Preparing/Fulfilled never mutates buyer inventory. During receipt, only good quantity enters stock; damaged/rejected/short-closed quantities do not.
- A short-closed completed receipt is displayed as **Received With Issues**.
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

**P27-WP06 — Connected Purchasing UX & Notifications** when authorized. P27-WP01–WP05 require owner device/browser validation; Phase 27 remains Open and is not Production Ready.
