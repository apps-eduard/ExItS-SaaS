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
SQLite LocalStore schema **v7**; selling catalog uses full-replace page sync. Connected suppliers use **selective linked-product** tables + delta cursor (v8).

### Existing catalog pagination/search
Page-based `ListProductsAsync` (`PosPagination`: default 20, max 100). Connected supplier catalog search reuses page+search conventions with a **hard max of 50** for supplier catalog pages.

### Organization-to-organization relationships
**None** today. Platform Personal↔merchant links are not a substitute. Phase 1 introduces an explicit buyer↔supplier relationship aggregate in the POS database (Platform org GUIDs as values only; no cross-DB FK).

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

## LocalStore schema v8

Schema v8 adds four per-user/per-organization SQLite tables:

- `local_connected_supplier` — relationship status and optional buyer `SupplierId` attachment
- `local_linked_supplier_product` — selective buyer-linked products only, with relationship/name/SKU/version indexes
- `local_connected_supplier_sync_state` — per-relationship delta cursor and last-sync timestamp
- `local_connected_po_draft` — device-local draft JSON with `LocalEntitySyncState`; a local save is never supplier submission

`LinkedSupplierProductSyncService` requests `/links/sync?sinceVersion=…`, applies changed and removed link IDs, then advances the local cursor. It does not call catalog search and must never download a complete supplier catalog. Catalog and linked-product commercial fields are plain-text merchant product data, matching the existing local selling-catalog pattern. Outbox payload encryption remains unchanged; Phase 1 drafts are local-only and are not added to the outbox.

## MAUI routes

- `/suppliers/connected/request` — request an organization connection (online)
- `/suppliers/{id}/connected-catalog?relationshipId=…` — paged supplier catalog search (online only)
- `/suppliers/{id}/linked-products?relationshipId=…` — selective local linked-product browse (offline capable)
- `/purchasing/new` — external supplier picker remains unchanged; connected suppliers use online catalog search or local linked products offline
- `/connected-suppliers/incoming` — supplier incoming-order list with Accept/Decline (online)

Connected PO submission is online-only. Before creation, MAUI revalidates supplier price and availability and presents Update and continue, Review order, and Remove unavailable items choices. Offline save text is explicitly “Saved on this device” / “Waiting to sync”; it does not imply submission.

## Deferred
Marketplace discovery, inter-org payments, AP/invoices, live stock sharing, logistics, images in sync, Redis, brokers, bag/kg conversion engine, auto-accept, auto-receive, full offline supplier catalog.
