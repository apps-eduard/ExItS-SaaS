# P28-WP02–WP09 — Customer Ordering Stage B Slice

| Field | Value |
|---|---|
| Status | **Code Complete / Validation Pending** |
| Phase | Phase 28 — Open |
| Starting SHA (Stage B) | `3ca38d43610bf831d65eecbf96524a19263f247a` |
| Feature commit(s) | `30f4fa93` domain · `2488afcb` api/persist · `5c0215c0` maui · `4b39d4d8` tests · `d692772d` docs |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Assignment

Implement Personal/Organization `CustomerOrder` with pickup and delivery fulfillment on top of Stage A fulfillment locations, without reusing ConnectedPurchaseOrder.

## Delivered capability

- POS `CustomerOrder` aggregate: party model, order/fulfillment/payment statuses, line + delivery snapshots, cancel/reject rules, stock reservation state
- Inventory `ReservedQuantity` / Available; Accept reserves; Reject/Cancel releases; Complete consumes
- Delivery quote with Haversine distance + fee formula aligned to Platform policy
- POS API seller + customer endpoints with idempotency on place/accept/reject/complete
- Entitlements `store-customer-ordering` / `store-delivery-orders` + role matrix
- MAUI dense seller order list/detail and personal order list/timeline/detail
- EN + fil-PH order strings
- Engineering design doc

## Architecture decisions

- Orders live in `PosDbContext`; branches/policies remain Platform-owned
- Server prices products at place; client unit prices are not trusted
- Soft stock check on submit; hard reservation on Accept
- Payment status axis exists but V1 does not add new payment rails

## Explicit exclusions / residuals

- Full customer catalog/cart/checkout MAUI storefront
- Personal inbox notifications (org new-order notification present)
- Organization-buyer membership verification hardening
- Platform fee-preview HTTP call from POS (local formula duplicate documented)
- Pro-only grant tightening (codes granted on Basic Store V1)
- Courier/driver, zones, auto-accept, Guest
- Migration apply/rollback evidence, device/browser verification
- WP10 E2E closeout

## Persistence

Migration `20260816104401_AddCustomerOrdersAndInventoryReservation` adds customer order tables and `inventory_accounts.reserved_quantity`.

## Tests (pre-commit evidence)

- CustomerOrder + InventoryReservation unit tests: **20 passed** (earlier filtered run)
- MAUI CustomerOrder + BranchFulfillment guards: **5 passed**
- Broader suite counts recorded at commit time

## Security

Seller org scope, customer party scope, entitlement gates, idempotent lifecycle. Not production-secure by itself.

## Exact next

P28-WP10 E2E validation and residual closeout (checkout UX, personal notifications, migration apply evidence). Phase 27 remains Open.
