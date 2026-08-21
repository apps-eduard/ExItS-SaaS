# RMAP-14 — Returns / Refunds

## Status

**HARD STOP — NOT STARTED (implementation withheld)**

**Code:** `RMAP14_EXPIRY_RETURN_CONTRACT_GAP`

## Baseline

| Item | Value |
|------|-------|
| Starting HEAD (this package attempt) | `08ba616c` (RMAP-13 complete) |
| Branch | `feat/pos-react-client` |

## HARD STOP finding

### `RMAP14_EXPIRY_RETURN_CONTRACT_GAP` — PROVEN

Return restock (`SaleReturnStockService.RestockForReturnAsync`) increases the organization `InventoryAccount` on-hand via `StockMovement.SaleReturnRestock` only.

For products with `TracksExpiration`:

- Sale checkout consumes lots via FEFO (`ConsumeFefoAsync`).
- Void restores original consumed lots via `RestoreSourceAsync` (`SaleVoidRestoration`).
- **Return does not call `RestoreSourceAsync` (or any lot restore).** It does not invent a new lot either.

Result: account quantity and lot ledger diverge after a return of an expiration-tracked product. React must not ship returns against that contract.

Package rule: do **not** invent a new lot, expiration date, or fake original lot. Closing this gap requires an Owner-authorized backend contract (likely mirroring void’s source restore for proportional return quantities) — not React-only UX.

### `RMAP14_UTANG_RETURN_RECONCILIATION_GAP` — NOT proven

Utang sale returns authoritatively reduce linked credit (`ReduceForSaleReturn`). Refund method is locked to the sale payment method. No ambiguous Cash-refund-leaving-debt path.

## What was inspected

| Area | Path / note |
|------|-------------|
| Return API | `POST /api/v1/pos/sale-returns`, refundable sale GET |
| Restock | `SaleReturnStockService.cs` — account only |
| Void lot restore | `InventoryUseCases` + `RestoreSourceAsync` |
| Utang return | `SaleReturnUseCases.ApplyUtangRefundAsync` |
| React | No sale-returns client/UI (correctly withheld) |

## Exclusions / not delivered

- Partial/full return UI
- Refund amount UX
- Inventory restore UX
- Any React return POST
- Lot invent / fake restore
- New DB migration

## Exact next

Owner / ChatGPT must authorize a backend fix for expiration-aware return restock (or an explicit alternate safe contract).

Then restart **RMAP-14 only** — do not reopen RMAP-11…RMAP-13 unless a regression is proven.

Do **not** start RMAP-15, RMAP-B01, RMAP-12b, RMAP-B04, RMAP-TAX, or provider payments.
