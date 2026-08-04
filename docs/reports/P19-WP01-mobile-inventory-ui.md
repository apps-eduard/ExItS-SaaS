# P19-WP01 — Mobile Inventory UI

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Commit | 01f7a87 |
| Production-ready | **No** |
| Device Verified | **No** |
| Date | 2026-08-04 |

## 1. Objective

Complete Mobile Inventory overview, low-stock, detail/movements, and stock adjustment UX with correct View vs Manage capability gating (Cashier view-only when granted).

## 2. Existing reuse

Phase 8–10 inventory APIs (`IPosInventoryClient`), MAUI Inventory pages, commercial feature grants `StoreInventoryView` / `StoreInventoryManage`.

## 3. Delivered

- Inventory list/search/paging with ViewInventory entry gate
- Low-stock list; stock counts CTA only when ManageInventory
- Detail with on-hand, reorder fields, movements list
- Adjust flow requires ManageInventory; quantity &gt; 0 and non-empty reason validation
- MoreHub Inventory nav hidden without ViewInventory
- Fixed incorrect Access Restricted banner for ViewInventory-only users

## 4. Residuals

- Server-side inventory category filter not on ListAsync contract — search + low-stock used instead (no new domain API)
- Reconciliation / reorder-suggestions APIs remain without dedicated list screens (detail/reorder pages cover primary manage paths)
- Purchasing / suppliers / warehouses / multi-branch out of scope

## 5. Tests

`InventoryPageGuardTests` — routes, ViewInventory/ManageInventory gates, adjust validation, MoreHub gating.

## 6. Authorization

API + session commercial grants authoritative. Client mirrors ViewInventory vs ManageInventory only.

## 7. Status

**Code Complete.** Phase 19 remains **Open**. Not Device Verified.
