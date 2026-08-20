# Customer Ordering, Pickup, and Delivery

## Dependency (CURRENT + OWNER)

```text
Organization
  → Branch
    → Address / Coordinates / Hours
      → Fulfillment settings (pickup/delivery)
        → Fulfillment readiness
          → Customer storefront / ordering
```

## CURRENT — Platform branch fulfillment

| Capability | Status | Evidence |
|------------|--------|----------|
| Pickup / delivery flags | PROVEN_CURRENT | fulfillment-settings |
| Readiness calculation | PROVEN_CURRENT | fulfillment-readiness |
| Delivery policy / fee preview | PROVEN_CURRENT | delivery-policy, delivery-fee-preview |
| Operating hours impact | PROVEN_CURRENT | operating-hours + readiness |
| Haversine serviceability | PROVEN_CURRENT | Platform + POS distance calculators |

MAUI configuration: `BranchEdit.razor`. React configuration: **MISSING**.

## CURRENT — Customer ordering (POS)

| Capability | Status | Evidence |
|------------|--------|----------|
| Storefront exposure | PROVEN_CURRENT | CustomerOrderEndpoints storefront |
| Linked merchant requirement | PROVEN_CURRENT | Personal linked-merchants shop |
| Cart / quote / stock revalidation | PROVEN_CURRENT | quote + reservation semantics |
| Pickup / delivery | PROVEN_CURRENT | order fulfillment modes |
| Manual payment methods | PROVEN_CURRENT | order payment fields |
| Merchant acceptance / lifecycle | PROVEN_CURRENT | seller orders MAUI `/orders*` |
| Customer tracking / cancellation / notifications | PROVEN_CURRENT | buyer `/personal/orders*` + notifications |
| Inventory reservation | PROVEN_CURRENT | `ReservedQuantity` |
| Offline | OnlineRequired residual | policy fail-closed |

APIs:

- `/api/v1/pos/organizations/{org}/customer-orders`
- `/api/v1/pos/customer-orders` (+ storefront, quote-delivery)

## React

Entire ordering/delivery/storefront: **MISSING**.

## OWNER-CONFIRMED

Delivery activation requires valid business/branch operational setup (location, hours, fulfillment config). Preserve branch-level behavior.
