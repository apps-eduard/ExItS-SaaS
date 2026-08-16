# Connected ExItS Suppliers — Phase 1 Design

Phase marker: `connected-exits-suppliers-phase-1`  
Status: **Code Complete (Phase 1 foundation)** — see [completion report](../reports/connected-exits-suppliers-phase-1.md)  
Commerce follow-on: **Phase 27 Open / In Progress** — [phase](../phases/phase-27-connected-supplier-commerce-and-purchasing.md); P27-WP01–WP05 Code Complete ([WP01](../reports/P27-WP01-buyer-specific-product-sharing-and-po-pricing.md), [WP02](../reports/P27-WP02-connected-po-delivery-and-reliability.md), [WP03](../reports/P27-WP03-supplier-response-synchronization.md), [WP04](../reports/P27-WP04-connected-po-cancellation-and-withdrawal.md), [WP05](../reports/P27-WP05-fulfillment-goods-receipt-and-discrepancies.md))
Starting SHA: `f67c2763c3e30a7a2b77847f23a101f57c403ca6`

## Architecture audit (authoritative)

### Existing supplier model
- Org-owned `Supplier` master data (`Active`/`Inactive`), scoped by `PosOrganizationId`
- No linked ExItS organization field today
- Online-only MAUI (`/suppliers*`)

### Existing purchase-order state machine
`Draft` → `Ordered` → `PartiallyReceived` / `Received`; terminal `Cancelled`  
**Inventory is affected only on buyer Goods Receipt** (`PurchaseStockService`), never on draft/create/submit/cancel/supplier acceptance.

### Existing local-first / outbox
Encrypted `offline_operations` queue + local projections for customers/credits/cash sales.  
Purchasing routes are **OnlineRequired** today (`PosOfflineCapabilityPolicy`). Phase 1 extends LocalStore for **linked supplier products** and **offline PO drafts** without inventing a second sync stack.

### Existing local MAUI persistence
SQLite LocalStore schema **v9** (product units); selling catalog uses full-replace page sync. Connected suppliers use **selective linked-product** tables + delta cursor (v8), with conversion metadata columns added in v9.

### Existing catalog pagination/search
Page-based `ListProductsAsync` (`PosPagination`: default 20, max 100). Connected supplier catalog search reuses page+search conventions with a **hard max of 50** for supplier catalog pages.

### Organization-to-organization relationships
**None** historically as a general Platform capability. Phase 1 introduces an explicit buyer↔supplier relationship aggregate in the POS database (Platform org GUIDs as values only; no cross-DB FK). Public Personal / Organization identity QR codes do **not** create supplier relationships — buyer↔supplier links remain org↔org commercial relationships only (see [personal-organization-identity-boundaries.md](../architecture/personal-organization-identity-boundaries.md)).

**Connect UX / server rule:** requesting a connected supplier requires the supplier **Business QR** / `ORG######`. Personal QR and POS device-registration QR are rejected in MAUI and in `RequestConnection` (Guid-only is no longer accepted).

## Connected Buyer vs Connected Supplier vs Customer

| Term | Meaning |
|---|---|
| **Connected Supplier** | Who supplies **my** Organization (buyer-side directory under Suppliers) |
| **Connected Buyer** | Who buys **from** my Organization through an Active supplier connection (supplier-side directory) |
| **Customer** | Seller-owned sales/customer master (Utang/credit ledger). Separate from Connected Buyer |

**Critical:** Accepting a Connected Supplier request creates / activates `ConnectedSupplierRelationship` only. It does **not** create a Customer, merge Customer records, create Business Utang, or change inventory. Explicit “Add as customer” remains deferred.

Supplier-side MAUI: `/suppliers/connected/buyers` (+ detail). Org Web: `/suppliers/buyers`. Active-only in both UIs; Pending stays under Requests.


```text
Supplier Organization
    | exposed products / server search
    v
ExItS POS API
    | paged results + delta linked-product sync
    v
Buyer MAUI local DB (~linked products only)
    | offline linked products
    v
Local PO Draft
    | reconnect + revalidate
    v
Submitted Connected PO
    v
Supplier Incoming Orders
```

### Scaling example
| Scope | Count |
|---|---:|
| Supplier catalog | 100,000 |
| Buyer linked | ~68 |
| Offline projection | ~68 (not 100,000) |
| Online search page | 25 (max 50) |
| Sync | changed linked products only |

## Aggregates (Phase 1)

