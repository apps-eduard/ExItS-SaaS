# POS-BRANCH-FULFILLMENT-SETUP-01

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**TASK:** POS-BRANCH-FULFILLMENT-SETUP-01  
**START_SHA:** `f0783a4741d8f3fcea464d534bfec1841b98b8fd`

## Goal

Upgrade Organization → Branch Fulfillment into a production-quality setup experience, preserving the authoritative Platform readiness architecture, and add city/municipality-level delivery service areas so DeliveryEnabled alone cannot imply unrestricted delivery.

## Architecture audited (prior)

| Layer | Existing (reused) |
|-------|-------------------|
| Platform readiness | `BranchFulfillmentReadinessEvaluator`, `CanUse*`, `*Ready`, `*Operational`, MissingRequirements, ReasonCodes, pause, open-now |
| Branch settings | `UpdateBranchFulfillmentSettings` with `BranchFulfillmentNotReady` guards |
| Hours / policy / coords | `BranchOperatingHours`, `BranchDeliveryPolicy`, WGS84 coords, Haversine distance |
| React | List/edit pages, readiness labels, hours helpers — **server remains authoritative** |
| POS customer order | Storefront DTO, quote, place, delivery snapshot, fee calculator |

`READINESS_RULES_DUPLICATED_IN_REACT=NO` — React renders server fields / setup summary only.

## Final behavior

### List page

- Pickup / Delivery **switches** outside navigation links (no nested interactives).
- ON gated by `PickupReady` / `DeliveryReady` (+ entitlement for delivery).
- OFF always allowed; mutations not fake-optimistic; failures revert UI.
- Incomplete: “Complete setup first”; no plan: localized not-included wording.

### Detail / setup tabs

Overview · Branch details · Operating hours · Delivery location · Delivery policy · Delivery areas  

Server-owned section completion icons via setup summary fields.

### Setup progress model

`BranchFulfillmentSetupSummary` (server):

| Channel | Sections |
|---------|----------|
| Pickup | 2 — Branch details, Operating hours |
| Delivery | 5 — those two + Location, Policy, Areas |

Enablement still requires `PickupReady` / `DeliveryReady`, not percentage alone.

### Delivery service areas

- Domain: `BranchDeliveryServiceArea` (city/municipality V1 only).
- Migration: `20260830212443_AddBranchDeliveryServiceAreas` → `platform.branch_delivery_service_areas`.
- API: GET/POST/DELETE `.../branches/{branchId}/delivery-service-areas`.
- ListBranches batch-loads hours, policies, area counts/areas + one entitlement snapshot (no N+1 readiness GETs from React).

### Readiness

- `DeliveryReady` requires ≥1 active service area (`delivery_area` / `delivery_area_missing`).
- Pickup / CustomerOrderingReady **do not** require delivery areas.

### Checkout / quote / place

- Storefront exposes public `deliveryServiceAreas`.
- Customer selects configured area (`deliveryServiceAreaId`); free-text city cannot authorize delivery.
- Quote + Place validate area belongs to branch (active), then distance/policy.
- Order snapshot stores canonical city/municipality name (no hard FK to mutable area row).

## Eligibility model

```
DELIVERY_ELIGIBILITY_MODEL=
ENTITLEMENT
+ BRANCH_READINESS
+ OWNER_ENABLED
+ ONLINE_ORDER_OPERATIONAL
+ CONFIGURED_SERVICE_AREA
+ DISTANCE_POLICY

PICKUP_DOES_NOT_REQUIRE_DELIVERY_AREA=YES
```

## Security / tenancy

- Area CRUD scoped to org membership + Manage Branch Fulfillment.
- Cross-org / cross-branch / inactive / missing area IDs rejected on quote and place.
- Distance remains enforced after area match.

## Tests

| Suite | Result |
|-------|--------|
| Platform readiness / areas / list bulk | 21 PASS |
| POS CustomerOrdering unit | 101 PASS |
| React Vitest | TOTAL=1353 PASS=1353 FAIL=0 |
| Typecheck | PASS |
| ESLint | 0 errors (baseline warnings only) |
| React production build | PASS |
| Platform + POS API Release build | PASS |

## Deferred

Barangay areas, polygons, Google Maps/geocoding, road routing, riders/tracking, time slots, zone pricing, org-wide delivery kill switch, B2B checkout, payments, offline/device packages.

## Manual acceptance

Operator path documented in task §33 (Joe store / Main Branch). Automated coverage verifies readiness gates, area CRUD uniqueness, quote/place area+distance, and React list switches. Live UI walkthrough deferred to operator environment with Delivery entitlement.
