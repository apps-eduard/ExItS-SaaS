# Connected ExItS Suppliers — Phase 1 Completion Report

Phase marker: `connected-exits-suppliers-phase-1`  
Date: 2026-08-14  
Starting SHA: `f67c2763c3e30a7a2b77847f23a101f57c403ca6`

## Implementation commits

| SHA | Message |
|---|---|
| `670a79e1` | `feat(suppliers): add connected organization relationships and catalog APIs` |
| `8d18dfa8` | `feat(sync): add selective connected-supplier offline projection` |
| `7f2020db` | `feat(purchasing): add connected supplier MAUI workflows` |
| `1fdc89d0` | `docs(purchasing): document connected ExItS supplier Phase 1` |

## Status

**Code Complete (Phase 1).** Not Device Verified. Not Browser Verified. **Not Production Ready.**

## Delivered capability

- Explicit buyer ↔ supplier organization relationship (`Pending` / `Active` / `Declined` / `Disconnected`)
- Additive supplier connection metadata (`External` vs `ConnectedOrganization`)
- Supplier-controlled product exposure with dedicated **supplier order price** (not POS selling price)
- Buyer ↔ supplier product links (extensible for future package conversion)
- Online server-side paged catalog search (default 25, max 50); no full-catalog download
- Selective LocalStore v8 linked-product projection + delta sync by `SyncVersion`
- Offline linked-product search and local PO draft (“Saved on this device” / “Waiting to sync”)
- Online submit revalidation for price/availability/relationship changes
- Supplier incoming-order inbox with Accept / Decline
- Existing PO → Submit → Receive → Inventory flow preserved; Accept never adds stock

## Explicit exclusions / deferred

Marketplace discovery, inter-org payments, AP/invoices, live stock sharing, logistics/shipping, images in sync, Redis, message brokers, bag/kg conversion engine, auto-accept, auto-receive, full offline supplier catalog copy, automatic reconnect submission of local drafts.

## Persistence / migration

| Item | Value |
|---|---|
| Migration | `20260814180000_AddPosConnectedSuppliers` |
| Tables | `connected_supplier_relationships`, `supplier_product_exposures`, `buyer_supplier_product_links`, `connected_purchase_orders`, `connected_purchase_order_lines` |
| Supplier columns | `connection_type`, `connected_relationship_id` |
| LocalStore | schema **v8** linked products + sync state + local PO drafts |

## Network / VPS behavior

| Question | Answer |
|---|---|
| Supplier full catalog downloaded? | **NO** |
| Server search page size | default **25**, hard max **50** |
| Offline data | linked products only (~buyer selection, not 100k) |
| Delta sync | `sinceVersion` / `SyncVersion` cursor |
| Images in payloads? | **NO** |

## Security

- Organization scope from server context
- Active relationship required for catalog search
- Cross-org IDOR → fail closed
- Supplier cannot mutate buyer inventory
- Buyer cannot mutate supplier catalog exposures of other orgs

## Device / Browser / Production

| Gate | Value |
|---|---|
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Documentation

- `docs/engineering/connected-exits-suppliers.md`
- `docs/engineering/offline-sync-design.md` (linked-product delta note)
- `docs/reports/connected-exits-suppliers-phase-1.md` (this file)
- `FILE-MANIFEST.md`

## Exact next work package

Do **not** begin marketplace, payments, or package-conversion engine until explicitly authorized. Recommended follow-up: Device Verified E2E with two Local Validation organizations + harden reconnect draft submission UX.
