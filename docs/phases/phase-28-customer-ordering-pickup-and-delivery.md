# Phase 28 — Customer Ordering, Pickup & Delivery

[Phases](README.md) | [Portfolio](../portfolio-progress.md) | [Branch locations](../engineering/organization-branches-and-fulfillment-locations.md) | [Delivery pricing](../engineering/branch-delivery-pricing.md) | [Customer ordering](../engineering/customer-ordering-pickup-and-delivery.md) | [WP01 report](../reports/P28-WP01-branch-fulfillment-location-foundation.md) | [Stage B report](../reports/P28-WP02-customer-ordering-stage-b-slice.md)

| Field | Value |
|---|---|
| Status | **Open / In Progress** — WP01 Code Complete; WP02–WP09 Stage B Code Complete / Validation Pending; WP11–WP15A Code Complete / Validation Pending (WP15A docs-only); WP10 Not Started |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |
| Related phase | Phase 27 remains **Open** |
| Personal storefront | `f689e863` (feat); quote/link authorization harden `87b0acc2`; docs `7f3b0d4f`; manual payment `75b12599`; storefront UX `0e3825aa` |

## Goal

Deliver customer ordering with explicit branch pickup and local-delivery fulfillment, while preserving organization ownership, product database boundaries, and operator control.

## Work packages

| Work package | Scope | Status |
|---|---|---|
| **P28-WP01** | Branch & Fulfillment Location Foundation (Stage A) | **Code Complete** (`8d0be5eb`…`6feb518f`) |
| **P28-WP02** | CustomerOrder domain + Personal/Organization party | **Code Complete** (Stage B slice) |
| **P28-WP03** | Customer-facing catalog and branch availability | **Code Complete / Validation Pending** — Personal linked-merchant storefront API + MAUI UX (`f689e863`); org product images + customer-facing available stock (`5083076f` / `95276a8e`); shared Platform template images + org-safe adoption/overrides (`957ab6f4` / `3611665d`); per-product exposure flag residual |
| **P28-WP04** | Cart, pricing, and order quote | **Code Complete / Validation Pending** — in-memory Personal MAUI cart/review + server quote/place revalidation (`f689e863`); delivery-quote active-link harden (`87b0acc2`); manual CustomerOrder payment method (`75b12599` / `0e3825aa`); tracked cart cap + structured stock-changed place errors (`5083076f` / `95276a8e`) |
| **P28-WP05** | Pickup ordering and readiness lifecycle | **Code Complete** |
| **P28-WP06** | Delivery address, serviceability, and fee quotation | **Code Complete** (Haversine V1) |
| **P28-WP07** | Merchant order acceptance and fulfillment operations | **Code Complete** (MAUI seller UX) |
| **P28-WP08** | Customer order tracking and notifications | **Partial** — customer timeline UX; org new-order notify; personal inbox residual |
| **P28-WP09** | Cancellation, exceptions, audit, entitlement gating | **Code Complete** (conservative cancel + feature codes) |
| **P28-WP11** | Organization setup + branch fulfillment readiness | **Code Complete / Validation Pending** — see [report](../reports/P28-WP11-organization-setup-and-branch-fulfillment-readiness.md) |
| **P28-WP12** | Multi-branch customer commerce hardening | **Code Complete / Validation Pending** — feat `69111d45`; see [report](../reports/P28-WP12-multi-branch-customer-commerce-hardening.md) |
| **P28-WP13** | Branch operational context + owner switching | **Code Complete / Validation Pending** — feat `ed75c827`; see [report](../reports/P28-WP13-branch-operational-context-and-owner-switching.md) |
| **P28-WP14** | Unified organization + branch workspace selection | **Code Complete / Validation Pending** — see [report](../reports/P28-WP14-unified-organization-branch-workspace-selection.md) |
| **P28-WP15A** | Organization/branch capability + client boundary baseline | **Docs Complete** — see [report](../reports/P28-WP15A-capability-client-boundary-baseline.md) |
| P28-WP10 | E2E validation and Phase 28 closeout | **Not Started** |

## Personal → Linked Merchant Shop (delivered)

Authenticated Personal users with an **active** Personal↔seller merchant link and seller `store-customer-ordering` entitlement can order without using Connected Purchase Order:

**Linked merchants → Shop → storefront → +/- cart → review → Pickup/Delivery → manual payment method → `CustomerOrder` place → Personal My Orders / detail.**

V1 catalog rules: seller-org `Active` + `CanBeSold` + `SellingPrice > 0`; soft stock availability; online-only. Storefront product rows follow Connected PO stepper UX (unadded `[+]`; added `[−][+] ` + Added · Qty; unavailable has a disabled `[+]`, never a fake qty 0). Review is Sales-style read-focused summary (back to Shop for quantity edits). Branch/fulfillment auto-select: one eligible branch is filled as read-only text; selector appears only when more than one eligible branch exists; Pickup/Delivery toggle appears only when both modes are available (default Pickup).

**Personal CustomerOrder V1 settlement (manual only):** Cash is the default; GCash persists as `ManualGCash` (manual/unverified, no QR/gateway/`PaymentAttempt`); Utang is a requested manual settlement method (no automatic customer debt or Business Utang ledger posting). `PaymentStatus` remains **Unpaid** on submit. Do not treat any method as collected at place. This supersedes the earlier Phase 28 statement that `CustomerOrder` has no `PaymentMethod`.

Delivery requires branch `DeliveryEnabled` **and** seller `store-delivery-orders`. Authorization is server-side (active link + entitlement); UI alone is not trusted. Storefront, delivery quote, and place fail closed for unlinked/revoked merchants.

## Stage A delivered

- Platform branch coordinates and pickup/delivery capability flags.
- Per-branch delivery policy and fee preview.
- Mobile-first MAUI branch list and densified progressive branch editor (sticky save, compact hours, expandable fulfillment).
- Responsive Organization Web branch and fulfillment management.
- English and Filipino MAUI localization.

## Explicit exclusions / residuals

Not claimed Device Verified, Browser Verified, or Production Ready.

Remaining Phase 28 residuals:

- Per-product customer storefront exposure flag / schema migration
- Personal lifecycle notification expansion
- Offline customer-order queue
- Automated CustomerOrder settlement/payment rails (gateway `PaymentAttempt`, automatic Paid, automatic Utang debt/ledger posting)
- P28-WP10 E2E / device / browser validation and closeout

Connected Purchase Order payment terms (Cash default; GCash manual/unverified; Utang B2B settlement) remain a **separate** commerce path and must not be merged into Personal `CustomerOrder`.

## Exact next

**P28-WP10:** E2E validation, device/browser evidence, residual closeout (personal notifications, per-product exposure flag, automated settlement/payment rails, migration apply evidence as needed). Phase 27 remains Open. Phase 28 remains **Open**.
