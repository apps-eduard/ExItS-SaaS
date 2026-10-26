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
| Stock | Soft availability from the **selected fulfillment branch** overlay, capped by org `AvailableQuantity` (`OnHand − Reserved`). Untracked shows `Available` with no quantity. New branches start at 0 and never inherit Main stock. Reserve still happens on seller Accept. See [product images and storefront availability](product-images-and-storefront-availability.md) and [P28-WP12](../reports/P28-WP12-multi-branch-customer-commerce-hardening.md) |
| Images | Merchant override → shared Platform template image → placeholder. DTOs carry `HasImage`/`ImageVersion`/`ImageSource` only. Authorized WebP thumb/medium; MAUI private file cache. Shared template image does not share price/SKU/stock. |
| Per-product storefront flag | **Not implemented** (reported residual; no schema migration) |
| Cart | In-memory MAUI session cart (cleared after successful place / leaving merchant) |
| Quantity UX | Connected PO stepper: unadded `[+]` only (no qty 0); first `+` ⇒ qty 1 then `[−][+] ` + Added · Qty; `−` at qty 1 removes; same `ProductId` never duplicates; out-of-stock / at-cap tracked `+` disabled; untracked has no fake max; retail `SellingPrice` |
| Fulfillment | Auto-select first eligible Active branch. One eligible branch ⇒ read-only branch text, never a blank dropdown. Selector only when more than one eligible branch. Pickup-only / Delivery-only ⇒ read-only mode; both ⇒ Pickup/Delivery toggle (default Pickup) |
| Payment | Manual V1 only: Cash (default), GCash → `ManualGCash`, Utang. `PaymentStatus` stays Unpaid on submit. No gateway/`PaymentAttempt`. Utang is **not** charged at submit; Personal Utang debt posts once at seller **Completed** via the canonical Product-Based Utang sale + credit path (idempotent; rejected/cancelled-before-completion create no debt) |
| Quote / place | Server-authoritative price, delivery quote, and revalidation on place |
| Offline | Online-only storefront/place; compact offline alert; no offline customer-order queue |
| After place | Navigate to Personal order history/detail (`/personal/orders/{id}`); Personal and seller detail show payment method + Unpaid status without implying collection |

Commits: storefront UX `f689e863`; delivery-quote / active-link authorization harden `87b0acc2`; manual CustomerOrder payment `75b12599`; storefront checkout UX `0e3825aa`.

## Fulfillment location

`OrganizationBranch` (Platform) is the only physical fulfillment location.

Each order stores:

- `SellerOrganizationId`
- `FulfillmentBranchId`
- `FulfillmentType` (`Pickup` | `Delivery`)
- Branch name snapshot

Branch must belong to the seller org, be Active (not Archived), and be **operationally** available for the selected capability (readiness + enabled + open + not paused). Pickup and delivery require explicit merchant enablement after setup.

## Fulfillment readiness and operability (P28-WP11)

| Layer | Pickup | Delivery |
|---|---|---|
| Entitlement | `store-customer-ordering` | + `store-delivery-orders` |
| Merchant intent | `PickupEnabled` | `DeliveryEnabled` (default off) |
| Setup | `CustomerOrderingReady` | + coordinates, policy, hours, phone |
| Live | `PickupOperational` | `DeliveryOperational` (includes open hours, not paused) |

Storefront status is advisory; quote and place revalidate operational state. Existing Submitted/Accepted orders continue after close/pause.

### Closed-store and scheduled orders (decision matrix)

| State | Customer may place? | Primary action | Notes |
|---|---|---|---|
| **Open** | Yes (ASAP optional preference) | Place order | Optional preferred day/period still validated against hours / lead / max advance. |
| **Closed by hours** | Yes, with valid preferred day+period | Schedule & checkout → Schedule & place order | Closed status is informational. Customer picks Today / Tomorrow / another open day and Morning / Afternoon / Evening. Submits a real `Submitted` order; seller still accepts. |
| **Seller paused online orders** | No | Continue shopping | Scheduling does **not** bypass pause. |
| **Ordering disabled / method not ready** | No | Continue shopping | Distinct from ordinary hours closure; entitlement and readiness messages apply. |

