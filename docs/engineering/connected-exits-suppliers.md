# Connected ExItS Suppliers — Phase 1 Design

Phase marker: `connected-exits-suppliers-phase-1`  
Status: **Code Complete (Phase 1)** — see [completion report](../reports/connected-exits-suppliers-phase-1.md)  
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

## Conceptual model

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
- `/suppliers/{id}/connected-catalog?relationshipId=…` — paged supplier catalog search (online only; **Active** only)
- `/suppliers/{id}/linked-products?relationshipId=…` — selective local linked-product browse (offline capable)
- `/purchasing/new` — external supplier picker remains unchanged; connected suppliers use online catalog search or local linked products offline
- `/connected-suppliers/incoming` — supplier **incoming purchase orders** with Accept/Decline (online) — distinct from connection requests

## Organization Web routes

- `/suppliers` — supplier masters + connection status (Pending visible after send)
- `/suppliers/requests` — Incoming (supplier view) and Sent (buyer view) connection requests
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
Supplier Accept → Active (catalog/PO enabled for buyer)
Supplier Decline → Declined (no catalog/PO)
Inventory unchanged until buyer Goods Receipt
```

### Notification / bell

Organization in-app notifications exist for **customer-link** events on Platform. Connected-supplier request events are **not** emitted into the bell yet (would require a POS→Platform cross-product notify path for supplier-org recipients). Visibility is via Owner/More/Suppliers banners, Org Web Overview banner, and menu count on Supplier requests. Bell integration is **deferred**.

## Deferred
Marketplace discovery, inter-org payments, AP/invoices, live stock sharing, logistics, images in sync, Redis, brokers, auto-accept, auto-receive, full offline supplier catalog, connection-request cancellation while Pending (Disconnect remains Active-only), organization-notification bell for supplier connections.
