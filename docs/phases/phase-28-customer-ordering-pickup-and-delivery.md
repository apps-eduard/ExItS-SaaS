# Phase 28 — Customer Ordering, Pickup & Delivery

[Phases](README.md) | [Portfolio](../portfolio-progress.md) | [Branch locations](../engineering/organization-branches-and-fulfillment-locations.md) | [Delivery pricing](../engineering/branch-delivery-pricing.md) | [Customer ordering](../engineering/customer-ordering-pickup-and-delivery.md) | [WP01 report](../reports/P28-WP01-branch-fulfillment-location-foundation.md) | [Stage B report](../reports/P28-WP02-customer-ordering-stage-b-slice.md)

| Field | Value |
|---|---|
| Status | **Open / In Progress** — WP01 Code Complete; WP02–WP09 Stage B Code Complete / Validation Pending; WP10 Not Started |
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
| **P28-WP02** | CustomerOrder domain + Personal/Organization party | **Code Complete** (Stage B slice) |
| **P28-WP03** | Customer-facing catalog and branch availability | **Partial** — API place uses live catalog; storefront UI deferred |
| **P28-WP04** | Cart, pricing, and order quote | **Partial** — server quote + place; MAUI cart/checkout deferred |
| **P28-WP05** | Pickup ordering and readiness lifecycle | **Code Complete** |
| **P28-WP06** | Delivery address, serviceability, and fee quotation | **Code Complete** (Haversine V1) |
| **P28-WP07** | Merchant order acceptance and fulfillment operations | **Code Complete** (MAUI seller UX) |
| **P28-WP08** | Customer order tracking and notifications | **Partial** — customer timeline UX; org new-order notify; personal inbox residual |
| **P28-WP09** | Cancellation, exceptions, audit, entitlement gating | **Code Complete** (conservative cancel + feature codes) |
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

P28-WP10: E2E validation, storefront/checkout residuals, personal notifications, migration apply evidence. Phase 27 remains Open.