| Aggregate | Purpose |
|---|---|
| `ConnectedSupplierRelationship` | Explicit buyer↔supplier approval (`Pending`/`Active`/`Declined`/`Disconnected`) |
| `Supplier` (additive) | `ConnectionType` External \| ConnectedOrganization + optional relationship id |
| `SupplierProductExposure` | Supplier-controlled orderable projection + **supplier order price** (not POS selling price) |
| `BuyerSupplierProductLink` | Stable buyer product ↔ supplier product link (extensible for future UOM conversion) |
| `ConnectedPurchaseOrder` | Supplier-visible incoming order correlated to buyer PO |

## Inventory invariant
Supplier Accept / Decline **must never** mutate buyer on-hand. Automated tests enforce this.

## LocalStore schema v8 / v9

Schema v8 adds four per-user/per-organization SQLite tables:

- `local_connected_supplier` — relationship status and optional buyer `SupplierId` attachment
- `local_linked_supplier_product` — selective buyer-linked products only, with relationship/name/SKU/version indexes
- `local_connected_supplier_sync_state` — per-relationship delta cursor and last-sync timestamp
- `local_connected_po_draft` — device-local draft JSON with `LocalEntitySyncState`; a local save is never supplier submission

Schema **v9** adds `multiplier_to_base` / `package_label` on linked products so buyer purchase-unit conversion metadata syncs offline. Bag/case ↔ base conversion is supported via product units + link multipliers (see [product-units-and-inventory-behavior.md](product-units-and-inventory-behavior.md)).

`LinkedSupplierProductSyncService` requests `/links/sync?sinceVersion=…`, applies changed and removed link IDs, then advances the local cursor. It does not call catalog search and must never download a complete supplier catalog. Catalog and linked-product commercial fields are plain-text merchant product data, matching the existing local selling-catalog pattern. Outbox payload encryption remains unchanged; Phase 1 drafts are local-only and are not added to the outbox.

## MAUI routes

- `/suppliers` — supplier list with Pending/Connected/Declined relationship status + incoming-request count badge
- `/suppliers/connected/request` — request an organization connection (online)
- `/suppliers/connected/requests` — **supplier-side** incoming connection requests (Accept / Decline)
- `/suppliers/connected/buyers` — **supplier-side** Active connected buyers (not Customers)
- `/suppliers/connected/buyers/{relationshipId}` — connected buyer relationship detail
- `/suppliers/{id}/connected-catalog?relationshipId=…` — **Browse products** (online only; **Active** only)
- `/suppliers/{id}/linked-products?relationshipId=…` — **Linked products** selective local projection (offline capable)
- `/purchasing/new` — external supplier picker remains unchanged; connected suppliers use online catalog search or local linked products offline
- `/connected-suppliers/incoming` — supplier **incoming purchase orders** with Accept / Decline (online) — distinct from connection requests

### Buyer browse / linked UX (MAUI)

| Screen | Behavior |
|---|---|
| **Browse products** | Online only. Returns products that are **globally exposable**, **explicitly shared to this relationship**, orderable, and have a valid effective PO price. Empty = this supplier has not shared products with your business yet. Search optional. Link and use maps a shared item to a buyer catalog product. |
| **Linked products** | Device cache of **explicitly linked** items only. Never downloads the full supplier catalog. |
| **Product create/edit** | Available to connected buyers + Default PO Price (initialized from SellingPrice only on first enable; then independent). Secondary one-off path. |
| **Catalog → Connected Buyer Availability** | Primary Level-1 mobile bulk Enable/Disable/Default PO Price (search, availability chips, category, select-all matching, price preview). Does **not** create buyer shares. |
| **Accept connection** | Activates relationship only, then opens share prompt with all exposable products selected by default. Confirm persists shares; Not now shares nothing. |
| **Connected buyer → Manage products** | Mobile bulk share/unshare/price (search, filters, category, select-all matching, price preview). |

`relationshipId` may be omitted on buyer entry; screens resolve `ConnectedRelationshipId` from the supplier master when online.

See [buyer-specific sharing report](../reports/connected-supplier-buyer-specific-sharing-and-pricing.md).

### Effective PO price

Buyer-specific PO price → Default PO Price (`SupplierProductExposure.SupplierOrderPrice`). Retail `SellingPrice` is never a runtime fallback.

### Connected PO submission

For connected suppliers, submit validates every line (Active relationship + active link + shared + effective price) before Ordering and creates `ConnectedPurchaseOrder` with effective price snapshots in the same path. External suppliers unchanged. Inventory still changes only on Goods Receipt.

### Connected PO lifecycle (P27-WP02–WP05)

`PurchaseOrderStatus` remains the buyer purchasing aggregate state. `ConnectedPurchaseOrderStatus` tracks supplier-side commercial progress:

```text
New ──→ Accepted ──→ Preparing ──→ Fulfilled
 │          └────────────────────────→ Fulfilled
 ├──→ Declined
 └──→ Withdrawn
```

