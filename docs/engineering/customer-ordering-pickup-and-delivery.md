# Customer Ordering, Pickup & Delivery

Authoritative Stage B design for Personal and Organization customer commerce against a seller organization branch. Distinct from ConnectedPurchaseOrder (buyer org → connected supplier purchasing).

## Purpose

Enable customers to place pickup or local-delivery orders fulfilled by an active seller branch, with server-authoritative pricing, snapshots, and seller-operated fulfillment.

## Commerce boundary

| Flow | Aggregate | Direction |
|---|---|---|
| Purchasing | `ConnectedPurchaseOrder` / `PurchaseOrder` | Org buyer → connected supplier |
| Customer commerce | `CustomerOrder` | Personal or Organization customer → seller org/branch |
| Walk-in POS | `Sale` | In-store checkout (unchanged) |

Do not store customer storefront orders in ConnectedPurchaseOrder.

## Parties

`CustomerPartyType`:

- `Personal` — Platform personal user id + display name snapshot
- `Organization` — buyer organization id + public ORG###### + display name

Exactly one party applies. Guest is reserved for future extension without rewriting the aggregate shape.

Personal ordering must not grant seller organization administration.

## Fulfillment location

`OrganizationBranch` (Platform) is the only physical fulfillment location.

Each order stores:

- `SellerOrganizationId`
- `FulfillmentBranchId`
- `FulfillmentType` (`Pickup` | `Delivery`)
- Branch name snapshot

Branch must belong to the seller org, be Active (not Archived), and support the selected capability (`PickupEnabled` / `DeliveryEnabled` + valid delivery location/policy for delivery).

## Status model

Separate axes:

- **OrderStatus:** Draft, Submitted, Accepted, Rejected, Cancelled, Completed
- **FulfillmentStatus:** Pending, Preparing, Ready, OutForDelivery, Delivered, ReadyForPickup, Collected
- **PaymentStatus:** Unpaid / Pending / Paid (V1 placeholder; not equated to order status)

V1 place creates **Submitted** orders (no long-lived Draft storefront cart required).

### Seller workflow

Submitted → Accept or Reject  
Accept → Preparing  
Pickup: Preparing → ReadyForPickup → Collected → Completed  
Delivery: Preparing → Ready → OutForDelivery → Delivered → Completed  

### Cancellation (V1 conservative)

- Draft/Submitted: customer may cancel
- After Accept / Preparing+: customer cannot silently cancel
- Out for Delivery: not cancellable in V1

### Rejection

Seller may reject before accept with reason (OutOfStock, StoreTooBusy, DeliveryUnavailable, UnableToFulfill, Other). Customer sees reason when provided.

## Snapshots

At submit, lines snapshot product id, name, SKU, unit, quantity, unit price, discount, line total. Catalog price changes never rewrite history.

Delivery orders also snapshot branch identity/address/coords, recipient, destination address/coords, distance, policy fee inputs, distance charge, and final delivery fee.

## Delivery quote

Server-authoritative quote recalculates distance and fee. Clients must not forge distance, fee, unit prices, or branch ownership. Final place revalidates.

V1 distance is straight-line (Haversine). Road distance may replace the calculator later. Fee formula matches Platform `BranchDeliveryPolicy` (minimum order, max distance, free threshold, base + extra km).

## Inventory reservation

`InventoryAccount`:

- `OnHandQuantity`
- `ReservedQuantity`
- `AvailableQuantity = OnHand − Reserved`

Policy:

1. Submit — soft Available check only (no reserve)
2. Accept — atomic Reserve; order `StockReservationState = Reserved`
3. Reject/Cancel while Reserved — Release
4. Complete — ConsumeReservation (reduces Reserved and OnHand) + `StockMovement` source `CustomerOrder`

Effects are idempotent at the order reservation-state level.

## Payment

Payment remains separate. V1 does not implement GCash/card/COD gateways for customer orders. Existing POS Sale payment paths are unaffected.

## Entitlements

Feature codes:

- `store-customer-ordering`
- `store-delivery-orders`

Enforced server-side via `UtangCapability` (View/Manage/Place). Downgrade must disable new restricted actions without deleting historical orders, branches, policies, or snapshots.

V1 grants both codes on commercial Basic Store plans so capability checks are live; Pro-only packaging can tighten grants later without schema rewrite.

## Notifications

Seller organization inbox receives new-order notifications. Personal customer inbox wiring remains a residual for later WP polish. Deep links target order detail routes.

## Offline

Customer place and seller lifecycle mutations are online-authoritative. Show connection-required messaging rather than optimistic success.

## Security

- Customer sees only own party orders
- Seller operates only own organization orders
- Branch must belong to seller
- Foreign ids, forged fees/prices/distance rejected

## Explicit non-goals (V1)

- Guest checkout
- Courier marketplace / driver assignment / live tracking
- Polygon/barangay zones, surge, traffic pricing
- Full customer catalog storefront / cart UX (API place + quote exist; MAUI checkout deferred)
- Auto-accept
- Production readiness claims

## Related docs

- [Organization branches](organization-branches-and-fulfillment-locations.md)
- [Branch delivery pricing](branch-delivery-pricing.md)
- [Phase 28](../phases/phase-28-customer-ordering-pickup-and-delivery.md)
