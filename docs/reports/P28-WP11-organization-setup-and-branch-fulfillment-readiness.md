# P28-WP11 — Organization Setup + Branch Fulfillment Readiness

| Field | Value |
|---|---|
| Status | **Code Complete / Validation Pending** |
| Migration | `20260818085609_AddBranchFulfillmentReadiness` |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Capability model (separate concerns)

| Concept | Meaning |
|---|---|
| `CanUseCustomerOrdering` / `CanUseDelivery` | Subscription entitlement permits capability; never auto-enables merchant settings |
| `CustomerOrderingEnabled` / `PickupEnabled` / `DeliveryEnabled` | Explicit merchant intent per branch |
| `CustomerOrderingReady` / `PickupReady` / `DeliveryReady` | Server-evaluated setup completeness |
| `CustomerOrderingOperational` / `PickupOperational` / `DeliveryOperational` | Ready + enabled + branch Active + within operating hours + not paused |

Delivery defaults **OFF** for new and main branches. Subscription upgrade makes delivery *available* (`CanUseDelivery`) but does not set `DeliveryEnabled`. Downgrade stops new delivery orders but **retains** hours, coordinates, policy, and merchant configuration.

## Operating hours

- Stored per branch in `branch_operating_hours` (Mon–Sun, closed / 24h / one interval per day).
- Evaluated in branch **effective timezone** (`branch.TimeZoneId` override, else organization profile timezone).
- Never evaluated against client/device timezone.
- Server exposes open/closed state and compact status messages for merchant UI and storefront.

## Pause online orders

- Branch flag `OnlineOrdersPaused` with optional `PauseReason` (`TooBusy`, `ClosingEarly`, `Emergency`, `FulfillmentUnavailable`, `Other`).
- Blocks **new** online customer orders only; walk-in POS and in-flight Submitted/Accepted orders continue.

## Readiness evaluator

`BranchFulfillmentReadinessEvaluator` returns structured `MissingRequirements[]` and stable `ReasonCodes[]` (e.g. `store_hours_missing`, `map_location_missing`, `delivery_policy_incomplete`).

Enablement is gated in `UpdateBranchFulfillmentSettings`; `UpdateBranch` no longer toggles pickup/delivery directly.

## Multi-branch isolation

Readiness is computed per branch in list/detail APIs. Configuring Main does not satisfy Branch B.

## Storefront / quote / place

- Storefront shows advisory open/paused/closed status; browsing allowed while closed/paused.
- `QuoteCustomerOrderDelivery` and `PlaceCustomerOrder` revalidate operational state, entitlements, hours, and delivery policy at execution time.

## Existing organizations

No invented default hours. Missing data ⇒ **Setup required**; delivery activation and new delivery placement blocked until ready.

## UI

- **MAUI:** dense branch editor (`BranchEdit.razor`) — compact setup rows, expandable details/address/hours/fulfillment, sticky save. See [P28 branch-edit UX densification](P28-branch-edit-ux-densification.md).
- **Organization Web:** same hierarchy with a wider form grid and sticky Save.

## Tests (Release)

- Platform: `BranchFulfillment*` + `BranchListBulk*` — **12 passed**
- POS: `CustomerOrdering*` filter — **54 passed**

## Explicit exclusions

- Holiday/special-date hours
- Scheduled auto-resume for pause
- Device/browser verification
- Production security hardening beyond existing Stage B posture
