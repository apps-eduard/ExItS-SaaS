# Phase 28 — Customer Ordering, Pickup & Delivery

[Phases](README.md) | [Portfolio](../portfolio-progress.md) | [Branch locations](../engineering/organization-branches-and-fulfillment-locations.md) | [Delivery pricing](../engineering/branch-delivery-pricing.md) | [WP01 report](../reports/P28-WP01-branch-fulfillment-location-foundation.md)

| Field | Value |
|---|---|
| Status | **Open / In Progress** — WP01 Stage A Code Complete; WP02–WP10 Stage B in progress |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |
| Related phase | Phase 27 remains **Open** |

## Goal

Deliver customer ordering with explicit branch pickup and local-delivery fulfillment, while preserving organization ownership, product database boundaries, and operator control.

## Work packages

| Work package | Scope | Status |
|---|---|---|
| **P28-WP01** | Branch & Fulfillment Location Foundation (Stage A) | **Code Complete** (`8d0be5eb`…`6feb518f`) |
| P28-WP02 | Customer ordering identity and organization storefront access | **Not Started** |
| P28-WP03 | Customer-facing catalog and branch availability | **Not Started** |
| P28-WP04 | Cart, pricing, and order quote | **Not Started** |
| P28-WP05 | Pickup ordering and readiness lifecycle | **Not Started** |
| P28-WP06 | Delivery address, serviceability, and fee quotation | **Not Started** |
| P28-WP07 | Merchant order acceptance and fulfillment operations | **Not Started** |
| P28-WP08 | Customer order tracking and notifications | **Not Started** |
| P28-WP09 | Cancellation, exceptions, audit, and privacy hardening | **Not Started** |
| P28-WP10 | E2E validation and Phase 28 closeout | **Not Started** |

## Stage A delivered

- Platform branch coordinates and pickup/delivery capability flags.
- Per-branch delivery policy and fee preview.
- Mobile-first MAUI branch list and progressive branch editor.
- Responsive Organization Web branch and fulfillment management.
- English and Filipino MAUI localization.

## Explicit exclusions

WP02–WP10 customer ordering, storefront, checkout, payment, dispatch, courier integration, route optimization, proof of delivery, live driver tracking, and production readiness are not delivered by WP01.

## Exact next

Implement Stage B starting at P28-WP02 (CustomerOrder domain + Personal/Organization party). Phase 27 remains Open.