- Buyer withdrawal is valid only from `New`; supplier Accept/Decline and buyer Withdraw compete for that same transition.
- Persistence compares the stored and requested statuses against the transition matrix, so an Accept/Withdraw race has one winner and rejects the contradictory write.
- Supplier decline may include a bounded reason and note.
- `ConnectedPoDisplayStatus` derives buyer/supplier labels from both aggregates: Waiting for Supplier, Supplier Accepted/Declined, Preparing, Ready, Partially Received, Received, Received With Issues, and Withdrawn.
- Preparing/Fulfilled never changes buyer inventory. Buyer stock changes only through Goods Receipt, and only for good received quantity.
- Lifecycle notifications (submitted, accepted, declined, preparing, fulfilled, withdrawn, received/receiving issue) publish through `IOrganizationBusinessNotificationPublisher`; the persisted orders remain the source of truth.
- WP01 exposable/shared eligibility and buyer-specific → Default PO pricing are unchanged.

## Organization Web routes

- `/suppliers` — supplier masters + connection status (Pending visible after send)
- `/suppliers/requests` — Incoming (supplier view) and Sent (buyer view) connection requests
- `/suppliers/buyers` — **Connected buyers** (Active relationships where current org is supplier)
- `/suppliers/buyers/{relationshipId}` — connected buyer detail
- `/suppliers/connect` — send Connected ExItS connection request (Business QR / ORG######)

## Connection-request lifecycle (authoritative)

```text
Buyer sends request
  → ConnectedSupplierRelationship Status=Pending (persisted)
  → Buyer Supplier master created (ConnectionType=ConnectedOrganization, ConnectedRelationshipId set)
  → Counterparty display/public-id snapshots stored on relationship
Buyer Suppliers list shows Pending + “Waiting for supplier approval”
Supplier Incoming requests (MAUI `/suppliers/connected/requests` or Org Web `/suppliers/requests`) shows Accept/Decline
Discoverability: pending count banners on Owner home, More, Suppliers list (MAUI) and Overview + People → Supplier requests badge (Org Web)
Supplier Accept → Active (catalog/PO enabled for buyer); buyer appears under **Connected buyers**
Supplier Decline → Declined (no catalog/PO)
**Connected buyer ≠ Customer** — Accept does **not** create a seller-owned Customer; explicit Add as customer is deferred
Inventory unchanged until buyer Goods Receipt
```

### Notification / bell

**Bell = unified Organization notification center** (Platform `organization_in_app_notifications`).

Connected supplier lifecycle publishes into the same inbox as customer-link responses:

| Event | RelatedType | Recipient org |
|---|---|---|
| Buyer sends request | `SupplierConnectionRequested` | Supplier (Owners + Administrators) |
| Supplier accepts | `SupplierConnectionAccepted` | Buyer |
| Supplier declines | `SupplierConnectionDeclined` | Buyer |

- MAUI: header bell → `/org/notifications` (Unread / All; Accept/Decline when Pending + `ManageSuppliers`)
- Org Web: header bell → `/notifications` (same semantics; Owner + Manager)
- **Tap/open marks `IsRead=true` immediately** (optimistic local update + bell refresh). Unread ≠ Pending Action.
- Unread badge counts server `!IsRead` for the selected Organization only (0 hidden, 1–99 exact, >99 → `99+`)
- All tab = notification history (newest first); read items remain without permanent “Unread” label
- Deep links: Accepted/Declined (buyer) → `/suppliers`; Requested open → mark read (Accept/Decline stay if Pending); Accept success → Connected buyers
- Accept/Decline call existing Connected Supplier relationship APIs (not duplicated in notification code)
- After Accept/Decline, supplier org also receives a **local confirmation** in All history (`SupplierConnectionAcceptedConfirmation` / `DeclinedConfirmation`) — “{Buyer} is now a connected buyer.”
- Actioned requests mark related notifications read; rows remain in All/history
- **Suppliers → Requests / Connected buyers** remain domain source of truth
- Dashboard banner is a compact secondary attention card only

POS publishes via `POST /api/v1/organizations/{sourceOrgId}/business-notifications` (session-forwarded, best-effort). No second notification table.

Retention: notifications are not hard-deleted on read; no separate retention enforcement beyond existing Platform storage.

## Deferred
Marketplace discovery, inter-org payments, AP/invoices, live stock sharing, logistics, images in sync, Redis, brokers, auto-accept, auto-receive, full offline supplier catalog, connection-request cancellation while Pending (Disconnect remains Active-only), SignalR/realtime bell push, Organization Web per-buyer product sharing UI (MAUI is source of truth for share/price management).
