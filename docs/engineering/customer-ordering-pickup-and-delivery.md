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

## Personal linked-merchant storefront (V1 delivered)

Authenticated **Personal** customers may shop only at merchants they are **actively linked** to. This is not a public marketplace.

Flow:

**Linked merchants → Shop → storefront → +/- cart → review → Pickup/Delivery → `PlaceAsCustomer` → Personal My Orders / detail.**

| Topic | V1 behavior |
|---|---|
| Who can shop | Authenticated Personal actor + **ACTIVE** Personal↔seller link for that `sellerOrganizationId` |
| Seller entitlement | Enterable POS + feature `store-customer-ordering` (delivery also needs `store-delivery-orders`) |
| Catalog | Seller-org products that are `Active` + `CanBeSold` + `SellingPrice > 0` |
| Stock | Soft availability from inventory; reserve still happens on seller Accept |
| Per-product storefront flag | **Not implemented** (reported residual; no schema migration) |
| Cart | In-memory MAUI session cart (cleared after successful place / leaving merchant) |
| Quantity UX | First `+` ⇒ qty 1; `+`/`−` per product; qty 0 removes; totals recompute immediately |
| Fulfillment | Eligible Active branches; Pickup and/or Delivery; auto-select first capable branch |
| Quote / place | Server-authoritative price, delivery quote, and revalidation on place |
| Offline | Online-only storefront/place; compact offline alert; no offline customer-order queue |
| After place | Navigate to Personal order history/detail (`/personal/orders/{id}`) |

Commits: storefront UX `f689e863`; delivery-quote / active-link authorization harden `87b0acc2`.

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

V1 place creates **Submitted** orders (no long-lived Draft storefront cart required). Personal MAUI holds an in-memory cart until submit.

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

Server-authoritative quote recalculates distance and fee. Clients must not forge distance, fee, unit prices, or branch ownership. Final place revalidates independently.

Customer-facing quote (`POST .../customer-orders/organizations/{sellerOrganizationId}/quote-delivery`) requires the same Personal active-link + seller ordering capability gate as storefront/place, including `CanCustomerDelivery`. Revocation between storefront load and quote/place fails closed.

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

## Payment — keep paths distinct

### CustomerOrder / Personal storefront

- No `PaymentMethod` field or payment-method UI yet
- Submit remains **`PaymentStatus = Unpaid`**
- Do **not** claim Cash / GCash / Utang selection on Personal storefront
- No payment gateway integration for customer orders
- Existing POS Sale payment paths are unaffected

### Connected Purchase Order (separate)

- Remains a different aggregate / commerce path from `CustomerOrder`
- Cash is the default payment term
- GCash is manual / unverified
- Utang is a B2B settlement / credit term
- No automatic gateway verification
- Do **not** merge Connected PO payment semantics into Personal `CustomerOrder`

## Entitlements

Feature codes:

- `store-customer-ordering`
- `store-delivery-orders`

Enforced server-side via seller entitlement probe (Personal linked-merchant ordering capability + commercial access for seller staff paths) and `UtangCapability` (View/Manage/Place). Downgrade must disable new restricted actions without deleting historical orders, branches, policies, or snapshots.

V1 grants both codes on commercial Basic Store plans so capability checks are live; Pro-only packaging can tighten grants later without schema rewrite.

## Notifications

Seller organization inbox receives new-order notifications. Personal customer inbox wiring remains a residual for later WP polish. Deep links target order detail routes.

## Offline

Customer place, storefront, quote, and seller lifecycle mutations are online-authoritative. Show connection-required messaging rather than optimistic success. No offline customer-order queue.

## Security

- Customer sees only own party orders
- Seller operates only own organization orders
- Branch must belong to seller
- Foreign ids, forged fees/prices/distance rejected
- Personal storefront / quote / place derive from **active Personal↔merchant relationship** plus seller entitlement — not from Linked Merchants UI alone
- Direct foreign / unlinked / revoked merchant access fails closed (privacy-safe)
- Link revoked after storefront load ⇒ quote and place fail; no `CustomerOrder` or stock reservation created on denied place

## Explicit non-goals / residuals (V1)

- Guest checkout
- Courier marketplace / driver assignment / live tracking
- Polygon/barangay zones, surge, traffic pricing
- Per-product customer-storefront exposure flag / schema migration
- Personal lifecycle notification expansion
- Offline customer-order queue
- CustomerOrder payment-method design/integration
- Auto-accept
- Production readiness / Device Verified / Browser Verified claims
- Merging Connected PO payment terms into Personal CustomerOrder

## Related docs

- [Organization branches](organization-branches-and-fulfillment-locations.md)
- [Branch delivery pricing](branch-delivery-pricing.md)
- [Phase 28](../phases/phase-28-customer-ordering-pickup-and-delivery.md)
