# RMAP-06 — Today's Prices

## Status

**COMPLETE**

## Baseline

starting SHA: `ae614cab6cc7ca43d3eff1d829d3840e7ba3606a` (post RMAP-05 docs)

## Contract review

| Area | Finding |
|------|---------|
| Endpoint | `POST /api/v1/pos/catalog/products/prices` — ManageCatalog; partial success 200 |
| Concurrency | `expectedUpdatedAtUtc` **required** (fail-closed) |
| Scope | Product base selling price + primary Sell unit only; other unit prices independent |
| Cashier override | Excluded (RMAP-B01 / RMAP-12b) |
| Owner decision | NO |

## Implementation

- `/catalog/todays-prices` bulk editor with dirty tracking, sticky save, per-row conflict feedback
- Uses required concurrency tokens from list/load

## Next

RMAP-07 — Inventory tracking
