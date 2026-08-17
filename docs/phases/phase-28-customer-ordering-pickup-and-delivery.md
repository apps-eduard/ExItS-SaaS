# Phase 28 — Customer Ordering, Pickup & Delivery

[Phases](README.md) | [Portfolio](../portfolio-progress.md) | [Branch locations](../engineering/organization-branches-and-fulfillment-locations.md) | [Delivery pricing](../engineering/branch-delivery-pricing.md) | [Customer ordering](../engineering/customer-ordering-pickup-and-delivery.md) | [WP01 report](../reports/P28-WP01-branch-fulfillment-location-foundation.md) | [Stage B report](../reports/P28-WP02-customer-ordering-stage-b-slice.md)

| Field | Value |
|---|---|
| Status | **Open / In Progress** — WP01 Code Complete; WP02–WP09 Stage B Code Complete / Validation Pending (Personal storefront/cart UX delivered); WP10 Not Started |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |
| Related phase | Phase 27 remains **Open** |
| Personal storefront | `f689e863` (feat); quote/link authorization harden `87b0acc2` |

## Goal

Deliver customer ordering with explicit branch pickup and local-delivery fulfillment, while preserving organization ownership, product database boundaries, and operator control.

## Work packages

| Work package | Scope | Status |
|---|---|---|
| **P28-WP01** | Branch & Fulfillment Location Foundation (Stage A) | **Code Complete** (`8d0be5eb`…`6feb518f`) |
| **P28-WP02** | CustomerOrder domain + Personal/Organization party | **Code Complete** (Stage B slice) |
| **P28-WP03** | Customer-facing catalog and branch availability | **Code Complete / Validation Pending** — Personal linked-merchant storefront API + MAUI UX (`f689e863`); per-product exposure flag residual |
| **P28-WP04** | Cart, pricing, and order quote | **Code Complete / Validation Pending** — in-memory Personal MAUI cart/review + server quote/place revalidation (`f689e863`); delivery-quote active-link harden (`87b0acc2`) |
| **P28-WP05** | Pickup ordering and readiness lifecycle | **Code Complete** |
| **P28-WP06** | Delivery address, serviceability, and fee quotation | **Code Complete** (Haversine V1) |
| **P28-WP07** | Merchant order acceptance and fulfillment operations | **Code Complete** (MAUI seller UX) |
| **P28-WP08** | Customer order tracking and notifications | **Partial** — customer timeline UX; org new-order notify; personal inbox residual |
| **P28-WP09** | Cancellation, exceptions, audit, entitlement gating | **Code Complete** (conservative cancel + feature codes) |
| P28-WP10 | E2E validation and Phase 28 closeout | **Not Started** |

## Personal → Linked Merchant Shop (delivered)

Authenticated Personal users with an **active** Personal↔seller merchant link and seller `store-customer-ordering` entitlement can order without using Connected Purchase Order:

**Linked merchants → Shop → storefront → +/- cart → review → Pickup/Delivery → `CustomerOrder` place → Personal My Orders / detail.**

V1 catalog rules: seller-org `Active` + `CanBeSold` + `SellingPrice > 0`; soft stock availability; online-only; no payment-method UI (`PaymentStatus` remains Unpaid on submit). Delivery requires branch `DeliveryEnabled` **and** seller `store-delivery-orders`. Authorization is server-side (active link + entitlement); UI alone is not trusted. Storefront, delivery quote, and place fail closed for unlinked/revoked merchants.

## Stage A delivered

- Platform branch coordinates and pickup/delivery capability flags.
- Per-branch delivery policy and fee preview.
- Mobile-first MAUI branch list and progressive branch editor.
- Responsive Organization Web branch and fulfillment management.
- English and Filipino MAUI localization.

## Explicit exclusions / residuals

Not claimed Device Verified, Browser Verified, or Production Ready.

Remaining Phase 28 residuals:

- Per-product customer storefront exposure flag / schema migration
- Personal lifecycle notification expansion
- Offline customer-order queue
- CustomerOrder payment-method design/integration (Cash/GCash/Utang selection not on `CustomerOrder`)
- P28-WP10 E2E / device / browser validation and closeout

Connected Purchase Order payment terms (Cash default; GCash manual/unverified; Utang B2B settlement) remain a **separate** commerce path and must not be merged into Personal `CustomerOrder`.

## Exact next

**P28-WP10:** E2E validation, device/browser evidence, residual closeout (personal notifications, payment-method design, per-product exposure flag, migration apply evidence as needed). Phase 27 remains Open. Phase 28 remains **Open**.