### Preferred fulfillment windows (flexible scheduling)

Customers no longer choose an exact pickup/delivery appointment clock time at Personal Soft Glass checkout.

| Step | Choices |
|---|---|
| Day | **Today**, **Tomorrow**, or **Choose another day** (Soft Glass date-only picker) |
| Period | **Morning** (before 12:00), **Afternoon** (12:00–18:00), **Evening** (after 18:00) — only periods that intersect open hours |

Rules:

- Branch-local timezone is authoritative (`PreferredFulfillmentTimeZoneId` / branch `TimeZoneId`).
- Period options are the intersection of the period window with published operating hours (including overnight spill into the next morning).
- Same-day periods respect preparation lead time; a period is offered only when at least **30 minutes** of usable intersection remains (`PreferredFulfillmentSchedule.MinUsableWindowMinutes` / frontend mirror).
- Max advance schedule days disable out-of-window calendar days; past and closed days are not selectable.
- Delivery uses the same preferred day/period model and still respects delivery readiness / areas / quote rules.
- Preference copy: the selected period is a **preference**, not a guaranteed ready time. Seller notifies when Ready.
- Persistence: `PreferredFulfillmentDate`, `PreferredFulfillmentPeriod`, timezone, and resolved usable `WindowStartUtc` / `WindowEndUtc` for validation. Do **not** invent a fake exact time such as 09:00 for Morning.
- Legacy exact-time orders keep `RequestedPickup*` / `RequestedDelivery*` snapshots readable; new preferred orders leave those null.
- Closed-by-hours place requires preferred day+period (or legacy exact date+time). ASAP empty schedule is rejected while closed.
- Seller Accept still reserves stock; **Mark Ready** remains a preparation statement (does not reserve). SignalR status events unchanged (`ExitsPosRealtimeHub`).

Server rules (`CustomerOrderPlaceBranchGate`, `PreferredFulfillmentSchedule`, legacy `RequestedFulfillmentSchedule`):

- Closed-by-hours requires preferred day+period (or legacy exact date+time); ASAP is rejected while closed.
- Branch timezone, published operating hours (including overnight windows), preparation lead, max advance, and min usable window are enforced on place.
- Missing or invalid hours **block** preferred periods (no silent allow). Closed days / unavailable periods cannot be selected.
- Delivery quote allows fee calculation when the store is closed by hours and delivery is configured; pause/disabled still fail closed.
- Cash / ManualGCash / Utang payment status on submit is unchanged (Unpaid / Pending / Unpaid). Inventory reserve remains on Accept.
- `AcceptScheduledOrdersWhileClosed` remains a seller preference field but does **not** block closed-hour scheduled checkout/place.

Personal Soft Glass cart: store closed + weekly hours (info only) + **Schedule & checkout**. Checkout requires a preferred day+period; order is submitted now for seller confirmation. Cart clearing happens only after a successful place.

### Status labels (UI — do not rename backend enums)

| Backend `CustomerOrderStatus` / fulfillment | Buyer chip | Seller chip |
|---|---|---|
| Submitted | Pending Approval | New |
| Accepted + Preparing | Preparing | Preparing |
| ReadyForPickup | Ready for Pickup | Ready |
| Ready (delivery) | Ready | Ready |
| OutForDelivery | Out for Delivery | Out for Delivery |
| Delivered / Collected / Completed / Rejected / Cancelled | Same wording | Same wording |

Compact buyer chips must **not** show “Awaiting Seller Approval”. Prefer **Pending Approval**.

### Partial fulfillment (seller line adjust — no buyer approval)

Under the checkout-disclosed partial-fulfillment policy, an authorized seller may reduce line quantities or cancel individual lines **without** buyer acceptance. Do not invent an approval thread.

