# P17-WP03 — Product and Inventory Setup

| Field | Value |
|---|---|
| Status | **Complete** (reconciled; no rebuild) |
| Phase | [Phase 17](../phases/phase-17-pos-mvp-operational-onboarding-and-first-sale.md) |
| Final Phase 17 commit | See [P17-WP08](P17-WP08-reports-hardening-and-closeout.md) |
| Date | 2026-07-29 |

## Objective

Minimum product catalog and inventory for the first sale.

## Existing functionality reused

- Catalog categories and products (P8/P10): name, SKU, barcode, selling price, active/inactive, search.
- Inventory accounts, stock adjustments, reorder/low-stock, stock validation on sale (`ISaleStockService`).
- Maui catalog/inventory pages and API endpoints under `/api/v1/pos/catalog/*` and `/api/v1/pos/inventory/*`.

## Implementation summary

- Reconciled requirements against existing modules; no duplicate catalog/inventory stack introduced.
- Phase 17 relies on existing product create/edit and stock checks during checkout.

## Files / components changed

- None required beyond shared Phase 17 documentation (catalog already sufficient for first sale).

## Authorization and isolation behavior

- `ViewCatalog` / `ManageCatalog` / inventory capabilities via commercial + POS role matrix.
- All catalog/inventory queries organization-scoped.

## Tests executed and results

- Existing catalog/inventory/sale stock integration tests remain authoritative (run in Phase 17 suite).

## Deferred items

- Suppliers, purchase orders, warehouses, variants — already present in Full POS for later ops; not Phase 17 MVP gates.

## Commit reference

Final Phase 17 commit recorded in P17-WP08.