| Action | Effect |
|---|---|
| Reduce quantity | `OriginalQuantity` preserved; `Quantity` lowered; shortage = cancelled qty; totals recalculated; reservation deltas released |
| Cancel item | Quantity → 0; line marked cancelled; order continues if other lines remain |
| All lines cancelled | Whole order → `Cancelled` (`CancelReason = AllItemsCancelled`) |

Rules:

- Seller cannot increase quantity, unit price, or payable total through adjust
- Paid orders: adjustment **allowed** during Submitted / Accepted / Preparing. Payment stays Paid; `AmountReceived − Total` is the explicit refund/credit obligation (existing change/overpayment field). Do not silently mark Unpaid or invent a completed refund.
- Activity events record actor, timestamp, previous/new qty, reason, note, previous/new total
- SignalR: `CustomerOrderLinesAdjusted` (plus existing cancel events when the whole order cancels)
- Buyer Soft Glass detail shows **Order Updated**, original qty as secondary text, revised total, and a simple activity timeline
- **Ready stays adjustable:** at `Ready` / `ReadyForPickup`, sellers may still Reduce Quantity or Cancel Item (customer drops or refuses lines at handoff). Adjust closes after OutForDelivery / Collected / Delivered. Mark Ready still requires at least one fulfillable line.

| Stage | Reduce / Cancel Item | Cancel entire order |
|---|---|---|
| Submitted | YES | Reject (existing) / seller cancel |
| Accepted / Preparing | YES | YES (`SellerUnableToFulfill`, …) |
| ReadyForPickup | YES (pickup handoff) | Customer refused pickup (note required) or Other |
| Ready (delivery) | YES (before dispatch) | Authorized delivery exception |
| OutForDelivery / Collected / Delivered / Completed | NO | Controlled exception / refund-return only |

Reason codes: InsufficientStock, ProductUnavailable, OutOfStock, DamagedProduct, QualityIssue, CustomerRequested, Other (Other requires a note).

### Seller approval and schedule proposals

Customer-facing wording for `Submitted` is **Pending Approval** (seller: **New**). Payment status stays separate.

Seller actions on eligible `Submitted` orders:

| Action | Effect |
|---|---|
| **Accept** | Accepts the customer's requested (or already confirmed) schedule; existing Accept → Preparing + inventory reserve. Blocked while a schedule proposal is pending. |
| **Reject** | Existing reject reasons/notes; clears any pending proposal; inventory release rules unchanged. |
| **Propose new schedule** | Persists a pending proposal on the order (`PendingScheduleProposal*`). Does **not** overwrite `RequestedPickup*` / `RequestedDelivery*`. Does **not** change order status, payment, or inventory. |

Customer response (Personal order detail):

| Action | Effect |
|---|---|
| **Accept new schedule** | Writes `ConfirmedFulfillment*`; clears pending proposal; order remains `Submitted` until seller Accept. Backend re-validates the proposed time against current branch hours / lead / max advance. Stale proposal ids fail closed. |
| **Decline** | Clears pending proposal; requested schedule remains; order stays `Submitted` for seller to propose again or reject. |

`EffectiveFulfillment*` prefers confirmed over original request for display. Proposal and response mutations are idempotent for the same proposal id; concurrent stale accepts fail. Notifications / SignalR: `CustomerOrderScheduleProposed`, `CustomerOrderScheduleAccepted`, `CustomerOrderScheduleDeclined` (existing hubs; no Redis).

Report: [P28-WP11](../reports/P28-WP11-organization-setup-and-branch-fulfillment-readiness.md). Multi-branch catalog/customer/inventory isolation: [P28-WP12](../reports/P28-WP12-multi-branch-customer-commerce-hardening.md).

Customers belong to the seller **organization**. Branch B may serve the same linked Personal customer; it does not create a per-branch customer row. Staff visibility remains role/device scoped.

## Status model

Separate axes:

- **OrderStatus:** Draft, Submitted, Accepted, Rejected, Cancelled, Completed
- **FulfillmentStatus:** Pending, Preparing, Ready, OutForDelivery, Delivered, ReadyForPickup, Collected
- **PaymentStatus:** Unpaid / Pending / Paid (not equated to order status; submit always Unpaid)
- **PaymentMethod:** Cash / ManualGCash / Utang (immutable submit-time snapshot; GCash request value stores as ManualGCash)

V1 place creates **Submitted** orders (no long-lived Draft storefront cart required). Personal MAUI holds an in-memory cart until submit.

### Seller workflow

Submitted (buyer: **Pending Approval**, seller: **New**) → Accept, Reject, or Propose schedule  
Propose schedule → customer Accept/Decline proposal (order stays Submitted) → seller Accept or Reject  
Accept → Preparing (seller may Adjust Items while unpaid and not yet out/collected)  
Pickup: Preparing → Ready for Pickup → Collected → Completed  
Delivery: Preparing → Ready → Out for Delivery → Delivered → Completed  

Schedule proposals do not add a new `CustomerOrderStatus`. Original requested times remain for audit; confirmed times live in `ConfirmedFulfillment*`.

### Cancellation cutoffs

**Buyer**

| Stage | Direct cancel? |
|---|---|
| Submitted | Yes |
| Accepted + fulfillment Pending | Yes (policy) |
| Preparing / Ready / Ready for Pickup / Out for Delivery / Collected / Delivered / Completed | No — do not render a cancel-request control without a backend action |

**Seller**

- Reject eligible Submitted orders with reason
- Cancel eligible active orders with reason (`CancelBySeller`), including **Cancel — Customer Refused** at pickup/delivery (mandatory note; inventory release via existing reserved-path; payment/refund obligation must remain safe — paid refusal still cancels the order lifecycle but does not invent automatic external refunds)
- Must not cancel Collected / Completed through ordinary actions

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

This supersedes the earlier statement that `CustomerOrder` has no `PaymentMethod`.

### CustomerOrder / Personal storefront (manual V1)

Persisted on `customer_orders.payment_method` (`Cash` default; existing rows read as Cash).

| Choice | Persist as | Meaning |
|---|---|---|
| Cash (default) | `Cash` | Seller enters amount received + change at confirm |
| GCash | `ManualGCash` | Buyer selects GCash at checkout **without** entering a reference; seller verifies receipt and enters reference + amount |
| Utang | `Utang` | Requested manual settlement; Business Utang posts on Complete for final order value |

Rules:

- Omitted/null request ⇒ Cash; invalid value ⇒ reject
- Submit remains **`PaymentStatus = Unpaid`** for all three methods (do not mark Paid because the buyer chose Manual GCash)
- Buyer must not enter a GCash transaction reference at checkout; seller `ConfirmPayment` requires reference (+ verified amount) for ManualGCash
- Duplicate confirmation with a different amount/reference is rejected; same values are idempotent no-ops
- Checkout discloses that unavailable items may be reduced/cancelled and totals adjusted per merchant policy
- Do **not** integrate `PaymentAttempt`, GCash/card gateway, QR/deep-link, or automatic Paid / automatic external refunds
- Do **not** automatically post customer debt except the existing Complete-time Utang path (recalculated for adjusted totals)
- Payment method is an immutable submit-time snapshot
- Personal Soft Glass checkout/detail and Organization seller detail keep payment status separate from order/fulfillment status

Automated settlement/payment rails and paid-line refund/credit workflows remain residuals.

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
- Automated CustomerOrder settlement/payment rails (gateway `PaymentAttempt`, automatic Paid, automatic Utang debt/ledger posting)
- Auto-accept
- Production readiness / Device Verified / Browser Verified claims
- Merging Connected PO payment terms into Personal CustomerOrder

## Related docs

- [Organization branches](organization-branches-and-fulfillment-locations.md)
- [Branch delivery pricing](branch-delivery-pricing.md)
- [Phase 28](../phases/phase-28-customer-ordering-pickup-and-delivery.md)
